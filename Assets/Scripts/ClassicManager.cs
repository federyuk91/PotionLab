using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassicManager : MonoBehaviour
{


    public GameObject[] sectionClassic;
    public GameObject currentSection;
    int index =0;

    private void Start()
    {
        currentSection = sectionClassic[index];

    }

    public void ScrollLeft()
    {
        if (index != 0)
        {
            index--;
            currentSection.SetActive(false);
            currentSection = sectionClassic[index];
            currentSection.SetActive(true);
        }
    }


    public void ScrollRight()
    {
        if (!(index >= sectionClassic.Length-1))
        {
            index++;
            currentSection.SetActive(false);
            currentSection = sectionClassic[index];
            currentSection.SetActive(true);
        }
      
    }

}
