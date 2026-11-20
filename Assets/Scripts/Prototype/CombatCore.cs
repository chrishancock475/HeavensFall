using System;
using System.Collections.Generic;
using UnityEngine;

// Shared combat types for the prototype slice. Frame counts are at 60 fps.

public enum Team { Player, Enemy }

public struct HitInfo
{
    public Team attackerTeam;
    public float damage;
    public Vector2 knockback;     // world space
    public float hitstun;         // seconds
    public bool launch;           // light enemies go airborne
    public bool isPogo;           // attacker bounces on contact
    public bool isAirMove;
    public Vector2 sourcePosition;
    public string moveName;
}

/// <summary>Anything an attack can touch. Return true if it counted as a hit.</summary>
public interface IHittable
{
    Team Team { get; }
    bool ReceiveHit(HitInfo hit);
}

/// <summary>Hazards and projectiles the player can bounce off with a pogo without "hitting" them.</summary>
public interface IPogoSurface { }

[Serializable]
public class AttackData
{
    public string name = "Attack";
    public int startup = 5;
    public int active = 3;
    public int recovery = 12;
    [Tooltip("Hitbox centre in the attacker's frame: x = forward, y = up (rotates with the surface).")]
    public Vector2 offset = new Vector2(1.2f, 0f);
    public Vector2 size = new Vector2(1.6f, 1.2f);
    public float damage = 6f;
    [Tooltip("x = forward, y = up, in the attacker's frame.")]
    public Vector2 knockback = new Vector2(4f, 2f);
    public float hitstun = 0.3f;
    public bool launch;
    public bool pogo;
    public bool airMove;
    public bool locksMovement = true;
    [Tooltip("Attacker velocity (forward, up) applied when the active frames start, e.g. a lunge.")]
    public Vector2 selfVelocity;
    [Tooltip("Hits each target once per swing unless this is > 0 (re-hit interval in seconds).")]
    public float rehitInterval;

    public float Startup => startup / 60f;
    public float Active => active / 60f;
    public float Recovery => recovery / 60f;
    public float Total => Startup + Active + Recovery;

    public AttackData Clone() => (AttackData)MemberwiseClone();
}

/// <summary>Where an attack is anchored: position, surface angle and facing.</summary>
public struct AttackFrame
{
    public Vector2 position;
    public float angleDeg;
    public int facing;

    public Vector2 ToWorldPoint(Vector2 local) => position + ToWorldDir(local);

    public Vector2 ToWorldDir(Vector2 local)
    {
        Vector2 v = new Vector2(local.x * facing, local.y);
        float r = angleDeg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}

public enum AttackPhase { None, Startup, Active, Recovery }

/// <summary>Runs one AttackData: startup, active (hitbox checks every frame), recovery.</summary>
public class AttackRunner
{
    public AttackData Data { get; private set; }
    public AttackPhase Phase { get; private set; }
    public float Elapsed { get; private set; }
    public bool HasHit { get; private set; }
    public float DamageScale { get; set; } = 1f;
    public bool Running => Phase != AttackPhase.None;

    public event Action<IHittable, HitInfo> OnHit;
    public event Action OnActiveStart;
    public event Action OnFinished;

    readonly Team team;
    readonly Transform owner;
    readonly Dictionary<IHittable, float> lastHitTime = new Dictionary<IHittable, float>();
    static readonly Collider2D[] results = new Collider2D[32];

    public AttackRunner(Team team, Transform owner)
    {
        this.team = team;
        this.owner = owner;
    }

    public void Start(AttackData data)
    {
        Data = data;
        Phase = AttackPhase.Startup;
        Elapsed = 0f;
        HasHit = false;
        DamageScale = 1f;
        lastHitTime.Clear();
    }

    public void Cancel()
    {
        if (Phase == AttackPhase.None) return;
        Phase = AttackPhase.None;
        OnFinished?.Invoke();
    }

    /// <summary>Call every frame with scaled delta time.</summary>
    public void Tick(float dt, AttackFrame frame)
    {
        if (Phase == AttackPhase.None) return;
        Elapsed += dt;

        if (Phase == AttackPhase.Startup && Elapsed >= Data.Startup)
        {
            Phase = AttackPhase.Active;
            OnActiveStart?.Invoke();
        }
        if (Phase == AttackPhase.Active)
        {
            CheckHits(frame);
            if (Elapsed >= Data.Startup + Data.Active) Phase = AttackPhase.Recovery;
        }
        if (Phase == AttackPhase.Recovery && Elapsed >= Data.Total)
        {
            Phase = AttackPhase.None;
            OnFinished?.Invoke();
        }
    }

