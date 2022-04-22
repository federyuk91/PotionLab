using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerLeva : MonoBehaviour
{
    public UnityEvent onLevaTriggered;
    bool isLeft = true;
    public string sentenceForPotion= "Ohhh no my potie!", sentenceForDrop = "mmm... What was that switch for?";

    public void GoLeft()
    {
        if (isLeft)
            return;
        GetComponent<Animator>().SetTrigger("goLeft");
        isLeft = true;
    }
    public void GoRight()
    {
        if (!isLeft)
            return;
        GetComponent<Animator>().SetTrigger("goRight");
        isLeft = false;
    }

    public void Switch()
    {
        if (isLeft)
            GoRight();
        else
            GoLeft();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Potion"))
        {
            collision.gameObject.SetActive(false);
            GameMan.Instance.PopDialog(sentenceForPotion,3f);
            //Questa pozione non contribuirà al punteggio
            GameMan.Instance.RemovePotion(collision.gameObject.GetComponent<PotionScript>(), false);
            Debug.Log("Animazione pozione che si rompe?");
        }
        else if (collision.gameObject.CompareTag("Drop"))
        {
            Destroy(collision.gameObject);
            GameMan.Instance.PopDialog(sentenceForDrop, 5f);
        }

        onLevaTriggered.Invoke();
    }

    public void ChangeSlidingPlatform(SurfaceEffector2D platform)
    {
        platform.transform.localScale = new Vector3(-platform.transform.localScale.x, platform.transform.localScale.y, platform.transform.localScale.z);
        platform.speed = platform.speed * -1f;
    }
}
