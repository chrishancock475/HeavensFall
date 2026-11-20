using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    GameObject pauseMenu;
    GameObject instructions;
    public static bool paused;
    private bool unpaused = false;

    private void Awake()
    {
        instructions = GameObject.Find("Tutorial_Instructions");
    }
    void Start()
    {
        pauseMenu = GetComponentInChildren<UniqueClass>().gameObject;
        paused = false;

        if (GameManager.LEVEL != 1) instructions.SetActive(false);
    }

    
    void Update()
    {
        if (paused)
        {
            pauseMenu.SetActive(true);
            TimeController.Paused = true;
            unpaused = false;
        }
        else if (!unpaused)
        {
            pauseMenu.SetActive(false);
            TimeController.Paused = false;
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
