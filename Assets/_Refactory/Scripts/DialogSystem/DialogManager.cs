using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CharacterSystem
{
    public class DialogManager : MonoBehaviour
    {
        [Header("Dialog UI")]
        [SerializeField] private GameObject textDialog;
        [SerializeField] private TMP_Text dialogText;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private CharacterStats characterStats;

        [Header("Dialog Rules")]
        [SerializeField] private List<CharacterDialogRule> characterRules;

        private Coroutine typedDialogRoutine;

        private void Awake()
        {
            if (characterStats == null)
            {
                characterStats = GetComponent<CharacterStats>();
            }
        }

        public void OnPotionDrunk(PotionScriptable potion, CharacterType character, CharacterStatusController statusController, float popUpDuration = 1.5f)
        {
            if (potion == null)
            {
                return;
            }

            if (TryPopRuleDialog(potion.effectType, character, statusController, popUpDuration))
            {
                return;
            }

            PickADialog(potion.dialogs, popUpDuration);
        }

        public void OnPotionDrunk(PotionScriptable.EffectType effectType, CharacterType character, CharacterStatusController statusController, float popUpDuration = 1.5f)
        {
            TryPopRuleDialog(effectType, character, statusController, popUpDuration);
        }

        private bool TryPopRuleDialog(PotionScriptable.EffectType effectType, CharacterType character, CharacterStatusController statusController, float popUpDuration)
        {
            CharacterDialogRule rule = characterRules.Find(r => r.character == character);

            if (rule == null)
            {
                return false;
            }

            PotionDialogEntry entry = rule.potionDialogs.Find(p => p.potion == effectType);

            if (entry == null)
            {
                return false;
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
                    return true;
                }

                string line = dialogCase.lines[Random.Range(0, dialogCase.lines.Count)];
                PopDialog(line, popUpDuration);
                return true;
            }

            return false;
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

        public void ShowLevelStartCatchphrase(string line, float duration)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            PopDialog(line, duration);
        }

        public void ShowLevelIntroPresentation(string line, float charactersPerSecond, float voiceDuration)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            StopTypedDialog();
            CancelInvoke(nameof(CloseDialog));
            typedDialogRoutine = StartCoroutine(TypeLevelIntroPresentation(line, charactersPerSecond, voiceDuration));
        }

        public void PopDialog(string dialog, float duration = -1f)
        {
            StopTypedDialog();
            CancelInvoke(nameof(CloseDialog));

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

            if (dialogText == null)
            {
                dialogText = textDialog.GetComponentInChildren<TMP_Text>(true);
            }

            if (dialogText != null)
            {
                dialogText.text = dialog;
            }
            else
            {
                Debug.LogWarning($"{name} has no TMP dialog text assigned.", this);
            }

            if (duration > 0)
            {
                Invoke(nameof(CloseDialog), duration);
            }
        }

        public void CloseDialog()
        {
            StopTypedDialog();
            CancelInvoke(nameof(CloseDialog));

            if (textDialog != null)
            {
                textDialog.SetActive(false);
            }
        }

        private IEnumerator TypeLevelIntroPresentation(string line, float charactersPerSecond, float voiceDuration)
        {
            if (!TryOpenDialog())
            {
                typedDialogRoutine = null;
                yield break;
            }

            float safeCharactersPerSecond = Mathf.Max(1f, charactersPerSecond);
            float secondsPerCharacter = 1f / safeCharactersPerSecond;
            float typingElapsedSeconds = 0f;
            int visibleCharacters = 0;

            dialogText.text = string.Empty;
            while (visibleCharacters < line.Length)
            {
                typingElapsedSeconds += Time.unscaledDeltaTime;
                int targetVisibleCharacters = Mathf.Clamp(
                    Mathf.FloorToInt(typingElapsedSeconds / secondsPerCharacter),
                    0,
                    line.Length);

                if (targetVisibleCharacters > visibleCharacters)
                {
                    visibleCharacters = targetVisibleCharacters;
                    dialogText.text = line.Substring(0, visibleCharacters);
                }

                yield return null;
            }

            dialogText.text = line;

            float remainingVoiceTime = Mathf.Max(0f, voiceDuration - typingElapsedSeconds);
            float holdDuration = Mathf.Max(0.75f, remainingVoiceTime);
            yield return new WaitForSecondsRealtime(holdDuration);

            typedDialogRoutine = null;
            if (textDialog != null)
            {
                textDialog.SetActive(false);
            }
        }

        private bool TryOpenDialog()
        {
            if (textDialog == null)
            {
                Debug.LogWarning($"{name} has no dialog UI assigned.", this);
                return false;
            }

            if (IsCharacterDead())
            {
                return false;
            }

            textDialog.SetActive(true);

            if (dialogText == null)
            {
                dialogText = textDialog.GetComponentInChildren<TMP_Text>(true);
            }

            if (dialogText != null)
            {
                return true;
            }

            Debug.LogWarning($"{name} has no TMP dialog text assigned.", this);
            textDialog.SetActive(false);
            return false;
        }

        private void StopTypedDialog()
        {
            if (typedDialogRoutine == null)
            {
                return;
            }

            StopCoroutine(typedDialogRoutine);
            typedDialogRoutine = null;
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
