using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckCollision : MonoBehaviour
{
    public Spawner spawner;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.Equals(spawner.potion.gameObject))
        {
            spawner.ActivateButton();
        }
    }
}
