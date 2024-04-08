using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneEnd : MonoBehaviour
{
    
    VideoPlayer player;
    [SerializeField] string Scene;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
    }

    private void Update()
    {
        if (player.clockTime > 38)
        {
            SceneManager.LoadScene(Scene);
        }
    }


}
