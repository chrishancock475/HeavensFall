using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneEnd : MonoBehaviour
{
    
    VideoPlayer player;
    [SerializeField] string Scene;
    [SerializeField] int cutscene_length;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
    }

    private void Update()
    {
        if (player.clockTime > cutscene_length)
        {
            SceneManager.LoadScene(Scene);
        }
    }


}
