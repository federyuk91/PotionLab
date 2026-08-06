using System.Collections;
using System.Collections.Generic;
using Refactory.UI.GridList;
using UnityEngine;

public class GrimoireAnimation : MonoBehaviour
{
    public Animator anim;
    public bool isOpen = false;
    public GameObject menuPanel;
    [SerializeField] private CompendiumView compendiumView;

    private bool missingCompendiumViewWarningShown;
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
            if(GameMan.Instance!=null && GameMan.Instance.cc.cameraShake.shake)
            {
                AchievementManager.instance.Achive("Shaky Shaky");
            }
        }
        else
        {
            if (compendiumView != null)
            {
                compendiumView.CloseWithFade();
            }
            else if (!missingCompendiumViewWarningShown)
            {
                missingCompendiumViewWarningShown = true;
                Debug.LogWarning($"{name}: Compendium View reference is missing. Assign it in Inspector to fade the grimoire when closing.", this);
            }

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
        if (compendiumView != null)
        {
            compendiumView.CloseWithFade();
            return;
        }

        menuPanel.SetActive(false);
    }

}
