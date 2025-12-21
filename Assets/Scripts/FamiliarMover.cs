using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FamiliarMover : MonoBehaviour
{
    public int timeBeforeGoOut = 15;
    bool canMove = false;
    public AudioSource familiarAudioSource;
    public int click = 0;
    public int maxClick = 3;
    public bool isClickable = true;

    public float clickTimer = 0.5f;
    [Range(0f, .25f)]
    public float speed;
    public List<Transform> points = new List<Transform>();
    public int dest;

    public bool reversePath = false;
    public bool restartPosition = false;
    public Vector3 startingPos;

    public enum FamiliarType
    {
        none,
        destruction,
        change,
        areaEffect,
    }

    public FamiliarType type;
    public PotionScriptable potion;
    public GameObject objectToInstantiate;


    private void OnDrawGizmos()
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (i < points.Count - 1)
                Debug.DrawLine(points[i].position, points[i + 1].position);
            else
                Debug.DrawLine(points[i].position, points[0].position);
        }
    }
    private void OnEnable()
    {

        if (restartPosition)
        {
             transform.position = startingPos;
        }
        StartCoroutine(GoOut());
        //ShufflePoints();
        //Attivare animazione o movimento di entrata e poi canMove a true
        click = 0;
        canMove = true;
    }

    private void OnDisable()
    {
        canMove = false;
        StopAllCoroutines();
    }

    private void FixedUpdate()
    {
        if (!canMove)
            return;

        if (reversePath)
        {
            if (Vector3.Distance(transform.position, points[dest].position) < .1f)
            {
                dest = dest - 1 >= 0 ? dest - 1 : points.Count - 1;
            }
            transform.position = Vector3.MoveTowards(transform.position, points[dest].position, speed);
        }
        else
        {
            if (Vector3.Distance(transform.position, points[dest].position) < .1f)
            {
                dest = (dest + 1) % points.Count;
            }
            transform.position = Vector3.MoveTowards(transform.position, points[dest].position, speed);
        }
    }

    IEnumerator GoOut()
    {
        yield return new WaitForSeconds(timeBeforeGoOut);

        DeactiveObject();
    }

    public void ShufflePoints()
    {
        List<Transform> shuffled = new List<Transform>();
        while (points.Count > 0)
        {
            int i = Random.Range(0, points.Count);
            shuffled.Add(points[i]);
            points.RemoveAt(i);
        }
        points = shuffled;
    }


    public void CatchTheFamiliar()
    {

        if(click <= maxClick)
        {
            if (isClickable)
            {
                familiarAudioSource.Play();
                click++;
                if(click >= 5)
                {
                    AchievementManager.instance.Achive("Spammer!");
                }

                switch (type)
                {
                    case FamiliarType.none:
                        break;
                    case FamiliarType.destruction:
                        DestroyPotion(potion);
                        break;
                    case FamiliarType.change:
                        ChangePotion();
                        break;
                }
                isClickable = false;
                Debug.Log("You catch the familiar");
                Invoke("Isclickable", clickTimer);
            }
            
        } else
        {
            DeactiveObject();
        }
        

        
    }


    public void Isclickable()
    {
        isClickable = !isClickable;
        Debug.Log("You catch the familiar");
    }


    public void DestroyPotion(PotionScriptable p)
    {
        List<PotionScript> tempList = new List<PotionScript>();
        if (p == null)
        {
            foreach (PotionScript g in GameMan.Instance.levelPotions)
            {
                tempList.Add(g);
                
            }

        } else
        {
            foreach (PotionScript g in GameMan.Instance.levelPotions)
            {
                if (g.potion.effectType.Equals(p.effectType))
                {
                    tempList.Add(g);

                   
                }
            }
        }

        foreach(PotionScript pot in tempList)
        {
            pot.gameObject.SetActive(false);
            GameMan.Instance.RemovePotion(pot, false);
            Destroy(pot.gameObject);

        }
        GameMan.Instance.cc.CameraShakeRef(1, 1);
        tempList.Clear();
        Instantiate(objectToInstantiate, transform.position, Quaternion.identity);
        DeactiveObject();
    }

    public void ChangePotion()
    {
        GameObject g = Instantiate(objectToInstantiate, transform.position, Quaternion.identity);
        g.GetComponent<AreaEffectFamiliar>().effect = AreaEffectFamiliar.Effect.change;
    }

    public void DeactiveObject()
    {
        gameObject.SetActive(false);
    }
}
