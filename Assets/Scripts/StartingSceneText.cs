using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartingSceneText : MonoBehaviour
{

    public GameObject level, thisGameObject;
    public Text dialogtext;
    public Animation dialogAnimator;

    public string[] dialogArray;
    public string[] dialogArray2;


    public void WriteStartDialog()
    {
        dialogAnimator.Play();
        dialogtext.text = dialogArray[Random.Range(0, dialogArray.Length-1)];
    }

    public void WriteMidDialog()
    {
        dialogAnimator.Play();
        dialogtext.text = dialogArray2[Random.Range(0, dialogArray2.Length - 1)];
    }

    public void ActivateScene()
    {
        level.SetActive(true);
        thisGameObject.SetActive(false);
    }
}
