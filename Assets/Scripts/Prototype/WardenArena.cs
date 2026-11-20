using UnityEngine;

/// <summary>Starts the Warden fight when the player walks in, locks the doors, drops +1 HP on the win, resets on death.</summary>
public class WardenArena : MonoBehaviour
{
    public WardenBoss warden;
    public ProtoDoor entranceDoor;
    public ProtoDoor exitDoor;
    public ProtoPickup hpReward;

    bool defeated;

    void Start()
    {
        if (hpReward != null) hpReward.gameObject.SetActive(false);
        if (warden != null) warden.OnDefeated += HandleDefeated;
        PartyController.OnPlayerDeath += HandlePlayerDeath;
        if (exitDoor != null) exitDoor.SetOpen(false);
    }

    void OnDestroy()
    {
        PartyController.OnPlayerDeath -= HandlePlayerDeath;
        if (warden != null) warden.OnDefeated -= HandleDefeated;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (defeated || warden == null || warden.Started) return;
        if (PlayerFilter.Party(other) == null) return;
        if (entranceDoor != null) entranceDoor.SetOpen(false);
        warden.Begin();
        ProtoHud.Message("The Levite Warden!");
    }

    void HandleDefeated()
    {
        defeated = true;
        if (entranceDoor != null) entranceDoor.SetOpen(true);
        if (exitDoor != null) exitDoor.SetOpen(true);
        if (hpReward != null)
        {
            hpReward.transform.position = warden.transform.position + Vector3.up;
            hpReward.gameObject.SetActive(true);
        }
        ProtoHud.Message("Warden down. The hatch in the ceiling is open.");
    }

    void HandlePlayerDeath()
    {
        if (defeated || warden == null) return;
        warden.ResetEnemy();
        if (entranceDoor != null) entranceDoor.SetOpen(true);
    }
}
