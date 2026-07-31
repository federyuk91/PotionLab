using UnityEngine;

namespace CharacterSystem
{
    public class LitchSummonGroup : MonoBehaviour
    {
        [SerializeField] private GameObject[] skeletons;

        private Vector3[] initialLocalPositions;
        private Quaternion[] initialLocalRotations;
        private Vector3[] initialLocalScales;
        private Rigidbody2D[] skeletonRigidbodies;

        private void Awake()
        {
            CacheSkeletons();
            CacheInitialTransforms();
        }

        private void CacheSkeletons()
        {
            if (skeletons == null || skeletons.Length == 0)
            {
                LitchSummonPotionDestroyer[] summonSkeletons = GetComponentsInChildren<LitchSummonPotionDestroyer>(true);
                skeletons = new GameObject[summonSkeletons.Length];

                for (int i = 0; i < summonSkeletons.Length; i++)
                {
                    skeletons[i] = summonSkeletons[i].gameObject;
                }
            }
        }

        private void CacheInitialTransforms()
        {
            if (skeletons == null)
            {
                return;
            }

            initialLocalPositions = new Vector3[skeletons.Length];
            initialLocalRotations = new Quaternion[skeletons.Length];
            initialLocalScales = new Vector3[skeletons.Length];
            skeletonRigidbodies = new Rigidbody2D[skeletons.Length];

            for (int i = 0; i < skeletons.Length; i++)
            {
                if (skeletons[i] == null)
                {
                    continue;
                }

                Transform skeletonTransform = skeletons[i].transform;
                initialLocalPositions[i] = skeletonTransform.localPosition;
                initialLocalRotations[i] = skeletonTransform.localRotation;
                initialLocalScales[i] = skeletonTransform.localScale;
                skeletonRigidbodies[i] = skeletons[i].GetComponent<Rigidbody2D>();
            }
        }

        public bool HasActiveSkeletons()
        {
            CacheSkeletons();

            if (skeletons == null)
            {
                return false;
            }

            foreach (GameObject skeleton in skeletons)
            {
                if (skeleton != null && skeleton.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        public void ActivateSkeletons()
        {
            CacheSkeletons();
            EnsureInitialTransformsCached();
            gameObject.SetActive(true);

            if (skeletons == null)
            {
                return;
            }

            for (int i = 0; i < skeletons.Length; i++)
            {
                GameObject skeleton = skeletons[i];
                if (skeleton != null)
                {
                    ResetSkeleton(i);
                    skeleton.SetActive(true);
                }
            }
        }

        private void EnsureInitialTransformsCached()
        {
            if (initialLocalPositions == null || initialLocalPositions.Length != skeletons.Length)
            {
                CacheInitialTransforms();
            }
        }

        private void ResetSkeleton(int index)
        {
            Transform skeletonTransform = skeletons[index].transform;
            skeletonTransform.localPosition = initialLocalPositions[index];
            skeletonTransform.localRotation = initialLocalRotations[index];
            skeletonTransform.localScale = initialLocalScales[index];

            Rigidbody2D skeletonRigidbody = skeletonRigidbodies[index];
            if (skeletonRigidbody == null)
            {
                return;
            }

            skeletonRigidbody.linearVelocity = Vector2.zero;
            skeletonRigidbody.angularVelocity = 0f;
        }
    }
}
