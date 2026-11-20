using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Levite Warden mini-boss. Fixed cycle: Sweep, Leap Slam (armoured in the air), Sweep, Summon drones.
/// Never stunned; hits still count for combos. Started and reset by WardenArena.
/// </summary>
public class WardenBoss : EnemyBase
{
    enum State { Dormant, Idle, SweepWindup, Sweep, LeapWindup, Leaping, SlamRecover, SummonWindup, Recover }
    enum Move { Sweep, Leap, Summon }

    public event Action OnDefeated;

    public float idleTime = 1.0f;
    public float sweepWindup = 0.7f;
    public float leapWindup = 0.5f;
    public float leapAirTime = 0.9f;
    public float slamRecover = 1.0f;
    public float summonWindup = 0.8f;
    public float recoverTime = 0.6f;
    public int maxDrones = 2;

    public AttackData sweep = new AttackData
    {
        name = "Warden Sweep", startup = 0, active = 12, recovery = 0,
        offset = new Vector2(2f, -0.8f), size = new Vector2(4f, 1.4f), damage = 1, knockback = new Vector2(10f, 6f),
        locksMovement = false,
    };
    public AttackData slam = new AttackData
    {
        name = "Warden Slam", startup = 0, active = 10, recovery = 0,
        offset = new Vector2(0f, -1f), size = new Vector2(8f, 1.2f), damage = 1, knockback = new Vector2(8f, 10f),
        locksMovement = false,
    };

    static readonly Move[] Cycle = { Move.Sweep, Move.Leap, Move.Sweep, Move.Summon };

    State state = State.Dormant;
    int cycleIndex;
    float timer;
    AttackRunner runner;
    readonly List<Drone> summoned = new List<Drone>();

    public bool Started => state != State.Dormant;

    void Reset()
    {
        maxHp = 200f;
        launchable = false;
        baseColor = new Color(0.35f, 0.1f, 0.45f);
    }

    protected override void Awake()
    {
        base.Awake();
        runner = new AttackRunner(Team.Enemy, transform);
        armoured = true;   // no hitstun ever; damage only
    }

    public void Begin()
    {
        if (state != State.Dormant || Dead) return;
        state = State.Idle;
        timer = idleTime;
        cycleIndex = 0;
    }

    protected override void TickAI(float dt)
    {
        if (state == State.Dormant) { UpdateColor(); return; }
        timer -= dt;
        bool grounded = IsGrounded();

        switch (state)
        {
            case State.Idle:
                facing = DirToPlayer();
                StopX();
                if (timer <= 0f) StartNext();
                break;
            case State.SweepWindup:
                StopX();
                if (timer <= 0f) { telegraphing = false; runner.Start(sweep); state = State.Sweep; timer = sweep.Total; }
                break;
            case State.Sweep:
                if (timer <= 0f) { state = State.Recover; timer = recoverTime; }
                break;
            case State.LeapWindup:
                facing = DirToPlayer();
                StopX();
                if (timer <= 0f) Leap();
                break;
            case State.Leaping:
                if (timer <= 0f && grounded)
                {
                    runner.Start(slam);
                    state = State.SlamRecover;
                    timer = slamRecover;
                    StopX();
                }
                break;
            case State.SlamRecover:
                StopX();
                if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                break;
            case State.SummonWindup:
                StopX();
                if (timer <= 0f) { telegraphing = false; Summon(); state = State.Recover; timer = recoverTime; }
                break;
            case State.Recover:
                StopX();
                if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                break;
        }
        runner.Tick(dt, Frame());
    }

    void StartNext()
    {
        Move m = Cycle[cycleIndex];
        cycleIndex = (cycleIndex + 1) % Cycle.Length;
        telegraphing = true;
        switch (m)
        {
            case Move.Sweep: state = State.SweepWindup; timer = sweepWindup; break;
            case Move.Leap: state = State.LeapWindup; timer = leapWindup; break;
            case Move.Summon:
                summoned.RemoveAll(d => d == null || d.Dead);
                if (summoned.Count >= maxDrones) { telegraphing = true; state = State.SweepWindup; timer = sweepWindup; }
                else { state = State.SummonWindup; timer = summonWindup; }
                break;
        }
    }

    void Leap()
    {
        telegraphing = false;
        var p = Player;
        float targetX = p != null ? p.position.x : transform.position.x;
        float g = -Physics2D.gravity.y * body.gravityScale;
        float t = leapAirTime;
        body.velocity = new Vector2((targetX - transform.position.x) / t, 0.5f * g * t);
        state = State.Leaping;
        timer = 0.2f;   // ignore "grounded" for the first frames of the jump
    }

    void Summon()
    {
        summoned.RemoveAll(d => d == null || d.Dead);
        for (int i = summoned.Count; i < maxDrones; i++)
        {
            Vector3 at = transform.position + new Vector3(i == 0 ? -5f : 5f, 6f, 0f);
            var go = new GameObject("Summoned Drone");
            go.SetActive(false);   // so Drone.Awake runs after the fields below are set
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = this.sr != null ? this.sr.sprite : HitboxVisualizer.MakeSquare();
            go.transform.localScale = new Vector3(1.2f / Mathf.Max(0.01f, sr.sprite.bounds.size.x), 0.8f / Mathf.Max(0.01f, sr.sprite.bounds.size.y), 1f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var d = go.AddComponent<Drone>();
            d.maxHp = 15f;
            d.launchable = false;
            d.baseColor = new Color(0.75f, 0.2f, 0.35f);
            d.pathHalfWidth = 3f;
            go.SetActive(true);
            summoned.Add(d);
        }
    }

    void StopX()
    {
        if (IsGrounded()) body.velocity = new Vector2(0f, body.velocity.y);
    }

    protected override void Die()
    {
        foreach (var d in summoned) if (d != null) Destroy(d.gameObject);
        summoned.Clear();
        runner.Cancel();
        base.Die();
        OnDefeated?.Invoke();
    }

    public override void ResetEnemy()
    {
        foreach (var d in summoned) if (d != null) Destroy(d.gameObject);
        summoned.Clear();
        runner.Cancel();
        base.ResetEnemy();
        armoured = true;
        state = State.Dormant;
    }
}
