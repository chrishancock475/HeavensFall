using System;
using UnityEngine;

/// <summary>
/// Rayne + John on one body: swap (O tap), assist (O hold), Shift ability (Rayne time slow, John air dash),
/// shared HP, combo gauge. Sits on the Player prefab instance next to PlayerMovement.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PartyController : MonoBehaviour, IHittable
{
    public static event Action OnPlayerDeath;
    public static PartyController Instance { get; private set; }

    [Header("Party")]
    public CharacterDef rayne = CharacterDef.Rayne();
    public CharacterDef john = CharacterDef.John();

    [Header("HP (shared)")]
    public int maxHp = 5;
    public float damageInvulnerability = 1f;
    public float respawnInvulnerability = 1.5f;

    [Header("Rayne: time slow")]
    public float slowMeterMax = 3f;        // real seconds of slow
    public float slowRegenPerSecond = 0.15f;
    public float slowRefillPerHit = 0.2f;

    [Header("John: air dash")]
    public float dashSpeed = 25f;
    public float dashTime = 0.2f;
    public float dashCooldown = 0.25f;
    [Tooltip("Getting hit this soon after the dash starts counts as a just dodge (6 frames).")]
    public float justDodgeWindow = 0.1f;
    public float justDodgeSlow = 1f;

    [Header("Swap / assist")]
    public float assistHoldTime = 0.25f;
    public float assistCooldown = 3f;
    public float assistCooldownPerHit = 0.15f;
    public float swapCooldown = 0.2f;
    [Tooltip("Swap to Rayne this soon after a dash and she snaps onto steel at dash speed.")]
    public float dashSnapWindow = 0.15f;

    [Header("Combo gauge")]
    public float gaugePerHit = 0.06f;

    public int Hp { get; private set; }
    public bool ActiveIsJohn { get; private set; }
    public CharacterDef Active => ActiveIsJohn ? john : rayne;
    public CharacterDef Partner => ActiveIsJohn ? rayne : john;
    public bool IsDashing => dashTimer > 0f;
    public float SlowMeter { get; private set; }
    public float SlowMeterMax => slowMeterMax * (AbilityState.MagCoil ? 1.5f : 1f);
    public float Gauge { get; private set; }
    public float AssistCooldownLeft => assistTimer;
    public bool Invulnerable => invulnTimer > 0f || IsDashing;
    public Team Team => Team.Player;

    PlayerMovement mv;
    Rigidbody2D body;
    CombatController combat;
    SpriteRenderer[] renderers;
    Color[] baseColors;

    float dashTimer, dashCooldownTimer, dashStartTime = -10f, lastDashEndTime = -10f;
    bool airDashAvailable = true;
    bool justDodgedThisDash;
    float invulnTimer;
    float swapTimer, assistTimer;
    float swapHeldFor;
    bool assistFiredThisHold;
    int snapCheckFrames;
    float snapSpeed;
    bool slowing;

    void Awake()
    {
        Instance = this;
        mv = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        combat = GetComponent<CombatController>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        mv.legacyTimeSlow = false;
        Hp = maxHp;
        SlowMeter = slowMeterMax;
        ApplyProfile();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        TimeController.SetSlow(this, false);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float rdt = Time.unscaledDeltaTime;
        if (TimeController.Paused) return;

        if (invulnTimer > 0f) invulnTimer -= dt;
        if (swapTimer > 0f) swapTimer -= dt;
        if (assistTimer > 0f) assistTimer -= dt;
        if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;
        if (dashTimer > 0f)
        {
            dashTimer -= dt;
            if (dashTimer <= 0f) lastDashEndTime = Time.time;
        }
        if (mv.GroundedNow && !IsDashing) airDashAvailable = true;

        UpdateTimeSlow(rdt);
        UpdateDash();
        UpdateSwap(dt);
        UpdateFlash();
    }

    void FixedUpdate()
    {
        // Dash snap: John dashed into steel, swapped to Rayne, and she grabs the surface at dash speed.
        if (snapCheckFrames > 0)
        {
            snapCheckFrames--;
            int s = mv.MagSwitch;
            if ((s & (2 | 4 | 8)) != 0)
            {
                Vector2 normal = SurfaceNormal(s);
                Vector2 tangent = new Vector2(-normal.y, normal.x);
                float sign = Mathf.Sign(Vector2.Dot(body.velocity, tangent));
                if (sign == 0f) sign = 1f;
                body.velocity = tangent * sign * Mathf.Max(snapSpeed, body.velocity.magnitude);
                TechLog.Report("Dash snap");
                snapCheckFrames = 0;
            }
        }
    }

    // ------------------------------------------------------------------ Shift ability

    void UpdateTimeSlow(float rdt)
    {
        bool want = !ActiveIsJohn && ProtoInput.AbilityHeld && SlowMeter > 0f;
        if (want)
        {
            SlowMeter = Mathf.Max(0f, SlowMeter - rdt);
        }
        else
        {
            SlowMeter = Mathf.Min(SlowMeterMax, SlowMeter + slowRegenPerSecond * rdt);
        }
        if (want != slowing)
        {
            slowing = want;
            TimeController.SetSlow(this, want);
        }
    }

    void UpdateDash()
    {
        if (!ActiveIsJohn || !ProtoInput.AbilityDown) return;
        if (combat != null && combat.Busy && !combat.InCancelWindow) return;
        if (TryDash() && combat != null && combat.Busy)
        {
            combat.CancelAttack("Hit cancel", "-> dash");
        }
    }

    public bool TryDash()
    {
        if (!ActiveIsJohn || IsDashing || dashCooldownTimer > 0f) return false;
        bool grounded = mv.GroundedNow;
        if (!grounded && !airDashAvailable) return false;
        Vector2 dir = ProtoInput.Dir8;
        if (dir == Vector2.zero) dir = new Vector2(mv.Facing, 0f);
        if (!grounded) airDashAvailable = false;
        dashTimer = dashTime;
        dashCooldownTimer = dashTime + dashCooldown;
        dashStartTime = Time.time;
        justDodgedThisDash = false;
        mv.Suspend(dashTime, 0f);
        body.velocity = dir * dashSpeed;
        return true;
    }

    public void RefundAirDash()
    {
        airDashAvailable = true;
        dashCooldownTimer = 0f;
    }

    // ------------------------------------------------------------------ swap and assist

    void UpdateSwap(float dt)
    {
        if (!AbilityState.HasJohn) return;
        if (ProtoInput.SwapDown) { swapHeldFor = 0f; assistFiredThisHold = false; }
        if (ProtoInput.SwapHeld)
        {
            swapHeldFor += Time.unscaledDeltaTime;
            if (!assistFiredThisHold && swapHeldFor >= assistHoldTime)
            {
                assistFiredThisHold = true;
                TryAssist();
            }
        }
        if (ProtoInput.SwapUp && !assistFiredThisHold) TrySwap();
    }

    public bool TrySwap()
    {
        if (!AbilityState.HasJohn || swapTimer > 0f) return false;
        bool cancel = false;
        if (combat != null && combat.Busy)
        {
            if (!combat.InCancelWindow) return false;
            cancel = true;
        }
        bool inCombo = ComboTracker.InCombo;
        bool wasDashing = IsDashing || Time.time - lastDashEndTime <= dashSnapWindow;
        float speedBefore = body.velocity.magnitude;

        if (cancel) combat.CancelAttack("Hit cancel", "-> swap");
        ActiveIsJohn = !ActiveIsJohn;
        swapTimer = swapCooldown;
        if (IsDashing && !ActiveIsJohn)
        {
            dashTimer = 0f;
            lastDashEndTime = Time.time;
            mv.EndSuspend();
        }
        ApplyProfile();

        if (inCombo) TechLog.Report("Tag combo", "-> " + Active.name);
        if (!ActiveIsJohn && wasDashing)
        {
            snapCheckFrames = 6;
            snapSpeed = Mathf.Max(speedBefore, dashSpeed * 0.9f);
        }
        return true;
    }

    void TryAssist()
    {
        if (assistTimer > 0f) return;
        assistTimer = assistCooldown;
        AssistGhost.Spawn(this, Partner, mv.Facing, body.position);
        if (ComboTracker.InCombo) TechLog.Report("Tag combo", "assist " + Partner.name);
    }

    void ApplyProfile()
    {
        mv.magSwitchEnabled = Active.magSwitch;
        if (ActiveIsJohn && slowing)
        {
            slowing = false;
            TimeController.SetSlow(this, false);
        }
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = baseColors[i] * Active.tint;
    }

    public Sprite CurrentSprite
    {
        get
        {
            foreach (var r in renderers) if (r != null && r.sprite != null) return r.sprite;
            return null;
        }
    }

    // ------------------------------------------------------------------ hits dealt

    /// <summary>Called by CombatController and AssistGhost for every hit that counted.</summary>
    public void OnDealtHit()
    {
        Gauge = Mathf.Min(1f, Gauge + gaugePerHit * (1f + Mathf.Min(ComboTracker.Count, 10) * 0.05f));
        SlowMeter = Mathf.Min(SlowMeterMax, SlowMeter + slowRefillPerHit);
        assistTimer = Mathf.Max(0f, assistTimer - assistCooldownPerHit);
    }

    public bool TrySpendGauge()
    {
        if (Gauge < 1f) return false;
        Gauge = 0f;
        return true;
    }

    // ------------------------------------------------------------------ hits taken

    public bool ReceiveHit(HitInfo hit)
    {
        if (hit.attackerTeam == Team.Player) return false;
        if (IsDashing)
        {
            if (!justDodgedThisDash && Time.time - dashStartTime <= justDodgeWindow)
            {
                justDodgedThisDash = true;
                TimeController.GrantSlow(justDodgeSlow);
                TechLog.Report("Just dodge");
            }
            return false;
        }
        if (invulnTimer > 0f) return false;

        Hp -= Mathf.Max(1, Mathf.RoundToInt(hit.damage));
        invulnTimer = damageInvulnerability;
        if (combat != null) combat.Interrupt();
        Vector2 kb = hit.knockback;
        if (kb.sqrMagnitude < 0.01f) kb = Vector2.up * 8f;
        mv.Suspend(0.2f, 3f);
        body.velocity = kb;
        if (Hp <= 0) Die();
        return true;
    }

    public void Die()
    {
        Hp = maxHp;
        invulnTimer = respawnInvulnerability;
        dashTimer = 0f;
        if (combat != null) combat.Interrupt();
        mv.EndSuspend();
        if (GameManager.checkpoint != null) mv.Die();
        ProtoHud.Message("Down. Back at the last save point.");
        OnPlayerDeath?.Invoke();
    }

    public void HealFull() => Hp = maxHp;

    public void DebugFillGauge() => Gauge = 1f;

    public void AddMaxHp(int amount)
    {
        maxHp += amount;
        Hp = maxHp;
    }

    void UpdateFlash()
    {
        bool blink = invulnTimer > 0f && Mathf.Repeat(Time.time, 0.12f) < 0.06f;
        float a = blink ? 0.35f : 1f;
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = baseColors[i] * Active.tint;
            if (IsDashing) c = Color.Lerp(c, Color.cyan, 0.5f);
            c.a *= a;
            renderers[i].color = c;
        }
    }

    public static Vector2 SurfaceNormal(int magSwitch)
    {
        // bit 1 floor, 2 wall on the left, 4 ceiling, 8 wall on the right (see PlayerMovement.MagSwitch)
        if ((magSwitch & 4) != 0) return Vector2.down;
        if ((magSwitch & 2) != 0) return Vector2.right;
        if ((magSwitch & 8) != 0) return Vector2.left;
        return Vector2.up;
    }
}
