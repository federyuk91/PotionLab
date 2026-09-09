using System;
using System.Collections.Generic;
using InspectorValidation;
using Refactory.UI.GridList;
using TMPro;
using UnityEngine;

namespace Refactory.UI
{
    [DisallowMultipleComponent]
    public sealed class GrimoireTextIllumination : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference] private RectTransform bookRoot;
        [SerializeField, RequiredInspectorReference] private Canvas rootCanvas;
        [SerializeField] private Color inkColor = new Color(0.12f, 0.085f, 0.065f, 1f);
        [SerializeField] private Color illuminatedColor = Color.white;
        [Tooltip("Radius in book-local units, independent of screen resolution.")]
        [SerializeField, Min(0.1f)] private float lightRadius = 12f;
        [SerializeField, Min(0.01f)] private float responseSpeed = 12f;

        private sealed class TextState
        {
            public TMP_Text Text;
            public Action<TMP_TextInfo> Rebuilt;
            public float[] Light = Array.Empty<float>();
        }

        private readonly List<TextState> texts = new List<TextState>();
        private Vector2 pointer;
        private bool pointerInside;

        private void OnEnable()
        {
            if (bookRoot == null || rootCanvas == null)
            {
                Debug.LogWarning($"{name}: assign Book Root and Root Canvas in GrimoireTextIllumination.", this);
                return;
            }
            RefreshTexts();
        }

        public void RefreshTexts()
        {
            ClearTexts();
            if (!isActiveAndEnabled || bookRoot == null || rootCanvas == null)
                return;
            // Scan only the owned book, when opening or rebuilding its list.
            foreach (CompendiumEntryView entry in bookRoot.GetComponentsInChildren<CompendiumEntryView>(true))
                entry.UseCursorIllumination();
            foreach (TMP_Text text in bookRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                TextState state = new TextState { Text = text };
                state.Rebuilt = info => ApplyColors(state, info, false);
                text.color = new Color(inkColor.r, inkColor.g, inkColor.b, text.color.a);
                text.OnPreRenderText += state.Rebuilt;
                text.SetVerticesDirty();
                texts.Add(state);
            }
        }

        private void LateUpdate()
        {
            if (bookRoot == null || rootCanvas == null)
                return;
            Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            pointerInside = RectTransformUtility.RectangleContainsScreenPoint(bookRoot, Input.mousePosition, camera)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(bookRoot, Input.mousePosition, camera, out pointer);
            foreach (TextState state in texts)
            {
                TMP_Text text = state.Text;
                // Hidden or dirty TMP objects may not yet have an uploadable mesh.
                if (text == null || !text.isActiveAndEnabled || text.havePropertiesChanged || text.mesh == null)
                    continue;
                TMP_TextInfo info = text.textInfo;
                if (info == null || info.characterCount == 0 || info.meshInfo == null)
                    continue;
                bool ready = info.materialCount <= info.meshInfo.Length;
                for (int i = 0; ready && i < info.materialCount; i++)
                    ready = info.meshInfo[i].mesh != null && info.meshInfo[i].colors32 != null;
                if (!ready)
                    continue;
                ApplyColors(state, info, true);
                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }

        private void ApplyColors(TextState state, TMP_TextInfo info, bool advance)
        {
            if (bookRoot == null || info == null || info.meshInfo == null)
                return;
            if (state.Light.Length != info.characterCount)
                state.Light = new float[info.characterCount];
            float blend = 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible || character.elementType == TMP_TextElementType.Sprite)
                    continue;
                int material = character.materialReferenceIndex;
                int vertex = character.vertexIndex;
                if (material >= info.meshInfo.Length)
                    continue;
                Color32[] colors = info.meshInfo[material].colors32;
                if (colors == null || vertex + 3 >= colors.Length)
                    continue;
                Vector3 center = (character.bottomLeft + character.topRight) * 0.5f;
                Vector2 position = bookRoot.InverseTransformPoint(state.Text.transform.TransformPoint(center));
                float distance = Vector2.Distance(pointer, position);
                float target = pointerInside ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lightRadius * 0.2f, lightRadius, distance)) : 0f;
                if (advance)
                    state.Light[i] = Mathf.Lerp(state.Light[i], target, blend);
                Color32 color = Color.Lerp(inkColor, illuminatedColor, state.Light[i]);
                for (int corner = 0; corner < 4; corner++)
                {
                    // Preserve TMP alpha; parent CanvasGroup fades remain independent.
                    color.a = colors[vertex + corner].a;
                    colors[vertex + corner] = color;
                }
            }
        }

        private void OnDisable()
        {
            pointerInside = false;
            ClearTexts();
        }

        private void ClearTexts()
        {
            foreach (TextState state in texts)
            {
                if (state.Text == null)
                    continue;
                state.Text.OnPreRenderText -= state.Rebuilt;
                state.Text.SetVerticesDirty();
            }
            texts.Clear();
        }
    }
}
