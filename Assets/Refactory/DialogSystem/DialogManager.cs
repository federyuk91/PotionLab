using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CharacterSystem
{
    public class DialogManager : MonoBehaviour
    {
        [Header("Dialog UI")]
        [SerializeField] private GameObject textDialog;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private CharacterStats characterStats;

        [Header("Dialog Rules")]
        [SerializeField] private List<CharacterDialogRule> characterRules;

        private void Awake()
        {
            if (characterStats == null)
            {
                characterStats = GetComponent<CharacterStats>();
            }
        }

        public void OnPotionDrunk(PotionScriptable.EffectType effectType, CharacterType character, CharacterStatusController statusController, float popUpDuration = 1.5f)
        {
            CharacterDialogRule rule = characterRules.Find(r => r.character == character);

            if (rule == null)
            {
                return;
            }

            PotionDialogEntry entry = rule.potionDialogs.Find(p => p.potion == effectType);

            if (entry == null)
            {
                return;
            }

            HashSet<Status> currentStatuses = statusController.GetCurrentStatuses();

            // More required statuses means a more specific dialog rule.
            entry.cases.Sort((StatusDialogCase a, StatusDialogCase b) => b.requiredStatuses.Count.CompareTo(a.requiredStatuses.Count));

            foreach (StatusDialogCase dialogCase in entry.cases)
            {
                if (!dialogCase.Matches(currentStatuses))
                {
                    continue;
                }

                // Empty lines intentionally stop the search: this case wants no popup.
                if (dialogCase.lines.Count == 0)
                {
                    return;
                }

                string line = dialogCase.lines[Random.Range(0, dialogCase.lines.Count)];
                PopDialog(line, popUpDuration);
                return;
            }
        }

        public void PickADialog(List<string> dialogs, float duration = -1f)
        {
            if (dialogs == null || dialogs.Count == 0)
            {
                return;
            }

            int index = Random.Range(0, dialogs.Count);
            PopDialog(dialogs[index], duration);
        }

        public void PopDialog(string dialog, float duration = -1f)
        {
            if (textDialog == null)
            {
                Debug.LogWarning($"{name} has no dialog UI assigned.", this);
                return;
            }

            if (textDialog.activeSelf || IsCharacterDead())
            {
                textDialog.SetActive(false);
            }

            Debug.Log("[DialogManager] PopDialog: " + dialog);

            textDialog.SetActive(true);
            textDialog.GetComponentInChildren<Text>().text = dialog;

            if (duration > 0)
            {
                CancelInvoke(nameof(CloseDialog));
                Invoke(nameof(CloseDialog), duration);
            }
        }

        public void CloseDialog()
        {
            if (textDialog != null)
            {
                textDialog.SetActive(false);
            }
        }

        public void SetContinueButtonActive(bool active)
        {
            if (continueButton != null)
            {
                continueButton.SetActive(active);
            }
        }

        private bool IsCharacterDead()
        {
            return characterStats != null && characterStats.HP <= 0;
        }
    }
}
