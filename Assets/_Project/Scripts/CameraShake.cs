using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{

    public bool shake = false;
    Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;  
    }
    public IEnumerator Shake(float duration, float magnitude)
    {

        float elapsed = 0f;
        shake = true;
        while (elapsed < duration)
        {
            
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(x, y, -10f);
            elapsed += Time.deltaTime;
            yield return 0;
        }
        transform.position = originalPosition;
        shake = false;
    }


}