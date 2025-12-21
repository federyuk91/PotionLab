using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaEffectFamiliar : MonoBehaviour
{

    public AudioSource au;
    public GameObject potionPrefab;

    public enum Effect
    {
        destruction,
        change,
    }

    public Effect effect;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Potion"))
        {

            switch (effect)
            {
                case Effect.change:
                    PotionScript pot = collision.GetComponent<PotionScript>();
                    pot.potion = potionPrefab.GetComponent<PotionScript>().potion;
                    pot.GetComponent<Animator>().runtimeAnimatorController = potionPrefab.GetComponent<Animator>().runtimeAnimatorController;
                    /*GameObject g = Instantiate(potionPrefab, collision.gameObject.transform.position, Quaternion.identity);
                    
                    
                    
                    g.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                    g.GetComponent<Rigidbody2D>().velocity = collision.GetComponent<Rigidbody2D>().velocity; 
                    GameMan.Instance.spawnedPotion++;
                    GameMan.Instance.levelPotions.Add(g.GetComponent<PotionScript>());
                    GameMan.Instance.RemovePotion(collision.gameObject.GetComponent<PotionScript>(), false);
                    Destroy(collision.gameObject);*/

                    break;
                case Effect.destruction:

                    collision.gameObject.SetActive(false);
                GameMan.Instance.PopDialog("FIREEEEE!", 2f);
            
                au.Play();
                GameMan.Instance.RemovePotion(collision.gameObject.GetComponent<PotionScript>(), false);
                Destroy(collision.gameObject);
                    break;
            }
            
        }

    }

    public void DestroyThis()
    {
        Destroy(this.gameObject);
    }
}
