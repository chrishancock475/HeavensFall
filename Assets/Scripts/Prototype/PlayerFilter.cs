using UnityEngine;

public static class PlayerFilter
{
    /// <summary>The party on this collider, or null. Works for the body and the hitbox child.</summary>
    public static PartyController Party(Collider2D c) => c == null ? null : c.GetComponentInParent<PartyController>();
}
