using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using static Cinemachine.CinemachineCore;

public class GameManager : MonoBehaviour
{
    public static Transform checkpoint;

    public static EventHandler<EventArgs> OnCheckPointRecieved;
    public static EventHandler<EventArgs> OnLevelEnd;

    [SerializeField] int level;

    public static int LEVEL;

    public static float time;
    public static int deathCount;


    private void Awake()
    {
        OnCheckPointRecieved += SetNewCheckPoint;
        LEVEL = level;
    }

    /// <summary>
    /// Attempts to load the next level. Loads the main menu if there is no next level. Called by the OnLevelEnd event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>


    public static void LoadNextLevel() // This is for the menu button
    {
        OnLevelEnd?.Invoke(new object(), new EventArgs());
    }

    public static void SetNewCheckPoint(object sender, EventArgs e)
    {
        // Play the checkpoint sound and handle other stuff that might happen when the player receives a checkpoint
    }

    public static void Quit()
    {
        Application.Quit();
        Debug.Log("Quit");
    }

    private void OnDestroy()
    {
        OnCheckPointRecieved -= SetNewCheckPoint;
    }

    private void Update()
    {
        time += Time.deltaTime;
    }

    #region CM LOGIC
    private static CinemachineBlendDefinition? nextBlend;
    static GameManager()
    {
        CinemachineCore.GetBlendOverride = GetBlendOverrideDelegate;
    }
    public static void ClearNextBlend()
    {
        nextBlend = null;
    }

    public static void SetNextBlend(CinemachineBlendDefinition blend)
    {
        nextBlend = blend;
    }

    public static CinemachineBlendDefinition GetBlendOverrideDelegate(ICinemachineCamera fromVcam, ICinemachineCamera toVcam, CinemachineBlendDefinition defaultBlend, MonoBehaviour owner)
    {
        if (nextBlend.HasValue)
        {
            var blend = nextBlend.Value;
            nextBlend = null;
            return blend;
        }
        return defaultBlend;
    }
    #endregion
}
