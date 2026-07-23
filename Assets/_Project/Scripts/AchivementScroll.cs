using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchivementScroll : MonoBehaviour
{
    public GameObject[] sectionAchivement;
    public GameObject currentSection;
    int index = 0;

    private void Start()
    {
        currentSection = sectionAchivement[index];

    }

    public void ScrollLeft()
    {
        if (index != 0)
        {
            index--;
            currentSection.SetActive(false);
            currentSection = sectionAchivement[index];
            currentSection.SetActive(true);
        }
    }


    public void ScrollRight()
    {
        if (!(index >= sectionAchivement.Length - 1))
        {
            index++;
            currentSection.SetActive(false);
            currentSection = sectionAchivement[index];
            currentSection.SetActive(true);
        }

    }

}
