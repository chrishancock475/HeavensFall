using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    GameObject pauseMenu;
    public static bool paused;
    void Start()
    {
        pauseMenu = GetComponentInChildren<Canvas>().gameObject;
        paused = false;
    }

    
    void Update()
    {
        if (paused)
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
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
