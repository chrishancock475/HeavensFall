using UnityEngine;

/// <summary>Debug HUD (IMGUI): HP, gauge, time-slow meter, active character, combo, technique log, room hint, pause menu.</summary>
public class ProtoHud : MonoBehaviour
{
    static string message;
    static float messageUntil;

    public bool showTechCounts = true;
    GUIStyle box, label, big, small;

    public static void Message(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 3.5f;
    }

    void Styles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
        label.normal.textColor = Color.white;
        small = new GUIStyle(label) { fontSize = 13 };
        big = new GUIStyle(label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        box = new GUIStyle(GUI.skin.box);
    }

    void OnGUI()
    {
        Styles();
        var p = PartyController.Instance;
        float x = 12, y = 10;

        if (p != null)
        {
            string hearts = "";
            for (int i = 0; i < p.maxHp; i++) hearts += i < p.Hp ? "<color=#ff5050>■</color> " : "<color=#555555>■</color> ";
            GUI.Label(new Rect(x, y, 400, 24), "HP " + hearts, label); y += 24;
            Bar(new Rect(x, y, 200, 12), p.Gauge, p.Gauge >= 1f ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.6f, 0.1f));
            GUI.Label(new Rect(x + 206, y - 5, 260, 20), p.Gauge >= 1f ? "Combo gauge FULL (I = finisher)" : "Combo gauge", small); y += 18;
            Bar(new Rect(x, y, 200, 12), p.SlowMeter / p.SlowMeterMax, new Color(0.4f, 0.7f, 1f));
            GUI.Label(new Rect(x + 206, y - 5, 260, 20), "Time slow (Rayne, Shift)", small); y += 22;

            string who = "<b>" + p.Active.name + "</b>";
            if (AbilityState.HasJohn)
                who += "   partner " + p.Partner.name + (p.AssistCooldownLeft > 0 ? "  assist " + p.AssistCooldownLeft.ToString("0.0") + "s" : "  assist ready");
            GUI.Label(new Rect(x, y, 500, 24), who, label); y += 26;
        }

        if (ComboTracker.Count > 1)
            GUI.Label(new Rect(Screen.width / 2f - 150, 60, 300, 40), ComboTracker.Count + " HITS", big);

        // technique log, top right
        float rx = Screen.width - 330, ry = 10;
        GUI.Label(new Rect(rx, ry, 320, 22), "<b>Techniques</b>   (best combo " + ComboTracker.Best + ")", label); ry += 22;
        foreach (var e in TechLog.Recent)
        {
            float age = Time.unscaledTime - e.time;
            float a = Mathf.Clamp01(1.5f - age * 0.25f);
            if (a <= 0f) continue;
            var c = GUI.color; GUI.color = new Color(1, 1, 1, a);
            GUI.Label(new Rect(rx, ry, 320, 20), e.text, small);
            GUI.color = c; ry += 18;
        }
        if (showTechCounts && TechLog.Counts.Count > 0)
        {
            ry += 6;
            foreach (var kv in TechLog.Counts) { GUI.Label(new Rect(rx, ry, 320, 18), kv.Key + ": " + kv.Value, small); ry += 16; }
        }

        // room + hint, bottom
        var room = RoomZone.Current;
        if (room != null)
        {
            GUI.Label(new Rect(12, Screen.height - 52, Screen.width - 24, 22), "<b>" + room.roomId + " " + room.roomName + "</b>", label);
            GUI.Label(new Rect(12, Screen.height - 30, Screen.width - 24, 22), room.hint, small);
        }

        if (message != null && Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 40), message, big);

        GUI.Label(new Rect(12, Screen.height - 74, 700, 20),
            "F1 hitboxes  F2 John  F3 heal  F4 gauge  F5 restart  Esc pause", small);

        if (TimeController.Paused) DrawPause();
    }

    void DrawPause()
    {
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float w = 240, h = 40, cx = Screen.width / 2f - w / 2f, cy = Screen.height / 2f - 80;
        GUI.Label(new Rect(0, cy - 50, Screen.width, 40), "Paused", big);
        if (GUI.Button(new Rect(cx, cy, w, h), "Resume")) TimeController.Paused = false;
        if (GUI.Button(new Rect(cx, cy + 50, w, h), "Restart slice")) ProtoGame.Restart();
        if (GUI.Button(new Rect(cx, cy + 100, w, h), "Main menu")) ProtoGame.QuitToMenu();
    }

    static void Bar(Rect r, float t, Color c)
    {
        var old = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
