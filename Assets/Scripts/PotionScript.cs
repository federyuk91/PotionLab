using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionScript : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Sprite _potionSprite;

    public PotionScriptable potion;
    public GameObject whiteSquare; 
    public bool isActive = false;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _potionSprite = GetComponent<Sprite>();

    }

    public void ActivateBox()
    {
        whiteSquare.SetActive(true);
        isActive = true;
    }

    public void DropPotion()
    {
        if (isActive)
        {
            Debug.Log("Drop Potion: " + this.name);

            _rb.bodyType = RigidbodyType2D.Dynamic;
            whiteSquare.SetActive(false);
        }
        
    }

}

