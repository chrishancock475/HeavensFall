using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class EndLevel : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 8)
        GameManager.OnLevelEnd?.Invoke(this, new EventArgs());
    }
}
