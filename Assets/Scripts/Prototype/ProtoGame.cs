using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Slice bootstrap: resets unlocks and logs, enables super jump, debug keys.
/// F1 hitboxes, F2 John joins, F3 full HP, F4 full gauge, F5 restart slice.
/// </summary>
public class ProtoGame : MonoBehaviour
{
    void Awake()
    {
        AbilityState.Reset();
        TechLog.Clear();
        ComboTracker.Reset();
        GameManager.LEVEL = 2;          // super jump on (PlayerMovement gates it on LEVEL > 1)
        TimeController.Paused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(ProtoInput.Pause)) TimeController.Paused = !TimeController.Paused;
        if (TimeController.Paused) return;

        if (Input.GetKeyDown(KeyCode.F1)) HitboxVisualizer.Enabled = !HitboxVisualizer.Enabled;
        if (Input.GetKeyDown(KeyCode.F2)) { AbilityState.HasJohn = true; ProtoHud.Message("Debug: John joined."); }
        if (Input.GetKeyDown(KeyCode.F3) && PartyController.Instance != null) PartyController.Instance.HealFull();
        if (Input.GetKeyDown(KeyCode.F4) && PartyController.Instance != null) PartyController.Instance.DebugFillGauge();
        if (Input.GetKeyDown(KeyCode.F5)) Restart();
    }

    void LateUpdate()
    {
        HitboxVisualizer.EndFrame();
        ComboTracker.Tick();
    }

    public static void Restart()
    {
        TimeController.Paused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void QuitToMenu()
    {
        TimeController.Paused = false;
        GameManager.LEVEL = 0;
        SceneManager.LoadScene("Main_menu");
    }
}
