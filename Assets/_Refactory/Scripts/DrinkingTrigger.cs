using UnityEngine;
using CharacterSystem;
public class DrinkingTrigger : MonoBehaviour
{
    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Potion"))
        {
            TransformationManager.Instance.Current.Drunk(collision.GetComponent<PotionScript>());
            Destroy(collision.gameObject, 2f);
            collision.gameObject.SetActive(false);

        }
        else if (collision.gameObject.CompareTag("Drop"))
        {
            Destroy(collision.gameObject);
        }
    }
}
