using UnityEngine;

/// <summary>Never attacks, refills HP after a pause. For trying combos and techniques.</summary>
public class TrainingDummy : EnemyBase
{
    public float refillDelay = 2f;
    float idle;

    void Reset()
    {
        maxHp = 9999f;
        baseColor = new Color(0.5f, 0.5f, 0.5f);
    }

    protected override void TickAI(float dt)
    {
        idle += dt;
        if (idle > refillDelay) Hp = maxHp;
        if (body != null && IsGrounded()) body.velocity = new Vector2(Mathf.MoveTowards(body.velocity.x, 0f, 40f * dt), body.velocity.y);
    }

    public override bool ReceiveHit(HitInfo hit)
    {
        idle = 0f;
        bool r = base.ReceiveHit(hit);
        if (Hp < 1f) Hp = maxHp;
        return r;
    }

    protected override void Die() { Hp = maxHp; Dead = false; }
}
