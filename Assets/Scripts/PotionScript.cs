using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionScript : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Sprite _potionSprite;
    private AudioSource _audio;

    public PotionScriptable potion;
    public GameObject whiteSquare;
    public bool isActive = false;

    public List<PotionScript> stock = new List<PotionScript>();

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _rb = GetComponent<Rigidbody2D>();
        _potionSprite = GetComponent<Sprite>();
        stock.Add(this);
    }

    public void ActivateBox()
    {
        if(_rb==null)
            return;
        if (_rb.bodyType.Equals(RigidbodyType2D.Kinematic))
        {
            whiteSquare.SetActive(true);
            isActive = true;
        }
    }

    public void DropPotion()
    {
        if (isActive)
        {
            //Debug.Log("Drop Potion: " + this.name);
            //Debug.Log(_audio);
            _audio.Play();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            whiteSquare.SetActive(false);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Potion"))
        {
            PotionScript collisionPotion = collision.collider.GetComponent<PotionScript>();
            foreach (PotionScript pot in collisionPotion.stock)
            {
                if (!stock.Contains(pot))
                    stock.Add(pot);
            }
            _rb.mass = 1f / stock.Count;
        }
    }


    private void OnCollisionExit2D(Collision2D collision)
    {
        PotionScript collisionPotion = collision.collider.GetComponent<PotionScript>();
        stock.Remove(collisionPotion);
        _rb.mass = 1f / stock.Count;
    }

}

