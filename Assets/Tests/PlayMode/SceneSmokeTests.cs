using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Loads each scene and lets it run. The test runner fails the test on any logged error or exception.
/// Run: Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults msg/smoke_results.xml
/// </summary>
public class SceneSmokeTests
{
    [UnityTest]
    public IEnumerator ProtoSliceRunsWithoutErrors()
    {
        yield return Load("Proto_Slice", 8f);
        Assert.IsNotNull(GameObject.Find("Proto Systems"), "slice systems object missing");
        var player = GameObject.Find("Player");
        Assert.IsNotNull(player, "player missing");
        Assert.Greater(player.transform.position.y, -5f, "player fell out of the world");
    }

    [UnityTest] public IEnumerator MainMenuRuns() { yield return Load("Main_menu", 2f); }
    [UnityTest] public IEnumerator Level1Runs() { yield return Load("Level_1", 4f); }
    [UnityTest] public IEnumerator Level2Runs() { yield return Load("Level_2", 4f); }
    [UnityTest] public IEnumerator EndScreenRuns() { yield return Load("End screen", 2f); }

    static IEnumerator Load(string scene, float seconds)
    {
        Time.timeScale = 1f;
        var op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone) yield return null;
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }
}
