using UnityEngine;

public class LevelStartUIController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject clickToStartLevel;

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.LevelStarted += HideStartPrompt;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.LevelStarted -= HideStartPrompt;
        }
    }

    public void StartLevel()
    {
        if (gameManager != null)
        {
            gameManager.StartLevel();
        }
    }

    private void HideStartPrompt()
    {
        if (clickToStartLevel != null)
        {
            clickToStartLevel.SetActive(false);
        }
    }
}
