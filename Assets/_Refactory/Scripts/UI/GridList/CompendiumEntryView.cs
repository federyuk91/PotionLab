using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI.GridList
{
    public class CompendiumEntryView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text shortDescription;
        [SerializeField] private Image iconImage;

        private GridListEntryData entry;
        private Action<GridListEntryData> selected;

        private void Awake()
        {
            if (button == null)
            {
                Debug.LogWarning($"{name}: Button reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(Select);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Select);
            }
        }

        public void Bind(GridListEntryData newEntry, GridListEntryData lockedEntry, Action<GridListEntryData> selectedCallback)
        {
            entry = newEntry != null && newEntry.UnlockedByDefault ? newEntry : lockedEntry;
            selected = selectedCallback;

            if (entry == null)
            {
                Debug.LogWarning($"{name}: cannot bind compendium entry because entry data is missing.", this);
                gameObject.SetActive(false);
                return;
            }

            if (titleText != null)
            {
                titleText.text = entry.DisplayName;
            }

            if (shortDescription != null)
            {
                bool hasShortDescription = !string.IsNullOrWhiteSpace(entry.ShortDescription);
                shortDescription.text = hasShortDescription ? entry.ShortDescription : string.Empty;
                shortDescription.gameObject.SetActive(hasShortDescription);
            }

            if (iconImage != null)
            {
                iconImage.sprite = entry.Sprite;
                iconImage.enabled = entry.Sprite != null;
            }

            gameObject.SetActive(true);
        }

        private void Select()
        {
            if (entry == null)
            {
                Debug.LogWarning($"{name}: cannot select compendium entry because entry data is missing.", this);
                return;
            }

            selected?.Invoke(entry);
        }
    }
}
