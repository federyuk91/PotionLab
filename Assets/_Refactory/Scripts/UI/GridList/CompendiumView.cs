using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Refactory.UI.GridList
{
    public class CompendiumView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GridListDatabase database;
        [SerializeField] private GridListCategoryType startingCategory = GridListCategoryType.Options;

        [Header("Book Pages")]
        [SerializeField] private RectTransform pageLeft;
        [SerializeField] private RectTransform pageRight;
        [SerializeField] private CompendiumPageSide detailsPage = CompendiumPageSide.Right;

        [Header("Page Transition")]
        [SerializeField] private CanvasGroup pageLeftCanvasGroup;
        [SerializeField] private CanvasGroup pageRightCanvasGroup;
        [SerializeField] private Animator pageTurnAnimator;
        [SerializeField] private string pageTurnTrigger = "TurnPage";
        [SerializeField] private string pageTurnStateName = "Anim_UI_Grimoire_NextPage";
        [SerializeField, Min(0f)] private float pageFadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float pageTurnDuration = 0.4f;
        [Header("Audio")]
        [FormerlySerializedAs("pageTurnAudioSource")]
        [SerializeField] private AudioSource compendiumAudioSource;
        [SerializeField] private AudioClip pageTurnClip;
        [SerializeField] private AudioClip openClip;

        [Header("Compendium Visibility")]
        [SerializeField] private CanvasGroup compendiumCanvasGroup;
        [SerializeField, Min(0f)] private float compendiumFadeDuration = 0.25f;

        [Header("Options Page")]
        [SerializeField] private Image grimoireBackgroundImage;
        [SerializeField] private Sprite optionsBackgroundSprite;
        [SerializeField] private GameObject rightPageMainMenu;

        [Header("Scroll View")]
        [SerializeField] private RectTransform scrollViewRoot;
        [SerializeField] private ScrollRect entriesScrollRect;
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
        private GridListCategoryType queuedCategory;
        private Coroutine categoryTransition;
        private Coroutine visibilityTransition;
        private bool hasRenderedCategory;
        private bool isShowingOptions;
        private Sprite defaultGrimoireSprite;

        private void Awake()
        {
            if (grimoireBackgroundImage != null)
            {
                defaultGrimoireSprite = grimoireBackgroundImage.sprite;
            }
        }

        private void OnEnable()
        {
            if (pageTurnAnimator != null)
            {
                pageTurnAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                SetPageTurnVisible(false);
            }

            SetPagesVisible(true);
            RestoreCategoryPageVisuals();
            RefreshLayout();

            if (startingCategory == GridListCategoryType.Options)
            {
                ShowOptionsImmediately();
            }
            else
            {
                RenderRequestedCategory(startingCategory);
            }

            StartCompendiumFadeIn();
            PlayCompendiumSound(openClip, "opening");
        }

        private void OnDisable()
        {
            categoryTransition = null;
            visibilityTransition = null;
            SetPageTurnVisible(false);
            SetPagesVisible(true);
        }

        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return;
            }

            StartCompendiumFadeIn();
        }

        public void CloseWithFade()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (visibilityTransition != null)
            {
                StopCoroutine(visibilityTransition);
            }

            visibilityTransition = StartCoroutine(FadeCompendiumOutAndDisable());
        }

        public void ShowCategory(int categoryIndex)
        {
            ShowCategory((GridListCategoryType)categoryIndex);
        }

        public void ShowCategory(GridListCategoryType categoryType)
        {
            if (categoryType == GridListCategoryType.Options)
            {
                ShowOptions();
                return;
            }

            if (!isActiveAndEnabled)
            {
                RenderRequestedCategory(categoryType);
                return;
            }

            if (categoryTransition != null)
            {
                return;
            }

            if (isShowingOptions)
            {
                queuedCategory = categoryType;
                categoryTransition = StartCoroutine(LeaveOptionsRoutine());
                return;
            }

            if (!hasRenderedCategory)
            {
                RenderRequestedCategory(categoryType);
                return;
            }

            if (categoryType == currentCategory)
            {
                return;
            }

            queuedCategory = categoryType;
            categoryTransition = StartCoroutine(ChangeCategoryRoutine());
        }

        public void ShowOptions()
        {
            if (!isActiveAndEnabled || isShowingOptions || categoryTransition != null)
            {
                return;
            }

            startingCategory = GridListCategoryType.Options;
            categoryTransition = StartCoroutine(ShowOptionsRoutine());
        }

        private void ShowOptionsImmediately()
        {
            startingCategory = GridListCategoryType.Options;
            isShowingOptions = true;
            ApplyOptionsBackground();

            if (detailsRoot != null)
            {
                detailsRoot.gameObject.SetActive(false);
            }

            SetPageCanvasGroupState(pageLeftCanvasGroup, 0f, false);
            SetPageCanvasGroupState(pageRightCanvasGroup, 1f, true);

            if (rightPageMainMenu != null)
            {
                rightPageMainMenu.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"{name}: Right Page Main Menu reference is missing. Assign the options menu root in Inspector.", this);
            }
        }

        private IEnumerator ChangeCategoryRoutine()
        {
            SetPageInputEnabled(false);
            yield return FadePages(1f, 0f);

            TriggerPageTurnAnimation();
            if (pageTurnDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(pageTurnDuration);
            }

            SetPageTurnVisible(false);

            GridListCategoryType categoryToRender = queuedCategory;
            RenderRequestedCategory(categoryToRender);

            yield return FadePages(0f, 1f);
            SetPageInputEnabled(true);
            categoryTransition = null;

            if (queuedCategory != currentCategory)
            {
                categoryTransition = StartCoroutine(ChangeCategoryRoutine());
            }
        }

        private IEnumerator ShowOptionsRoutine()
        {
            isShowingOptions = true;
            SetPageInputEnabled(false);
            ApplyOptionsBackground();

            if (detailsRoot != null)
            {
                detailsRoot.gameObject.SetActive(false);
            }

            if (rightPageMainMenu != null)
            {
                rightPageMainMenu.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"{name}: Right Page Main Menu reference is missing. Assign the options menu root in Inspector.", this);
            }

            SetPageCanvasGroupState(pageLeftCanvasGroup, 0f, false);
            SetPageCanvasGroupState(pageRightCanvasGroup, 1f, false);
            yield return PlayReversePageTurnAnimation();

            SetPageTurnVisible(false);

            if (rightPageMainMenu != null)
            {
                rightPageMainMenu.SetActive(true);
            }

            SetPageCanvasGroupState(pageRightCanvasGroup, 1f, true);
            categoryTransition = null;
        }

        private IEnumerator LeaveOptionsRoutine()
        {
            if (rightPageMainMenu != null)
            {
                rightPageMainMenu.SetActive(false);
            }

            RestoreCategoryBackground();
            SetPageCanvasGroupState(pageLeftCanvasGroup, 0f, false);
            SetPageCanvasGroupState(pageRightCanvasGroup, 0f, false);
            TriggerPageTurnAnimation();

            if (pageTurnDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(pageTurnDuration);
            }

            SetPageTurnVisible(false);
            isShowingOptions = false;

            if (detailsRoot != null)
            {
                detailsRoot.gameObject.SetActive(true);
            }

            RenderRequestedCategory(queuedCategory);
            yield return FadePages(0f, 1f);
            SetPageInputEnabled(true);
            categoryTransition = null;
        }

        private void RenderRequestedCategory(GridListCategoryType categoryType)
        {
            startingCategory = categoryType;
            currentCategory = categoryType;
            queuedCategory = categoryType;
            hasRenderedCategory = true;

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

        private IEnumerator FadePages(float startAlpha, float endAlpha)
        {
            if (pageLeftCanvasGroup == null || pageRightCanvasGroup == null)
            {
                Debug.LogWarning($"{name}: page CanvasGroup references are missing. Assign both page CanvasGroups in Inspector to enable the compendium fade.", this);
                yield break;
            }

            if (pageFadeDuration <= 0f)
            {
                SetPageAlpha(endAlpha);
                yield break;
            }

            float elapsed = 0f;
            SetPageAlpha(startAlpha);

            while (elapsed < pageFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / pageFadeDuration);
                SetPageAlpha(Mathf.Lerp(startAlpha, endAlpha, progress));
                yield return null;
            }

            SetPageAlpha(endAlpha);
        }

        private void TriggerPageTurnAnimation()
        {
            if (pageTurnAnimator == null)
            {
                Debug.LogWarning($"{name}: Page Turn Animator reference is missing. Assign the UI Animator in Inspector to enable the page-turn animation.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(pageTurnTrigger))
            {
                Debug.LogWarning($"{name}: Page Turn Trigger is empty. Assign the Animator trigger name in Inspector.", this);
                return;
            }

            SetPageTurnVisible(true);
            pageTurnAnimator.ResetTrigger(pageTurnTrigger);
            pageTurnAnimator.SetTrigger(pageTurnTrigger);
            PlayPageTurnSound();
        }

        private IEnumerator PlayReversePageTurnAnimation()
        {
            if (pageTurnAnimator == null)
            {
                Debug.LogWarning($"{name}: Page Turn Animator reference is missing. Assign the UI Animator in Inspector to enable the reverse page-turn animation.", this);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(pageTurnStateName))
            {
                Debug.LogWarning($"{name}: Page Turn State Name is empty. Assign the page-turn Animator state name in Inspector.", this);
                yield break;
            }

            SetPageTurnVisible(true);
            pageTurnAnimator.speed = 0f;
            PlayPageTurnSound();

            if (pageTurnDuration <= 0f)
            {
                pageTurnAnimator.Play(pageTurnStateName, 0, 0f);
                pageTurnAnimator.Update(0f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < pageTurnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / pageTurnDuration);
                float normalizedTime = 1f - progress;
                pageTurnAnimator.Play(pageTurnStateName, 0, normalizedTime);
                pageTurnAnimator.Update(0f);
                yield return null;
            }

            pageTurnAnimator.Play(pageTurnStateName, 0, 0f);
            pageTurnAnimator.Update(0f);
        }

        private void PlayPageTurnSound()
        {
            PlayCompendiumSound(pageTurnClip, "page turn");
        }

        private void PlayCompendiumSound(AudioClip clip, string context)
        {
            if (compendiumAudioSource == null)
            {
                Debug.LogWarning($"{name}: Compendium Audio Source reference is missing. Assign it in Inspector to play the {context} sound.", this);
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning($"{name}: Compendium {context} AudioClip is missing. Assign it in Inspector.", this);
                return;
            }

            compendiumAudioSource.PlayOneShot(clip);
        }

        private void SetPageTurnVisible(bool visible)
        {
            if (pageTurnAnimator == null)
            {
                return;
            }

            GameObject pageTurnObject = pageTurnAnimator.gameObject;
            if (pageTurnObject == gameObject)
            {
                Debug.LogError($"{name}: Page Turn Animator must be assigned to a separate child UI GameObject; otherwise it cannot be hidden safely.", this);
                return;
            }

            if (!visible)
            {
                pageTurnAnimator.speed = 1f;
            }

            pageTurnObject.SetActive(visible);
        }

        private void ApplyOptionsBackground()
        {
            if (grimoireBackgroundImage == null)
            {
                Debug.LogWarning($"{name}: Grimoire Background Image reference is missing. Assign the base grimoire Image in Inspector.", this);
                return;
            }

            if (optionsBackgroundSprite == null)
            {
                Debug.LogWarning($"{name}: Options Background Sprite reference is missing. Assign it in Inspector.", this);
                return;
            }

            grimoireBackgroundImage.sprite = optionsBackgroundSprite;
        }

        private void RestoreCategoryBackground()
        {
            if (grimoireBackgroundImage != null && defaultGrimoireSprite != null)
            {
                grimoireBackgroundImage.sprite = defaultGrimoireSprite;
            }
        }

        private void RestoreCategoryPageVisuals()
        {
            isShowingOptions = false;
            RestoreCategoryBackground();

            if (rightPageMainMenu != null)
            {
                rightPageMainMenu.SetActive(false);
            }

            if (detailsRoot != null)
            {
                detailsRoot.gameObject.SetActive(true);
            }
        }

        private void StartCompendiumFadeIn()
        {
            if (compendiumCanvasGroup == null)
            {
                Debug.LogWarning($"{name}: Compendium Canvas Group reference is missing. Assign the CanvasGroup on the compendium root to enable open and close fades.", this);
                return;
            }

            if (visibilityTransition != null)
            {
                StopCoroutine(visibilityTransition);
            }

            visibilityTransition = StartCoroutine(FadeCompendiumIn());
        }

        private IEnumerator FadeCompendiumIn()
        {
            SetCompendiumInputEnabled(false);
            yield return FadeCanvasGroup(compendiumCanvasGroup, 0f, 1f, compendiumFadeDuration);
            SetCompendiumInputEnabled(true);
            visibilityTransition = null;
        }

        private IEnumerator FadeCompendiumOutAndDisable()
        {
            if (compendiumCanvasGroup == null)
            {
                Debug.LogWarning($"{name}: Compendium Canvas Group reference is missing. Closing the compendium without a fade.", this);
                gameObject.SetActive(false);
                yield break;
            }

            SetCompendiumInputEnabled(false);
            yield return FadeCanvasGroup(compendiumCanvasGroup, compendiumCanvasGroup.alpha, 0f, compendiumFadeDuration);
            visibilityTransition = null;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
        {
            if (duration <= 0f)
            {
                canvasGroup.alpha = endAlpha;
                yield break;
            }

            float elapsed = 0f;
            canvasGroup.alpha = startAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
        }

        private void SetCompendiumInputEnabled(bool enabled)
        {
            if (compendiumCanvasGroup == null)
            {
                return;
            }

            compendiumCanvasGroup.interactable = enabled;
            compendiumCanvasGroup.blocksRaycasts = enabled;
        }

        private void SetPagesVisible(bool visible)
        {
            SetPageAlpha(visible ? 1f : 0f);
            SetPageInputEnabled(visible);
        }

        private void SetPageAlpha(float alpha)
        {
            if (pageLeftCanvasGroup != null)
            {
                pageLeftCanvasGroup.alpha = alpha;
            }

            if (pageRightCanvasGroup != null)
            {
                pageRightCanvasGroup.alpha = alpha;
            }
        }

        private void SetPageInputEnabled(bool enabled)
        {
            SetPageInputEnabled(pageLeftCanvasGroup, enabled);
            SetPageInputEnabled(pageRightCanvasGroup, enabled);
        }

        private void SetPageInputEnabled(CanvasGroup pageCanvasGroup, bool enabled)
        {
            if (pageCanvasGroup == null)
            {
                return;
            }

            pageCanvasGroup.interactable = enabled;
            pageCanvasGroup.blocksRaycasts = enabled;
        }

        private void SetPageCanvasGroupState(CanvasGroup pageCanvasGroup, float alpha, bool inputEnabled)
        {
            if (pageCanvasGroup == null)
            {
                Debug.LogWarning($"{name}: a page CanvasGroup reference is missing. Assign both page CanvasGroups in Inspector.", this);
                return;
            }

            pageCanvasGroup.alpha = alpha;
            pageCanvasGroup.interactable = inputEnabled;
            pageCanvasGroup.blocksRaycasts = inputEnabled;
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
                ResetEntriesScroll();
                return;
            }

            ClearDetails();
            ResetEntriesScroll();
        }

        private void ResetEntriesScroll()
        {
            if (entriesScrollRect == null)
            {
                Debug.LogWarning($"{name}: Entries Scroll Rect reference is missing. Assign the compendium ScrollRect in Inspector.", this);
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (entriesContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(entriesContainer);
            }

            entriesScrollRect.StopMovement();

            if (entriesScrollRect.horizontal)
            {
                entriesScrollRect.horizontalNormalizedPosition = 0f;
            }

            if (entriesScrollRect.vertical)
            {
                entriesScrollRect.verticalNormalizedPosition = 1f;
            }
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
