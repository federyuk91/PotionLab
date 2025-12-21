using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchScript : MonoBehaviour
{
    public Vector2 punchDirection;
    public float punchForce = 10;

    public int count = 0;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Potion"))
        {
            count++;

            if(count == 4)
            {
                AchievementManager.instance.Achive("Falcon PUNCH!");
            }

            Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
            Vector2 force = punchDirection.normalized * punchForce;
            otherRb.mass = 1;

            // Applica la forza all'oggetto colliso
            otherRb.AddForce(force, ForceMode2D.Impulse);
           
        }
    }


    private void OnDisable()
    {
        count = 0;
    }
}
