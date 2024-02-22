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
        OnLevelEnd += NextLevel;
        OnCheckPointRecieved += SetNewCheckPoint;
    }

    /// <summary>
    /// Attempts to load the next level. Loads the main menu if there is no next level. Called by the OnLevelEnd event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void NextLevel(object sender, EventArgs e)
    {
        // This will need a rework to scale for more scenes because scene count only appears to count the loaded scenes so we must load the scenes async to make that work
        if (SceneManager.GetActiveScene().buildIndex < SceneManager.sceneCount)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        else
            SceneManager.LoadScene("Main_menu");
    }

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
        OnLevelEnd -= NextLevel;
        OnCheckPointRecieved -= SetNewCheckPoint;
    }
}
