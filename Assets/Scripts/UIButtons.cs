using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtons : MonoBehaviour
{
    public void Continue()
    {
        PauseMenu.paused = false;
    }

    public void Quit()
    {
        SceneManager.LoadScene("Main_menu");
    }
}
