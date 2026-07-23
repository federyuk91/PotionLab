using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadLevelButton : MonoBehaviour
{
    public Button loadButton;
    public Image[] sprites;
    public Color inactiveColor;

    public void Config(int score, bool unlocked)
    {
        loadButton.interactable = unlocked;
        if (score > 0)
        {
            for (int i = 0; i < score - 1; i++)
            {
                sprites[i].color = Color.white;
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                sprites[i].color = inactiveColor;
            }
        }
    }
}
