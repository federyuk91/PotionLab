using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PotionCustom : MonoBehaviour
{

    public GameObject firePotion, lavaPotion, grassPotion, antiMagicPotion, icePotion, healthPotion;
    public Button Button1, Button2, Button3, Button4, Button5, Button6;
    public Animator button1IMG, button2IMG, button3IMG, button4IMG, button5IMG, button6IMG;

    public RuntimeAnimatorController[] baseAnimator;
    public RuntimeAnimatorController[] altAnimator;

    private void Start()
    {
        if (AchievementManager.d_achievements["Casual Drinker"].unlocked)
        {
            Button1.interactable = true;
            button1IMG.gameObject.SetActive(true);
        }
        if (AchievementManager.d_achievements["Regular Drinker"].unlocked)
        {
            Button2.interactable = true;
            button2IMG.gameObject.SetActive(true);
        }
        if (AchievementManager.d_achievements["Hardcore Drinker"].unlocked)
        {
            Button3.interactable = true;
            button3IMG.gameObject.SetActive(true);
        }
        if (AchievementManager.d_achievements["Master Drinker"].unlocked)
        {
            Button4.interactable = true;
            button4IMG.gameObject.SetActive(true);
        }
        if (AchievementManager.d_achievements["Almost a problem...Drinker"].unlocked)
        {
            Button5.interactable = true;
            button5IMG.gameObject.SetActive(true);
        }

        if (AchievementManager.d_achievements["God of Libations!"].unlocked)
        {
            Button6.interactable = true;
            button6IMG.gameObject.SetActive(true);
        }
    }

    public void ChangeFireSkin()
    {
        if (firePotion.GetComponent<Animator>().runtimeAnimatorController.Equals(altAnimator[0]))
        {
            firePotion.GetComponent<Animator>().runtimeAnimatorController = baseAnimator[0];
            button1IMG.runtimeAnimatorController = baseAnimator[0];
            Button1.GetComponent<Image>().color = Color.white;

        } else
        {
            firePotion.GetComponent<Animator>().runtimeAnimatorController = altAnimator[0];
            button1IMG.runtimeAnimatorController = altAnimator[0];
            Button1.GetComponent<Image>().color = Color.black;
        }
        
    }

    public void ChangeVenomSkin()
    {
        if (lavaPotion.GetComponent<Animator>().runtimeAnimatorController.Equals(altAnimator[1]))
        {
            lavaPotion.GetComponent<Animator>().runtimeAnimatorController = baseAnimator[1];
            button2IMG.runtimeAnimatorController = baseAnimator[1];
            Button2.GetComponent<Image>().color = Color.white;

        }
        else
        {
            lavaPotion.GetComponent<Animator>().runtimeAnimatorController = altAnimator[1];
            button2IMG.runtimeAnimatorController = altAnimator[1];
            Button2.GetComponent<Image>().color = Color.black;
        }

    }

    public void ChangeGrassSkin()
    {
        if (grassPotion.GetComponent<Animator>().runtimeAnimatorController.Equals(altAnimator[2]))
        {
            grassPotion.GetComponent<Animator>().runtimeAnimatorController = baseAnimator[2];
            button3IMG.runtimeAnimatorController = baseAnimator[2];
            Button3.GetComponent<Image>().color = Color.white;

        }
        else
        {
            grassPotion.GetComponent<Animator>().runtimeAnimatorController = altAnimator[2];
            button3IMG.runtimeAnimatorController = altAnimator[2];
            Button3.GetComponent<Image>().color = Color.black;
        }

    }

    public void ChangeAntiSkin()
    {
        if (antiMagicPotion.GetComponent<Animator>().runtimeAnimatorController.Equals(altAnimator[3]))
        {
            antiMagicPotion.GetComponent<Animator>().runtimeAnimatorController = baseAnimator[3];
            button4IMG.runtimeAnimatorController = baseAnimator[3];
            Button4.GetComponent<Image>().color = Color.white;

        }
        else
        {
            antiMagicPotion.GetComponent<Animator>().runtimeAnimatorController = altAnimator[3];
            button4IMG.runtimeAnimatorController = altAnimator[3];
            Button4.GetComponent<Image>().color = Color.black;
        }

    }

    public void ChangeIceSkin()
    {
        if (icePotion.GetComponent<Animator>().runtimeAnimatorController.Equals(altAnimator[4]))
        {
            icePotion.GetComponent<Animator>().runtimeAnimatorController = baseAnimator[4];
            button5IMG.runtimeAnimatorController = baseAnimator[4];
            Button5.GetComponent<Image>().color = Color.white;

        }
        else
        {
            icePotion.GetComponent<Animator>().runtimeAnimatorController = altAnimator[4];
            button5IMG.runtimeAnimatorController = altAnimator[4];
            Button5.GetComponent<Image>().color = Color.black;
        }

    }

    public void ChangeHealthSkin()
    {
        if (healthPotion.GetComponent<Animator>().runtimeAnimatorController.Equals(altAnimator[5]))
        {
            healthPotion.GetComponent<Animator>().runtimeAnimatorController = baseAnimator[5];
            button6IMG.runtimeAnimatorController = baseAnimator[5];
            Button6.GetComponent<Image>().color = Color.white;

        }
        else
        {
            healthPotion.GetComponent<Animator>().runtimeAnimatorController = altAnimator[5];
            button6IMG.runtimeAnimatorController = altAnimator[5];
            Button6.GetComponent<Image>().color = Color.black;
        }

    }

}
