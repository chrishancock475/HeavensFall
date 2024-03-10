using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static Transform checkpoint;

    public static EventHandler<EventArgs> OnCheckPointRecieved;
    public static EventHandler<EventArgs> OnLevelEnd;


    private void Awake()
    {
        OnCheckPointRecieved += SetNewCheckPoint;
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
}
