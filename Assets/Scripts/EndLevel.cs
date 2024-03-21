using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



public class EndLevel : MonoBehaviour
{
    [SerializeField] string Cutscene;
    private void Start()
    {
        GameManager.OnLevelEnd += CutsceneStart;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 8)
        GameManager.OnLevelEnd?.Invoke(this, new EventArgs());
    }

    void CutsceneStart(object sender, EventArgs e)
    {
        SceneManager.LoadScene(Cutscene); // please figure out how to fix this to work with the variable.
    }

    private void OnDestroy()
    {
        GameManager.OnLevelEnd -= CutsceneStart;
    }
}
