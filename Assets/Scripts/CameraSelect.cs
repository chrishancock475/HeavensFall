using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSelect : MonoBehaviour
{
    [SerializeField] private GameObject localCamera; 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        localCamera.SetActive(true);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        localCamera.SetActive(false);
    }
}
