using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerBounce : MonoBehaviour
{
    private Animator animator;
    private AudioSource audio;

    private void Start()
    {
        animator = GetComponent<Animator>();
        audio = GetComponent<AudioSource>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        animator.SetTrigger("activate");
        if (audio.isPlaying)
        {
            audio.Stop();
        }

        audio.Play();
    }
}
