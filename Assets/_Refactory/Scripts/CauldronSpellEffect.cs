using UnityEngine;

namespace CharacterSystem
{
    public class CauldronSpellEffect : MonoBehaviour
    {
        [SerializeField] private GameObject baseCauldronObject;
        [SerializeField] private GameObject poweredCauldronObject;

        public void Configure(GameObject baseCauldron, GameObject poweredCauldron)
        {
            baseCauldronObject = baseCauldron;
            poweredCauldronObject = poweredCauldron;
        }

        public bool Play(bool powered)
        {
            GameObject selectedCauldron = powered && poweredCauldronObject != null
                ? poweredCauldronObject
                : baseCauldronObject;

            if (selectedCauldron == null)
            {
                Debug.LogWarning($"{name} has no cauldron object assigned.", this);
                return false;
            }

            SetCauldronActive(baseCauldronObject, selectedCauldron == baseCauldronObject);
            SetCauldronActive(poweredCauldronObject, selectedCauldron == poweredCauldronObject);
            RestartLegacyAnimations(selectedCauldron);
            RestartSpriteAnimations(selectedCauldron);
            return true;
        }

        private void SetCauldronActive(GameObject cauldron, bool active)
        {
            if (cauldron != null)
            {
                cauldron.SetActive(active);
            }
        }

        private void RestartLegacyAnimations(GameObject cauldron)
        {
            Animation[] cauldronAnimations = cauldron.GetComponentsInChildren<Animation>(true);

            if (cauldronAnimations.Length == 0)
            {
                Debug.LogWarning($"{cauldron.name} has no legacy Animation component assigned.", cauldron);
                return;
            }

            foreach (Animation cauldronAnimation in cauldronAnimations)
            {
                if (cauldronAnimation.GetComponent<PotionDestroyTrigger>() != null)
                {
                    continue;
                }

                cauldronAnimation.enabled = true;
                cauldronAnimation.Stop();
                cauldronAnimation.Rewind();
                cauldronAnimation.Play();
            }
        }

        private void RestartSpriteAnimations(GameObject cauldron)
        {
            PotionDestroyFeedback[] feedbacks = cauldron.GetComponentsInChildren<PotionDestroyFeedback>(true);
            foreach (PotionDestroyFeedback feedback in feedbacks)
            {
                feedback.RestartSpriteAnimation();
            }

            // Temporary compatibility while TestingNew still has sprite animation
            // data on PotionDestroyTrigger instead of PotionDestroyFeedback.
            PotionDestroyTrigger[] potionDestroyTriggers = cauldron.GetComponentsInChildren<PotionDestroyTrigger>(true);
            foreach (PotionDestroyTrigger potionDestroyTrigger in potionDestroyTriggers)
            {
                potionDestroyTrigger.RestartSpriteAnimation();
            }
        }
    }
}
