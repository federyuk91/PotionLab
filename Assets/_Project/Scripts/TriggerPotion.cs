using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterSystem;

public class TriggerPotion : MonoBehaviour
{

    public CharacterController ch;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Potion"))
        {
            ch.Drunk(collision.GetComponent<PotionScript>());
            Destroy(collision.gameObject, 2f);
            collision.gameObject.SetActive(false);

        }else if (collision.gameObject.CompareTag("Drop"))
        {
            Destroy(collision.gameObject);
        }
    }

}
