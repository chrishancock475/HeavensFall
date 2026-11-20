using UnityEngine;

/// <summary>Prototype keys (RPG doc layout). Movement, Jump and SuperJump stay on the Input Manager axes.</summary>
public static class ProtoInput
{
    public const KeyCode Attack = KeyCode.U;
    public const KeyCode Heavy = KeyCode.J;
    public const KeyCode Finisher = KeyCode.I;
    public const KeyCode Ability = KeyCode.LeftShift;
    public const KeyCode Swap = KeyCode.O;
    public const KeyCode Pause = KeyCode.Escape;

    static bool Blocked => TimeController.Paused;

    public static bool AttackDown => !Blocked && Input.GetKeyDown(Attack);
    public static bool HeavyDown => !Blocked && Input.GetKeyDown(Heavy);
    public static bool FinisherDown => !Blocked && Input.GetKeyDown(Finisher);
    public static bool AbilityDown => !Blocked && Input.GetKeyDown(Ability);
    public static bool AbilityHeld => !Blocked && Input.GetKey(Ability);
    public static bool SwapDown => !Blocked && Input.GetKeyDown(Swap);
    public static bool SwapHeld => !Blocked && Input.GetKey(Swap);
    public static bool SwapUp => !Blocked && Input.GetKeyUp(Swap);
    public static bool JumpDown => !Blocked && Input.GetButtonDown("Jump");
    public static bool FastFallDown => !Blocked && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow));

    public static Vector2 Move => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    /// <summary>Move input snapped to 8 directions; zero if no input.</summary>
    public static Vector2 Dir8
    {
        get
        {
            Vector2 m = Move;
            float x = Mathf.Abs(m.x) > 0.3f ? Mathf.Sign(m.x) : 0f;
            float y = Mathf.Abs(m.y) > 0.3f ? Mathf.Sign(m.y) : 0f;
            return new Vector2(x, y).normalized;
        }
    }
}

/// <summary>Unlocks for the slice. Reset when the slice starts.</summary>
public static class AbilityState
{
    public static bool HasJohn;
    public static bool MagCoil;

    public static void Reset()
    {
        HasJohn = false;
        MagCoil = false;
    }
}
