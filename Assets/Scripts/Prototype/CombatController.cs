using UnityEngine;

/// <summary>
/// U attack (ground string, W+U launcher, air attack, S+U pogo), J heavy, I finisher.
/// Techniques: hit cancel, pogo reset, surface attacks, momentum hits, launcher into air combo, fast fall.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class CombatController : MonoBehaviour
{
    enum Kind { None, String, Launcher, Air, Pogo, Heavy, Finisher }

    [Header("Feel")]
    public float inputBuffer = 0.15f;
    public float pogoBounceSpeed = 14f;
    public float fastFallSpeed = 20f;
    public float hitstopLight = 0.045f;
    public float hitstopHeavy = 0.08f;
    [Tooltip("Speed at which hits start to get stronger, and speed at which the bonus is maxed.")]
    public Vector2 momentumSpeedRange = new Vector2(8f, 22f);
    public float momentumMaxScale = 1.8f;
    [Tooltip("Rayne's Mag Pulse on a wall or ceiling pushes her off it at this speed.")]
    public float magPulseLaunchSpeed = 16f;

    public bool Busy => runner != null && runner.Running;
    /// <summary>After a hit lands, recovery can be cancelled into jump, dash, heavy, finisher or swap.</summary>
    public bool InCancelWindow => Busy && runner.HasHit && runner.Phase != AttackPhase.Startup;
    public string CurrentMove => Busy ? runner.Data.name : "";

    PlayerMovement mv;
    Rigidbody2D body;
    PartyController party;
    AttackRunner runner;
    Kind kind;
    int stringIndex;
    bool surfaceAttack;
    float attackBufferTimer;
    bool onSurfaceAtStart;

    void Awake()
    {
        mv = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        party = GetComponent<PartyController>();
        runner = new AttackRunner(Team.Player, transform);
        runner.OnHit += HandleHit;
        runner.OnActiveStart += HandleActiveStart;
        runner.OnFinished += HandleFinished;
    }

    void Update()
    {
        if (TimeController.Paused) return;
        float dt = Time.deltaTime;
        if (attackBufferTimer > 0f) attackBufferTimer -= Time.unscaledDeltaTime;

        int surface = mv.MagSwitch;
        bool airborne = surface == 0 && !mv.GroundedNow;

        // Fast fall (RPG doc): S in the air
        if (airborne && !mv.IsSuspended && ProtoInput.FastFallDown && body.velocity.y < 2f && !Input.GetKey(ProtoInput.Attack))
            body.velocity = new Vector2(body.velocity.x, Mathf.Min(body.velocity.y, -fastFallSpeed));

        if (ProtoInput.AttackDown) attackBufferTimer = inputBuffer;

        if (Busy)
        {
            if (InCancelWindow)
            {
                if (ProtoInput.JumpDown) { CancelAttack("Hit cancel", "-> jump"); }
                else if (ProtoInput.FinisherDown && party.Gauge >= 1f) { CancelAttack("Hit cancel", "-> finisher"); StartFinisher(); }
                else if (ProtoInput.HeavyDown && kind != Kind.Heavy) { CancelAttack("Hit cancel", "-> heavy"); StartMove(Kind.Heavy, party.Active.heavy); }
            }
            // String continuation (works on whiff too, from recovery)
            if (Busy && attackBufferTimer > 0f && kind == Kind.String && runner.Phase == AttackPhase.Recovery
                && stringIndex + 1 < party.Active.groundString.Length)
            {
                attackBufferTimer = 0f;
                stringIndex++;
                StartMove(Kind.String, party.Active.groundString[stringIndex], keepString: true);
            }
        }
        else
        {
            if (ProtoInput.FinisherDown && party.Gauge >= 1f) StartFinisher();
            else if (ProtoInput.HeavyDown) StartMove(Kind.Heavy, party.Active.heavy);
            else if (attackBufferTimer > 0f)
            {
                attackBufferTimer = 0f;
                StartAttack(surface, airborne);
            }
        }

        runner.Tick(dt, CurrentFrame());

        bool lockRun = Busy && runner.Data.locksMovement && !(InCancelWindow && runner.Phase == AttackPhase.Recovery);
        mv.inputLocked = lockRun;
        mv.jumpLocked = Busy && runner.Data.locksMovement && !InCancelWindow;
    }

    void StartAttack(int surface, bool airborne)
    {
        CharacterDef c = party.Active;
        Vector2 m = ProtoInput.Move;
        bool onWallOrCeiling = c.magSwitch && (surface & (2 | 4 | 8)) != 0;
        if (onWallOrCeiling) { StartMove(Kind.String, c.groundString[0]); return; }
        if (airborne)
        {
            if (m.y < -0.5f) StartMove(Kind.Pogo, c.pogo);
            else StartMove(Kind.Air, c.air);
            return;
        }
        if (m.y > 0.5f) StartMove(Kind.Launcher, c.launcher);
        else StartMove(Kind.String, c.groundString[0]);
    }

    void StartFinisher()
    {
        if (!party.TrySpendGauge()) return;
        StartMove(Kind.Finisher, party.Active.finisher);
        TechLog.Report("Finisher", party.Active.finisher.name);
    }

    void StartMove(Kind k, AttackData data, bool keepString = false)
    {
        if (data == null) return;
        if (!keepString) stringIndex = 0;
        kind = k;
        int surface = mv.MagSwitch;
        onSurfaceAtStart = party.Active.magSwitch && (surface & (2 | 4 | 8)) != 0;
        surfaceAttack = onSurfaceAtStart;
        runner.Start(data);
        float speed = body.velocity.magnitude;
        float t = Mathf.InverseLerp(momentumSpeedRange.x, momentumSpeedRange.y, speed);
        runner.DamageScale = Mathf.Lerp(1f, momentumMaxScale, t);
    }

    AttackFrame CurrentFrame()
    {
        int s = mv.MagSwitch;
        float angle = (party.Active.magSwitch && s != 0) ? mv.ToRotation : 0f;
        return new AttackFrame { position = body.position, angleDeg = angle, facing = mv.Facing == 0 ? 1 : mv.Facing };
    }

    void HandleActiveStart()
    {
        AttackData d = runner.Data;
        if (d.selfVelocity != Vector2.zero)
        {
            // Lunge along the attack's forward direction
            Vector2 v = CurrentFrame().ToWorldDir(d.selfVelocity);
            mv.Suspend(d.Active, 0f);
            body.velocity = v;
        }
        if (kind == Kind.Heavy && party.Active.magSwitch && onSurfaceAtStart)
        {
            // Mag Pulse on a wall or ceiling throws Rayne off it
            Vector2 n = PartyController.SurfaceNormal(mv.MagSwitch);
            mv.Suspend(0.12f, 0f);
            body.velocity = n * magPulseLaunchSpeed + new Vector2(mv.Facing * 4f, 0f);
        }
    }

    void HandleFinished()
    {
        kind = Kind.None;
    }

    void HandleHit(IHittable target, HitInfo hit)
    {
        if (hit.isPogo)
        {
            body.velocity = new Vector2(body.velocity.x, pogoBounceSpeed);
            mv.EndSuspend();
            party.RefundAirDash();
            TechLog.Report("Pogo reset", target is EnemyBase ? "enemy" : "hazard");
        }
        if (!(target is EnemyBase enemy)) return;

        ComboTracker.RegisterHit();
        party.OnDealtHit();
        bool heavy = kind == Kind.Heavy || kind == Kind.Finisher;
        TimeController.Hitstop(heavy ? hitstopHeavy : hitstopLight);

        if (surfaceAttack) TechLog.Report("Surface attack", runner.Data.name);
        if (runner.DamageScale >= 1.25f) TechLog.Report("Momentum hit", "x" + runner.DamageScale.ToString("0.0"));
        if (hit.isAirMove && enemy.WasLaunched) TechLog.Report("Launcher -> air combo");
    }

    /// <summary>End the current attack because of a cancel; logs the technique.</summary>
    public void CancelAttack(string tech, string detail)
    {
        if (!Busy) return;
        bool wasHit = runner.HasHit;
        runner.Cancel();
        mv.inputLocked = false;
        mv.jumpLocked = false;
        if (wasHit) TechLog.Report(tech, detail);
    }

    /// <summary>Player got hit or respawned: drop the attack, no log.</summary>
    public void Interrupt()
    {
        runner.Cancel();
        attackBufferTimer = 0f;
        mv.inputLocked = false;
        mv.jumpLocked = false;
    }

    /// <summary>Hit from an assist ghost: counts toward combo and gauge like a normal hit.</summary>
    public void RegisterAssistHit(IHittable target, HitInfo hit)
    {
        if (!(target is EnemyBase)) return;
        ComboTracker.RegisterHit();
        party.OnDealtHit();
        TimeController.Hitstop(hitstopLight);
    }
}
