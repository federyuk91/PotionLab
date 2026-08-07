using InspectorValidation;
using UnityEngine;

public class CheckCollision : MonoBehaviour
{
    [SerializeField, RequiredInspectorReference] private Spawner spawner;

    private bool missingSpawnerWarningShown;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null)
        {
            return;
        }

        if (spawner == null)
        {
            WarnMissingSpawner();
            return;
        }

        PotionScript currentPotion = spawner.potion;
        if (currentPotion == null)
        {
            return;
        }

        if (collision.gameObject == currentPotion.gameObject)
        {
            spawner.ActivateButton();
        }
    }

    private void WarnMissingSpawner()
    {
        if (missingSpawnerWarningShown)
        {
            return;
        }

        missingSpawnerWarningShown = true;
        Debug.LogWarning($"{name}: Spawner reference is missing. Assign it in Inspector so the drop button can be activated when the potion reaches the holder.", this);
    }
}
