using UnityEngine;

/// <summary>HP upgrade, Mag Coil, or John joining the party.</summary>
public class ProtoPickup : MonoBehaviour
{
    public enum Kind { HpUpgrade, MagCoil, JohnJoins }
    public Kind kind;

    void Update()
    {
        // gentle bob so pickups read as pickups
        if (kind != Kind.JohnJoins)
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f) * 10f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var party = PlayerFilter.Party(other);
        if (party == null) return;
        switch (kind)
        {
            case Kind.HpUpgrade:
                party.AddMaxHp(1);
                ProtoHud.Message("HP up! Max HP " + party.maxHp + ".");
                break;
            case Kind.MagCoil:
                AbilityState.MagCoil = true;
                ProtoHud.Message("Mag Coil: time-slow meter +50%.");
                break;
            case Kind.JohnJoins:
                AbilityState.HasJohn = true;
                ProtoHud.Message("John joins! O tap = swap, O hold = assist. As John, Shift = air dash.");
                break;
        }
        gameObject.SetActive(false);
    }
}
