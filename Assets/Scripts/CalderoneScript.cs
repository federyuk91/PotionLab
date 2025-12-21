using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalderoneScript : MonoBehaviour
{
    public string sentenceForPotion = "BURN!";
    public bool burst = false;
    public Animator animator;
    public AudioSource audioSource;
    private int consecutivePotion = 0;
    private int nextPotion = 0;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Potion"))
        {
            
            collision.gameObject.SetActive(false);
            if(collision.gameObject.GetComponent<PotionScript>().potion.effectType == PotionScriptable.EffectType.magicUP)
            {
                consecutivePotion++;
                if(consecutivePotion == 5)
                {
                    
                   AchievementManager.instance.Achive("Mana BURN!");
                }
            } else {

                nextPotion++;
                if(nextPotion == 5) {

                    GameMan.Instance.SetUpLightLevel();
                }
                    
            }
            GameMan.Instance.PopDialog(sentenceForPotion, 3f);
            //Questa pozione non contribuirà al punteggio
            if (burst)
            {
                animator.SetTrigger("burst");
            }

            audioSource.Play();
            GameMan.Instance.RemovePotion(collision.gameObject.GetComponent<PotionScript>(), true);
            Destroy(collision.gameObject);
        }
       
    }


    private void OnDisable()
    {
        consecutivePotion = 0;
        nextPotion = 0;
    }
}
