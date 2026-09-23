using InspectorValidation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Refactory.Audio
{
    [DisallowMultipleComponent]
    public sealed class PersistentMusicController : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private AudioSource musicSource;

        [Header("Level Section Tracks")]
        [SerializeField, RequiredInspectorReference] private AudioClip basementTrack;
        [SerializeField, RequiredInspectorReference] private AudioClip laboratoryTrack;
        [SerializeField, RequiredInspectorReference] private AudioClip coldStorageTrack;

        [Header("Scene Routing")]
        [SerializeField, Min(1)] private int basementFirstLevel = 1;
        [SerializeField, Min(1)] private int basementLastLevel = 10;
        [SerializeField, Min(1)] private int laboratoryFirstLevel = 11;
        [SerializeField, Min(1)] private int laboratoryLastLevel = 20;
        [SerializeField, Min(1)] private int coldStorageFirstLevel = 21;
        [SerializeField, Min(1)] private int coldStorageLastLevel = 30;
        [SerializeField, Min(1)] private int endlessSceneBuildIndex = 35;
        [SerializeField, Min(1)] private int potionLaboratorySceneBuildIndex = 36;

        private static PersistentMusicController activeInstance;

        private void Awake()
        {
            if (musicSource == null)
            {
                Debug.LogError($"{name}: assign Music Source in PersistentMusicController.", this);
                enabled = false;
                return;
            }

            if (activeInstance != null && activeInstance != this)
            {
                Destroy(activeInstance.gameObject);
            }

            activeInstance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // MainMenuController handles the title voice before starting menu music.
            if (scene.buildIndex == 0)
            {
                return;
            }

            AudioClip targetTrack = ResolveTrack(scene.buildIndex);
            if (targetTrack == null)
            {
                musicSource.Stop();
                return;
            }

            if (musicSource.clip == targetTrack && musicSource.isPlaying)
            {
                return;
            }

            musicSource.Stop();
            musicSource.clip = targetTrack;
            musicSource.loop = true;
            musicSource.Play();
        }

        private AudioClip ResolveTrack(int sceneBuildIndex)
        {
            if (IsInsideRange(sceneBuildIndex, basementFirstLevel, basementLastLevel)
                || sceneBuildIndex == endlessSceneBuildIndex)
            {
                return basementTrack;
            }

            if (IsInsideRange(sceneBuildIndex, laboratoryFirstLevel, laboratoryLastLevel)
                || sceneBuildIndex == potionLaboratorySceneBuildIndex)
            {
                return laboratoryTrack;
            }

            if (IsInsideRange(sceneBuildIndex, coldStorageFirstLevel, coldStorageLastLevel))
            {
                return coldStorageTrack;
            }

            return null;
        }

        private static bool IsInsideRange(int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }
    }
}