    void CheckHits(AttackFrame frame)
    {
        Vector2 centre = frame.ToWorldPoint(Data.offset);
        HitboxVisualizer.Show(centre, Data.size, frame.angleDeg, team == Team.Player);
        int n = Physics2D.OverlapBoxNonAlloc(centre, Data.size, frame.angleDeg, results);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = results[i];
            if (c == null || (owner != null && c.transform.IsChildOf(owner))) continue;
            IHittable target = c.GetComponentInParent<IHittable>();
            if (target == null || target.Team == team) continue;
            if (lastHitTime.TryGetValue(target, out float t))
            {
                if (Data.rehitInterval <= 0f || Elapsed - t < Data.rehitInterval) continue;
            }
            lastHitTime[target] = Elapsed;

            var hit = new HitInfo
            {
                attackerTeam = team,
                damage = Data.damage * DamageScale,
                knockback = frame.ToWorldDir(Data.knockback) * Mathf.Lerp(1f, DamageScale, 0.5f),
                hitstun = Data.hitstun,
                launch = Data.launch,
                isPogo = Data.pogo,
                isAirMove = Data.airMove,
                sourcePosition = frame.position,
                moveName = Data.name,
            };
            bool counted = target.ReceiveHit(hit);
            if (counted || target is IPogoSurface)
            {
                HasHit = HasHit || counted;
                OnHit?.Invoke(target, hit);
            }
        }
    }
}

/// <summary>Draws active hitboxes in the Game view (F1 toggles). Red = player, orange = enemy.</summary>
public static class HitboxVisualizer
{
    public static bool Enabled = true;
    static Sprite square;
    static readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    static int frameUsed = -1;
    static int used;

    public static void Show(Vector2 centre, Vector2 size, float angle, bool player)
    {
        if (!Enabled) return;
        if (frameUsed != Time.frameCount) { Hide(); frameUsed = Time.frameCount; }
        if (square == null) square = MakeSquare();
        while (pool.Count <= used || pool[used] == null)
        {
            if (pool.Count > used && pool[used] == null) pool.RemoveAt(used);
            var go = new GameObject("[Hitbox]");
            go.hideFlags = HideFlags.HideInHierarchy;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.sortingOrder = 100;
            pool.Add(sr);
        }
        var r = pool[used++];
        r.gameObject.SetActive(true);
        r.transform.SetPositionAndRotation(centre, Quaternion.Euler(0, 0, angle));
        r.transform.localScale = new Vector3(size.x, size.y, 1);
        r.color = player ? new Color(1f, 0.2f, 0.2f, 0.35f) : new Color(1f, 0.6f, 0f, 0.35f);
    }

    /// <summary>Hides boxes from earlier frames. Called by ProtoGame.LateUpdate.</summary>
    public static void EndFrame()
    {
        if (frameUsed != Time.frameCount) Hide();
    }

    static void Hide()
    {
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false);
        used = 0;
    }

    public static Sprite MakeSquare()
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}

/// <summary>Log of advanced techniques for the HUD and for tuning.</summary>
public static class TechLog
{
    public struct Entry { public string text; public float time; }
    public static readonly List<Entry> Recent = new List<Entry>();
    public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

    public static void Report(string tech, string detail = null)
    {
        Counts.TryGetValue(tech, out int c);
        Counts[tech] = c + 1;
        Recent.Insert(0, new Entry { text = detail == null ? tech : tech + "  " + detail, time = Time.unscaledTime });
        if (Recent.Count > 6) Recent.RemoveAt(Recent.Count - 1);
    }

    public static void Clear()
    {
        Recent.Clear();
        Counts.Clear();
    }
}

/// <summary>Counts hits while at least one enemy is still in hitstun.</summary>
public static class ComboTracker
{
    public static int Count { get; private set; }
    public static int Best { get; private set; }
    static float lastHitTime;
    const float Grace = 0.35f;

    public static bool InCombo => Count > 0 && (EnemyBase.AnyInHitstun || Time.time - lastHitTime < Grace);

    public static void RegisterHit()
    {
        if (!InCombo) Count = 0;
        Count++;
        Best = Mathf.Max(Best, Count);
        lastHitTime = Time.time;
    }

    public static void Tick()
    {
        if (Count > 0 && !InCombo) Count = 0;
    }

    public static void Reset()
    {
        Count = 0;
        Best = 0;
    }
}

public static class Geo
{
    static readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    /// <summary>First solid (non-trigger) collider along the ray that is not the caller, an enemy or the player.</summary>
    public static bool Solid(Vector2 origin, Vector2 dir, float dist, Transform self, out RaycastHit2D hit)
    {
        int n = Physics2D.RaycastNonAlloc(origin, dir, hits, dist);
        float best = float.MaxValue;
        hit = default;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            if (self != null && h.collider.transform.IsChildOf(self)) continue;
            if (h.collider.GetComponentInParent<EnemyBase>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (h.distance < best) { best = h.distance; hit = h; found = true; }
        }
        return found;
    }

    public static bool Solid(Vector2 origin, Vector2 dir, float dist, Transform self) => Solid(origin, dir, dist, self, out _);
}
