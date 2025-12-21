using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerScript : MonoBehaviour
{

    public Animator animator;
    public int status = 0;
    public AudioSource audioSource;

    public void Grow()
    {
        if(status == 0)
        {
            status++;
            animator.SetInteger("status", status);
            audioSource.Play();
        }
       
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Potion"))
        {
            if(other.gameObject.GetComponent<PotionScript>().potion.effectType == PotionScriptable.EffectType.lava ||
            other.gameObject.GetComponent<PotionScript>().potion.effectType == PotionScriptable.EffectType.poisoned ||
            other.gameObject.GetComponent<PotionScript>().potion.effectType == PotionScriptable.EffectType.freezed ||
            other.gameObject.GetComponent<PotionScript>().potion.effectType == PotionScriptable.EffectType.burned)
            {
                if (status == 0)
                {
                    other.gameObject.SetActive(false);
                    GameMan.Instance.RemovePotion(other.gameObject.GetComponent<PotionScript>(), true);
                    Destroy(other.gameObject);

                    GameMan.Instance.PopDialog("Bye my friend", 2f);

                    DestroyFlowers();
                    GameMan.Instance.cc.spellManager.bloom = false;
                }
                else if (status == 1)
                {
                    other.gameObject.SetActive(false);
                    GameMan.Instance.RemovePotion(other.gameObject.GetComponent<PotionScript>(), true);
                    Destroy(other.gameObject);
                    status++; GameMan.Instance.PopDialog("My precious child! :(", 2f);
                    animator.SetInteger("status", status);
                    return;
                }
                else if (status == 2)
                {
                    other.gameObject.SetActive(false);
                    GameMan.Instance.RemovePotion(other.gameObject.GetComponent<PotionScript>(), true);
                    Destroy(other.gameObject);
                    GameMan.Instance.PopDialog("I will miss you! ", 2f);
                    GameMan.Instance.cc.spellManager.bloom = false;
                    DestroyFlowers();
                }
                audioSource.Play();
            }
            else
            {
                //GameMan.Instance.cc.ApplyEffect(other.gameObject.GetComponent<PotionScript>());
            }

        } 
    }

    public void DestroyFlowers()
    {
        Destroy(gameObject, 0.1f);

    }
}
