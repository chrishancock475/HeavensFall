using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == 8)
        {
            GameManager.checkpoint = transform;
            GameManager.OnCheckPointRecieved?.Invoke(this, new EventArgs());

            Debug.Log("Checkpoint!!");

            GetComponent<BoxCollider2D>().enabled = false;
        }
    }


}
