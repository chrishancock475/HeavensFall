using UnityEngine;

/// <summary>Solid door. Arena doors are opened and closed by WardenArena; a hatch opens when the player touches its trigger.</summary>
public class ProtoDoor : MonoBehaviour
{
    public bool startOpen = true;
    public string openMessage = "";

    Collider2D solid;
    SpriteRenderer sr;
    public bool IsOpen { get; private set; }

    void Awake()
    {
        solid = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        SetOpen(startOpen);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (solid != null) solid.enabled = !open;
        if (sr != null) sr.enabled = !open;
    }

    /// <summary>Called by DoorTrigger on the trigger object.</summary>
    public void TriggerTouched()
    {
        if (IsOpen) return;
        SetOpen(true);
        if (!string.IsNullOrEmpty(openMessage)) ProtoHud.Message(openMessage);
    }
}
