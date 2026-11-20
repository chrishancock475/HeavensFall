using UnityEngine;

/// <summary>Levite energy fence: solid, except to John while he dashes. The slice's ability lock.</summary>
public class DashFence : MonoBehaviour
{
    public float graceAfterDash = 0.12f;
    Collider2D fence;
    Collider2D[] playerCols;
    SpriteRenderer sr;
    bool passable;
    float grace;

    void Start()
    {
        fence = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (PartyController.Instance != null) playerCols = PartyController.Instance.GetComponentsInChildren<Collider2D>();
    }

    void Update()
    {
        var p = PartyController.Instance;
        if (p == null || playerCols == null) return;
        bool dashingJohn = p.ActiveIsJohn && p.IsDashing;
        if (dashingJohn) grace = graceAfterDash; else grace -= Time.deltaTime;
        bool want = dashingJohn || grace > 0f;
        if (want != passable)
        {
            passable = want;
            foreach (var c in playerCols) Physics2D.IgnoreCollision(fence, c, want);
        }
        if (sr != null)
        {
            Color c = new Color(0.3f, 0.9f, 1f, passable ? 0.25f : 0.55f + 0.15f * Mathf.Sin(Time.time * 8f));
            sr.color = c;
        }
    }
}
