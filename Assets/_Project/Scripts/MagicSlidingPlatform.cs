using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicSlidingPlatform : MonoBehaviour
{
    [Range(0f,.1f)]
    public float speed=.01f;
    public List<Transform> points = new List<Transform>();

    public List<GameObject> trappedObjects;
    public List<int> dest;

    public bool reversePath = false;

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

    private void Awake()
    {
        if (trappedObjects.Count > points.Count)
        {
            Debug.LogError("To much objects in this slider");
        }

        /*dest = new List<int>();
        for(int i = 0; i<points.Count; i++)
        {
            dest.Add(i);
        }*/
        
    }

    private void FixedUpdate()
    {
        if (reversePath)
        {
            for (int i = 0; i < trappedObjects.Count; i++)
            {
                if (Vector3.Distance(trappedObjects[i].transform.position, points[dest[i]].position) < .1f)
                {
                    dest[i] = (dest[i] - 1)>=0? (dest[i] - 1):points.Count-1;
                }
                trappedObjects[i].transform.position = Vector3.MoveTowards(trappedObjects[i].transform.position, points[dest[i]].position, speed);
            }

        }
        else
        {
            for (int i = 0; i < trappedObjects.Count; i++)
            {
                if (Vector3.Distance(trappedObjects[i].transform.position, points[dest[i]].position) < .1f)
                {
                    dest[i] = (dest[i] + 1) % points.Count;
                }
                trappedObjects[i].transform.position = Vector3.MoveTowards(trappedObjects[i].transform.position, points[dest[i]].position, speed);
            }
        }
    }

    public void FreeObject(GameObject obj)
    {
        int index = trappedObjects.IndexOf(obj);
        dest.RemoveAt(index);
        trappedObjects.RemoveAt(index);
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

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            ShufflePoints();
        }
    }
}
