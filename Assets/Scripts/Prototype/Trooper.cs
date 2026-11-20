using UnityEngine;

/// <summary>Levite Trooper: patrols, winds up 0.6 s when the player is in front, lunges with a slash.</summary>
public class Trooper : EnemyBase
{
    enum State { Patrol, Windup, Lunge, Recover }

    public float patrolSpeed = 3f;
    public Vector2 detectRange = new Vector2(6f, 2.5f);
    public float windup = 0.6f;
    public float lungeSpeed = 10f;
    public float recover = 0.8f;
    public AttackData slash = new AttackData
    {
        name = "Trooper Slash", startup = 0, active = 9, recovery = 0,
        offset = new Vector2(1.2f, 0f), size = new Vector2(1.6f, 1.6f),
        damage = 1, knockback = new Vector2(9f, 5f), hitstun = 0f, locksMovement = false,
    };

    State state;
    float timer;
    AttackRunner runner;

    protected override void Awake()
    {
        base.Awake();
        runner = new AttackRunner(Team.Enemy, transform);
    }

    protected override void TickAI(float dt)
    {
        timer -= dt;
        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (PlayerInFront()) { state = State.Windup; timer = windup; telegraphing = true; Stop(); }
                break;
            case State.Windup:
                Stop();
                if (timer <= 0f)
                {
                    telegraphing = false;
                    state = State.Lunge;
                    timer = slash.Active;
                    body.velocity = new Vector2(facing * lungeSpeed, body.velocity.y);
                    runner.Start(slash);
                }
                break;
            case State.Lunge:
                if (timer <= 0f) { state = State.Recover; timer = recover; runner.Cancel(); }
                break;
            case State.Recover:
                Stop();
                if (timer <= 0f) state = State.Patrol;
                break;
        }
        runner.Tick(dt, Frame());
    }

    protected override void OnHitstunStart()
    {
        runner.Cancel();
        state = State.Recover;
        timer = recover * 0.5f;
    }

    void Patrol()
    {
        if (!IsGrounded()) return;
        Bounds b = cols[0].bounds;
        Vector2 front = new Vector2(facing > 0 ? b.max.x + 0.05f : b.min.x - 0.05f, b.center.y);
        bool wall = Geo.Solid(front, new Vector2(facing, 0), 0.3f, transform);
        bool floorAhead = Geo.Solid(new Vector2(front.x + facing * 0.3f, b.min.y + 0.1f), Vector2.down, 0.6f, transform);
        if (wall || !floorAhead) facing = -facing;
        body.velocity = new Vector2(facing * patrolSpeed, body.velocity.y);
    }

    void Stop()
    {
        if (IsGrounded()) body.velocity = new Vector2(0f, body.velocity.y);
    }

    bool PlayerInFront()
    {
        var p = Player;
        if (p == null) return false;
        Vector2 d = p.position - transform.position;
        if (Mathf.Abs(d.y) > detectRange.y || Mathf.Abs(d.x) > detectRange.x) return false;
        if (Mathf.Sign(d.x) != facing)
        {
            if (Mathf.Abs(d.x) < detectRange.x * 0.5f) facing = (int)Mathf.Sign(d.x); // turn if close behind
            else return false;
        }
        return true;
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        state = State.Patrol;
        runner.Cancel();
    }
}
