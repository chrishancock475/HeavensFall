using UnityEngine;

/// <summary>Spikes in the slice: 1 damage and knockback away from the spike. Pogo-able. (Prologue spikes still one-hit kill.)</summary>
public class DamageHazard : MonoBehaviour, IHittable, IPogoSurface
{
    public int damage = 1;
    public float knockback = 12f;
    public Team Team => Team.Enemy;
    Collider2D col;

    void Awake() => col = GetComponent<Collider2D>();

    void OnTriggerStay2D(Collider2D other)
    {
        var party = PlayerFilter.Party(other);
        if (party == null || party.Invulnerable) return;
        Vector2 centre = other.bounds.center;
        Vector2 closest = col != null ? col.ClosestPoint(centre) : (Vector2)transform.position;
        Vector2 dir = centre - closest;
        dir = dir.sqrMagnitude < 0.0001f ? Vector2.up : dir.normalized;
        party.ReceiveHit(new HitInfo
        {
            attackerTeam = Team.Enemy, damage = damage, knockback = dir * knockback + Vector2.up * 4f,
            sourcePosition = closest, moveName = "Spikes",
        });
    }

    public bool ReceiveHit(HitInfo hit) => false;   // only bounces pogo attacks
}
