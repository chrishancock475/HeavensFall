using UnityEngine;

/// <summary>Trigger zone that opens a ProtoDoor when the player enters (one-way hatch).</summary>
public class DoorTrigger : MonoBehaviour
{
    public ProtoDoor door;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (door != null && PlayerFilter.Party(other) != null) door.TriggerTouched();
    }
}
