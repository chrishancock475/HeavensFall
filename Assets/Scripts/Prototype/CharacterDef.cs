using System;
using UnityEngine;

/// <summary>One party member: mag boots or not, tint, and moveset. Defaults are the spec values; tune in the Inspector.</summary>
[Serializable]
public class CharacterDef
{
    public string name = "Rayne";
    public Color tint = Color.white;
    public bool magSwitch = true;
    public AttackData[] groundString;
    public AttackData launcher;
    public AttackData air;
    public AttackData pogo;
    public AttackData heavy;
    public AttackData finisher;
    [Tooltip("What this character does when called in as an assist.")]
    public AttackData assist;

    static AttackData A(string name, int s, int a, int r, Vector2 off, Vector2 size, float dmg, Vector2 kb, float stun)
        => new AttackData { name = name, startup = s, active = a, recovery = r, offset = off, size = size, damage = dmg, knockback = kb, hitstun = stun };

    public static CharacterDef Rayne()
    {
        var d = new CharacterDef { name = "Rayne", tint = Color.white, magSwitch = true };
        d.groundString = new[]
        {
            A("Knife 1", 4, 3, 10, new Vector2(1.3f, 0.1f), new Vector2(1.6f, 1.2f), 6, new Vector2(3, 1), 0.30f),
            A("Knife 2", 5, 3, 12, new Vector2(1.3f, 0.1f), new Vector2(1.7f, 1.2f), 7, new Vector2(3, 1), 0.32f),
            A("Knife 3", 7, 4, 18, new Vector2(1.4f, 0.2f), new Vector2(2.0f, 1.4f), 10, new Vector2(8, 4), 0.45f),
        };
        d.launcher = A("Rising Knife", 7, 4, 18, new Vector2(0.6f, 1.5f), new Vector2(1.6f, 1.8f), 8, new Vector2(1, 14), 0.6f);
        d.launcher.launch = true;
        d.air = A("Air Knife", 5, 4, 12, new Vector2(1.2f, 0f), new Vector2(1.8f, 1.4f), 7, new Vector2(4, 3), 0.35f);
        d.air.airMove = true; d.air.locksMovement = false;
        d.pogo = A("Pogo Knife", 5, 5, 10, new Vector2(0f, -1.4f), new Vector2(1.4f, 1.4f), 7, new Vector2(0, -3), 0.3f);
        d.pogo.pogo = true; d.pogo.airMove = true; d.pogo.locksMovement = false;
        d.heavy = A("Mag Pulse", 10, 4, 20, new Vector2(1.5f, 0f), new Vector2(2.6f, 2.0f), 12, new Vector2(14, 4), 0.5f);
        d.finisher = A("Mag Burst", 12, 6, 30, new Vector2(0f, 0f), new Vector2(5f, 5f), 35, new Vector2(10, 8), 0.8f);
        d.assist = A("Assist: Mag Pulse", 8, 4, 10, new Vector2(1.5f, 0f), new Vector2(2.6f, 2.0f), 10, new Vector2(12, 6), 0.6f);
        return d;
    }

    public static CharacterDef John()
    {
        var d = new CharacterDef { name = "John", tint = new Color(0.78f, 0.82f, 0.55f), magSwitch = false };
        d.groundString = new[]
        {
            A("Rifle Butt 1", 6, 4, 14, new Vector2(1.6f, 0.1f), new Vector2(2.2f, 1.3f), 9, new Vector2(6, 2), 0.38f),
            A("Rifle Butt 2", 9, 5, 22, new Vector2(1.7f, 0.2f), new Vector2(2.4f, 1.5f), 14, new Vector2(12, 5), 0.5f),
        };
        d.launcher = A("Stock Uppercut", 8, 4, 20, new Vector2(0.8f, 1.5f), new Vector2(1.8f, 2.0f), 10, new Vector2(1, 15), 0.65f);
        d.launcher.launch = true;
        d.air = A("Air Swing", 6, 5, 14, new Vector2(1.4f, -0.2f), new Vector2(2.2f, 1.6f), 9, new Vector2(5, 3), 0.4f);
        d.air.airMove = true; d.air.locksMovement = false;
        d.pogo = A("Bayonet Drop", 6, 5, 12, new Vector2(0f, -1.5f), new Vector2(1.4f, 1.6f), 9, new Vector2(0, -3), 0.35f);
        d.pogo.pogo = true; d.pogo.airMove = true; d.pogo.locksMovement = false;
        d.heavy = A("Bayonet Lunge", 8, 10, 18, new Vector2(1.2f, 0f), new Vector2(2.2f, 1.4f), 14, new Vector2(10, 3), 0.5f);
        d.heavy.selfVelocity = new Vector2(20f, 0f);
        d.finisher = A("Rifle Shot", 14, 3, 26, new Vector2(6f, 0.2f), new Vector2(12f, 0.8f), 40, new Vector2(14, 3), 0.8f);
        d.assist = A("Assist: Bayonet Lunge", 8, 10, 8, new Vector2(1.2f, 0f), new Vector2(2.2f, 1.4f), 12, new Vector2(10, 3), 0.6f);
        d.assist.selfVelocity = new Vector2(18f, 0f);
        return d;
    }
}
