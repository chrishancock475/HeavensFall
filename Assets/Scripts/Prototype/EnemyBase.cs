using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base for prototype enemies: HP, hitstun, knockback, launch, flash, telegraph tint.
/// All AI is frame-timed and deterministic (no RNG, RPG doc pillar).
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IHittable
{
    public static readonly List<EnemyBase> All = new List<EnemyBase>();
    public static bool AnyInHitstun
    {
        get
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].InHitstun) return true;
            return false;
        }
    }

    [Header("Enemy")]
    public float maxHp = 30f;
    [Tooltip("Armoured enemies take damage but no hitstun or knockback.")]
    public bool armoured;
    public bool launchable = true;
    public Color baseColor = new Color(0.55f, 0.2f, 0.65f);
    public Color telegraphColor = new Color(1f, 0.55f, 0.1f);

    public float Hp { get; protected set; }
    public bool Dead { get; protected set; }
    public bool InHitstun => !Dead && (hitstunTimer > 0f || recentHitTimer > 0f);
    public bool WasLaunched { get; protected set; }
    public Team Team => Team.Enemy;

    protected Rigidbody2D body;
    protected SpriteRenderer sr;
    protected Collider2D[] cols;
    protected float hitstunTimer;
    protected float recentHitTimer;
    protected float flashTimer;
    protected bool telegraphing;
    protected int facing = -1;
    protected Vector3 spawnPosition;

    protected Transform Player => PartyController.Instance != null ? PartyController.Instance.transform : null;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        cols = GetComponentsInChildren<Collider2D>();
        Hp = maxHp;
        spawnPosition = transform.position;
    }

    protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    protected virtual void Start()
    {
        // Enemies don't shove the player around; attacks are hitbox-only.
        var player = FindObjectOfType<PlayerMovement>();
        if (player == null) return;
        foreach (var pc in player.GetComponentsInChildren<Collider2D>())
            foreach (var c in cols)
                Physics2D.IgnoreCollision(pc, c, true);
    }

    protected virtual void Update()
    {
        if (Dead || TimeController.Paused) return;
        float dt = Time.deltaTime;
        if (flashTimer > 0f) flashTimer -= dt;
        if (recentHitTimer > 0f) recentHitTimer -= dt;
        if (WasLaunched && hitstunTimer <= 0f && IsGrounded()) WasLaunched = false;

        if (hitstunTimer > 0f)
        {
            hitstunTimer -= dt;
            TickHitstun(dt);
        }
        else
        {
            TickAI(dt);
        }
        UpdateColor();
    }

    protected abstract void TickAI(float dt);
    protected virtual void TickHitstun(float dt) { }
    protected virtual void OnHitstunStart() { }

    public virtual bool ReceiveHit(HitInfo hit)
    {
        if (Dead || hit.attackerTeam == Team.Enemy) return false;
        Hp -= hit.damage;
        flashTimer = 0.08f;
        if (armoured)
        {
            recentHitTimer = 0.45f;
        }
        else
        {
            hitstunTimer = hit.hitstun;
            telegraphing = false;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                Vector2 kb = hit.knockback;
                if (hit.launch && launchable)
                {
                    kb = new Vector2(kb.x * 0.3f, Mathf.Max(kb.y, 12f));
                    WasLaunched = true;
                }
                else if (WasLaunched)
                {
                    kb.y = Mathf.Max(kb.y, 4f);   // juggle: keep them up a little
                }
                body.velocity = kb;
            }
            OnHitstunStart();
        }
        if (Hp <= 0f) Die();
        return true;
    }

    protected virtual void Die()
    {
        Dead = true;
        gameObject.SetActive(false);
    }

    public virtual void ResetEnemy()
    {
        Dead = false;
        Hp = maxHp;
        hitstunTimer = recentHitTimer = 0f;
        WasLaunched = false;
        telegraphing = false;
        transform.position = spawnPosition;
        if (body != null) body.velocity = Vector2.zero;
        gameObject.SetActive(true);
    }

    protected void UpdateColor()
    {
        if (sr == null) return;
        Color c = baseColor;
        if (telegraphing) c = Mathf.Repeat(Time.time, 0.15f) < 0.075f ? telegraphColor : Color.Lerp(baseColor, telegraphColor, 0.5f);
        if (InHitstun && !armoured) c = Color.Lerp(c, Color.white, 0.3f);
        if (flashTimer > 0f) c = Color.white;
        sr.color = c;
        sr.flipX = facing > 0;
    }

    protected bool IsGrounded()
    {
        if (cols.Length == 0) return true;
        Bounds b = cols[0].bounds;
        return Geo.Solid(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, transform);
    }

    protected int DirToPlayer()
    {
        var p = Player;
        if (p == null) return facing;
        return p.position.x >= transform.position.x ? 1 : -1;
    }

    protected AttackFrame Frame() => new AttackFrame { position = transform.position, angleDeg = 0f, facing = facing };
}
