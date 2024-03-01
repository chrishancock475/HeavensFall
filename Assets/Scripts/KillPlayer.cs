using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlayer : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 9)
        {
            PlayerMovement player = collision.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                player.Die();
            }
        }
    }
}
