using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    GameObject pauseMenu;
    public static bool paused;
    private bool unpaused = false;
    void Start()
    {
        pauseMenu = GetComponentInChildren<UniqueClass>().gameObject;
        paused = false;
    }

    
    void Update()
    {
        if (paused)
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
            unpaused = false;
        }
        else if (!unpaused) 
        {
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
            unpaused = true;
        }

        if (paused && Input.GetKeyDown(KeyCode.Escape))
        {
            paused = false;
        }
        else if (!paused && Input.GetKeyDown(KeyCode.Escape))
        {
            paused = true;
        }
    }
}
