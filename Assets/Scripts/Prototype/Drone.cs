using UnityEngine;

/// <summary>Levite Drone: flies a fixed back-and-forth path, telegraphs, then fires a slow shot at the player.</summary>
public class Drone : EnemyBase
{
    public float pathHalfWidth = 4f;
    public float speed = 2f;
    public float fireInterval = 2.4f;
    public float telegraph = 0.5f;
    public float shotSpeed = 6f;
    public float range = 14f;

    float cycle;
    float pathT;
    Vector2 aim;
    Vector2 knock;
    Vector3 home;

    void Reset()
    {
        maxHp = 20f;
        launchable = false;
        baseColor = new Color(0.75f, 0.2f, 0.35f);
    }

    protected override void Awake()
    {
        base.Awake();
        home = transform.position;
        cycle = fireInterval;
    }

    protected override void TickAI(float dt)
    {
        pathT += dt * speed;
        float x = Mathf.PingPong(pathT, pathHalfWidth * 2f) - pathHalfWidth;
        knock = Vector2.MoveTowards(knock, Vector2.zero, 20f * dt);
        transform.position = home + new Vector3(x, 0f, 0f) + (Vector3)knock;
        facing = DirToPlayer();

        var p = Player;
        bool inRange = p != null && Vector2.Distance(p.position, transform.position) < range;
        cycle -= dt;
        if (!telegraphing && cycle <= telegraph && inRange)
        {
            telegraphing = true;
            aim = (Vector2)p.position;
        }
        if (cycle <= 0f)
        {
            if (telegraphing && inRange) Fire();
            telegraphing = false;
            cycle = fireInterval;
        }
    }

    protected override void TickHitstun(float dt)
    {
        knock = Vector2.MoveTowards(knock, Vector2.zero, 20f * dt);
        transform.position = home + new Vector3(Mathf.PingPong(pathT, pathHalfWidth * 2f) - pathHalfWidth, 0f, 0f) + (Vector3)knock;
    }

    public override bool ReceiveHit(HitInfo hit)
    {
        bool r = base.ReceiveHit(hit);
        if (r)
        {
            knock += hit.knockback.normalized * 0.6f;
            cycle = fireInterval;   // getting hit resets the shot
        }
        return r;
    }

    void Fire()
    {
        Vector2 dir = (aim - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.down;
        Projectile.Spawn(transform.position, dir * shotSpeed);
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        pathT = 0f;
        knock = Vector2.zero;
        cycle = fireInterval;
    }
}
