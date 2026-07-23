using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrimoireAnimation : MonoBehaviour
{
    public Animator anim;
    public bool isOpen = false;
    public GameObject menuPanel;
    public void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Open_Close()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            Time.timeScale = 0;
            if(GameMan.Instance.cc.cameraShake.shake)
            {
                AchievementManager.instance.Achive("Shaky Shaky");
            }
        }
        else
        {
            Time.timeScale = 1;
        }
        anim.SetBool("IsOpen", isOpen);
    }

    public void ActivatePanel()
    {
        menuPanel.SetActive(true);
    }
    public void DeactivatePanel()
    {
        menuPanel.SetActive(false);
    }

}
