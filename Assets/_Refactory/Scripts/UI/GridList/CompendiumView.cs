using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI.GridList
{
    public class CompendiumView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GridListDatabase database;
        [SerializeField] private GridListCategoryType startingCategory = GridListCategoryType.Potion;

        [Header("Book Pages")]
        [SerializeField] private RectTransform pageLeft;
        [SerializeField] private RectTransform pageRight;
        [SerializeField] private CompendiumPageSide detailsPage = CompendiumPageSide.Right;

        [Header("Scroll View")]
        [SerializeField] private RectTransform scrollViewRoot;
        [SerializeField] private RectTransform entriesContainer;
        [SerializeField] private CompendiumEntryView entryPrefab;

        [Header("Details")]
        [SerializeField] private RectTransform detailsRoot;
        [SerializeField] private TMP_Text categoryTitleText;
        [SerializeField] private TMP_Text detailTitleText;
        [SerializeField] private TMP_Text detailDescriptionText;
        [SerializeField] private Image detailImage;

        private readonly List<CompendiumEntryView> spawnedEntries = new List<CompendiumEntryView>();
        private GridListCategoryType currentCategory;

        private void OnEnable()
        {
            RefreshLayout();
            ShowCategory(startingCategory);
        }

        public void ShowCategory(int categoryIndex)
        {
            ShowCategory((GridListCategoryType)categoryIndex);
        }

        public void ShowCategory(GridListCategoryType categoryType)
        {
            currentCategory = categoryType;

            if (database == null)
            {
                Debug.LogWarning($"{name}: GridListDatabase reference is missing. Assign it in Inspector.", this);
                ClearEntries();
                ClearDetails();
                return;
            }

            if (!database.TryGetCategory(currentCategory, out GridListCategoryData category))
            {
                Debug.LogWarning($"{name}: category {currentCategory} is missing from GridListDatabase.", this);
                ClearEntries();
                ClearDetails();
                return;
            }

            RenderCategory(category);
        }

        public void SetDetailsPage(int pageIndex)
        {
            detailsPage = (CompendiumPageSide)pageIndex;
            RefreshLayout();
        }

        public void SetDetailsPage(CompendiumPageSide pageSide)
        {
            detailsPage = pageSide;
            RefreshLayout();
        }

        private void RenderCategory(GridListCategoryData category)
        {
            ClearEntries();

            if (!HasRequiredViewReferences())
            {
                ClearDetails();
                return;
            }

            if (categoryTitleText != null)
            {
                categoryTitleText.text = category.Title;
            }

            IReadOnlyList<GridListEntryData> entries = category.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                CompendiumEntryView entryView = Instantiate(entryPrefab, entriesContainer);
                entryView.Bind(entries[index], database.LockedEntry, ShowDetails);
                spawnedEntries.Add(entryView);
            }

            if (entries.Count > 0)
            {
                GridListEntryData firstEntry = entries[0].UnlockedByDefault ? entries[0] : database.LockedEntry;
                ShowDetails(firstEntry);
                return;
            }

            ClearDetails();
        }

        private void ShowDetails(GridListEntryData entry)
        {
            if (entry == null)
            {
                ClearDetails();
                return;
            }

            if (detailTitleText != null)
            {
                detailTitleText.text = entry.DisplayName;
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = entry.Description;
            }

            if (detailImage != null)
            {
                detailImage.sprite = entry.Sprite;
                detailImage.enabled = entry.Sprite != null;
            }
        }

        private void RefreshLayout()
        {
            RectTransform targetDetailsPage = detailsPage == CompendiumPageSide.Left ? pageLeft : pageRight;
            RectTransform targetScrollPage = detailsPage == CompendiumPageSide.Left ? pageRight : pageLeft;

            SetParentIfAvailable(detailsRoot, targetDetailsPage);
            SetParentIfAvailable(scrollViewRoot, targetScrollPage);
        }

        private void SetParentIfAvailable(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null)
            {
                return;
            }

            child.SetParent(parent, false);
        }

        private void ClearEntries()
        {
            foreach (CompendiumEntryView entryView in spawnedEntries)
            {
                if (entryView != null)
                {
                    Destroy(entryView.gameObject);
                }
            }

            spawnedEntries.Clear();
        }

        private void ClearDetails()
        {
            if (detailTitleText != null)
            {
                detailTitleText.text = string.Empty;
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = string.Empty;
            }

            if (detailImage != null)
            {
                detailImage.sprite = null;
                detailImage.enabled = false;
            }
        }

        private bool HasRequiredViewReferences()
        {
            bool isValid = true;

            if (entriesContainer == null)
            {
                Debug.LogWarning($"{name}: Entries Container reference is missing. Assign it in Inspector or run the compendium scene setup.", this);
                isValid = false;
            }

            if (entryPrefab == null)
            {
                Debug.LogWarning($"{name}: Entry Prefab reference is missing. Assign it in Inspector or run the compendium scene setup.", this);
                isValid = false;
            }

            if (detailsRoot == null)
            {
                Debug.LogWarning($"{name}: Details Root reference is missing. Assign it in Inspector or run the compendium scene setup.", this);
                isValid = false;
            }

            return isValid;
        }
    }
}
