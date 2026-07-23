using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ElementUI : MonoBehaviour
{
    public string title, info;
    public Text _name;
    public Text description;
    public Image image;
    public UnityEvent click;

    public GameObject descriptionPanel;

    public void Show()
    {
        _name.text = title;
        description.text = info;
        descriptionPanel.SetActive(true);
    }
    public void Hide()
    {
        descriptionPanel.SetActive(false);
    }

    public void Click()
    {
        click?.Invoke();
    }


}
