using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDestroy : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.M))
        {
            Destroy(gameObject);
        }
    }
}
