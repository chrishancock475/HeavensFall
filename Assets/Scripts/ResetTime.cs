using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetTime : MonoBehaviour
{
    private void Awake()
    {
        GameManager.deathCount = 0;
        GameManager.time = 0;
    }
}
