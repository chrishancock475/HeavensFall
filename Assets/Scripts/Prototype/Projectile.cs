using UnityEngine;

/// <summary>Slow enemy shot. Hurts the player, breaks on walls, can be cut down, and can be pogoed.</summary>
public class Projectile : MonoBehaviour, IHittable, IPogoSurface
{
    static Sprite sprite;
    public Vector2 velocity;
    public float life = 4f;
    public Team Team => Team.Enemy;

    public static Projectile Spawn(Vector2 at, Vector2 velocity)
    {
        var go = new GameObject("Shot");
        go.transform.position = at;
        go.transform.localScale = Vector3.one * 0.6f;
        var sr = go.AddComponent<SpriteRenderer>();
        if (sprite == null) sprite = HitboxVisualizer.MakeSquare();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.35f, 0.2f);
        sr.sortingOrder = 6;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        var p = go.AddComponent<Projectile>();
        p.velocity = velocity;
        return p;
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger && other.GetComponentInParent<PartyController>() == null) return;
        var party = other.GetComponentInParent<PartyController>();
        if (party != null)
        {
            party.ReceiveHit(new HitInfo
            {
                attackerTeam = Team.Enemy, damage = 1, knockback = velocity.normalized * 8f + Vector2.up * 4f,
                sourcePosition = transform.position, moveName = "Shot",
            });
            if (!party.IsDashing) Destroy(gameObject);
            return;
        }
        if (other.GetComponentInParent<EnemyBase>() != null) return;
        Destroy(gameObject);   // hit a wall or floor
    }

    public bool ReceiveHit(HitInfo hit)
    {
        if (hit.attackerTeam == Team.Enemy) return false;
        Destroy(gameObject);
        return false;
    }
}
