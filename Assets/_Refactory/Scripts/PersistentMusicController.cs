using InspectorValidation;
using UnityEngine;

namespace Refactory.Audio
{
    [DisallowMultipleComponent]
    public sealed class PersistentMusicController : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private AudioSource musicSource;

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
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }
    }
}
