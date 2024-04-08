
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtons : MonoBehaviour
{
    [SerializeField] string playScene;
    #nullable enable
    GameObject? menu;


    private void Awake()
    {
        if (transform.parent.GetComponentInChildren<UniqueClass>() != null)
        menu = transform.parent.GetComponentInChildren<UniqueClass>().gameObject;
    }

    private void Start()
    {
        if (gameObject.name == "Menu_Credits")
        gameObject.SetActive(false);
    }

    public void Continue()
    {
        PauseMenu.paused = false;
    }

    public void Quit()
    {
        GameManager.LEVEL = 0;
        SceneManager.LoadScene("Main_menu");
    }

    public void Play()
    {
        SceneManager.LoadScene(playScene);
    }

    public void Instructions()
    {
        PauseMenu.paused = false;
        gameObject.SetActive(true);
        if (menu != null) menu.SetActive(false);
    }

    public void Credits()
    {
        PauseMenu.paused = false;
        gameObject.SetActive(true);
        if (menu != null) menu.SetActive(false);
    }
}
