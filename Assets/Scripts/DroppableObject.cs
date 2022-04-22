using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppableObject : MonoBehaviour
{

    private Rigidbody2D _rb;
    public GameObject whiteSquare;
    public bool isActive = false;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void ActivateBox()
    {
        whiteSquare.SetActive(true);
        isActive = true;
    }

    public void Drop()
    {
        if (isActive)
        {
            Debug.Log("Drop Potion: " + this.name);

            _rb.bodyType = RigidbodyType2D.Dynamic;
            whiteSquare.SetActive(false);
        }

    }
}
