using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class timeDisplay : MonoBehaviour
{
    TextMeshProUGUI tmp;
    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        int minutes = ((int)GameManager.time) / 60;
        tmp.text += $"\nYou completed the game in {minutes} minutes and {GameManager.time % 60} seconds";
        tmp.text += $"\nYou died {GameManager.deathCount} times";
    }
}
