using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The only place that writes Time.timeScale. Pause beats hitstop beats time slow.
/// Slow sources are keyed by object so two systems can't cancel each other by accident.
/// </summary>
public static class TimeController
{
    public const float SlowScale = 0.5f;

    static bool paused;
    static readonly HashSet<object> slowSources = new HashSet<object>();
    static float freeSlowUntil;   // unscaled time (just-dodge reward)
    static float hitstopUntil;    // unscaled time
    static Runner runner;

    public static bool Paused
    {
        get => paused;
        set { paused = value; Apply(); }
    }

    public static bool IsSlowed => slowSources.Count > 0 || Time.unscaledTime < freeSlowUntil;

    public static void SetSlow(object source, bool on)
    {
        if (on) slowSources.Add(source); else slowSources.Remove(source);
        Apply();
    }

    /// <summary>Free slow-motion for a while (real seconds), on top of any held slow.</summary>
    public static void GrantSlow(float realSeconds)
    {
        freeSlowUntil = Mathf.Max(freeSlowUntil, Time.unscaledTime + realSeconds);
        EnsureRunner();
        Apply();
    }

    /// <summary>Freeze the game for a few real frames on a hit.</summary>
    public static void Hitstop(float realSeconds)
    {
        hitstopUntil = Mathf.Max(hitstopUntil, Time.unscaledTime + realSeconds);
        EnsureRunner();
        Apply();
    }

    static void Apply()
    {
        float scale = 1f;
        if (IsSlowed) scale = SlowScale;
        if (Time.unscaledTime < hitstopUntil) scale = 0.02f;
        if (paused) scale = 0f;
        Time.timeScale = scale;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        paused = false;
        slowSources.Clear();
        freeSlowUntil = 0f;
        hitstopUntil = 0f;
        runner = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        slowSources.Clear();
        freeSlowUntil = 0f;
        hitstopUntil = 0f;
        Apply();
    }

    static void EnsureRunner()
    {
        if (runner != null) return;
        var go = new GameObject("[TimeController]");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy;
        runner = go.AddComponent<Runner>();
    }

    /// <summary>Ends hitstop and free slow on time (they are timed in real seconds).</summary>
    class Runner : MonoBehaviour
    {
        bool wasTimed;
        void Update()
        {
            bool timed = Time.unscaledTime < hitstopUntil || Time.unscaledTime < freeSlowUntil;
            if (timed || wasTimed) Apply();
            wasTimed = timed;
        }
    }
}
