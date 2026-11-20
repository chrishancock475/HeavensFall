using UnityEngine;

/// <summary>Full heal and respawn point. The one marked initial is the respawn point from the start.</summary>
public class SavePoint : MonoBehaviour
{
    public bool initial;
    public Transform spawn;

    void Awake()
    {
        if (spawn == null) spawn = transform;
        if (initial) GameManager.checkpoint = spawn;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var party = PlayerFilter.Party(other);
        if (party == null) return;
        party.HealFull();
        bool changed = GameManager.checkpoint != spawn;
        GameManager.checkpoint = spawn;
        if (changed) ProtoHud.Message("Saved. HP restored.");
    }
}
