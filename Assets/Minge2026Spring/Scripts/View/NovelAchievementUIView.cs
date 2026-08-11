using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class NovelAchievementUIView : MonoBehaviour
    {
        [Serializable]
        public class EndingTextPair
        {
            [SerializeField] private TMP_Text titleText;
            [SerializeField] private TMP_Text descriptionText;

            public TMP_Text TitleText => titleText;
            public RectTransform Root => titleText.transform.parent as RectTransform;

            public void SetText(int index, bool unlocked, bool showDescription)
            {
                titleText.text = unlocked
                    ? AchievementEndingText.FormatTitle(index)
                    : $"{(char)('A' + index)}. ？？？";
                descriptionText.text = showDescription ? AchievementEndingText.GetDescription(index) : "???";
                titleText.color = Color.white;
            }
        }

        [Header("エンディング実績（AからKの順）")]
        [SerializeField] private List<EndingTextPair> endingTexts = new();
        [Header("Kエンド虹色")]
        [SerializeField, Min(0f)] private float rainbowSpeed = 0.2f;
        [Header("現在選択中のエンディング")]
        [SerializeField, Min(0.01f)] private float selectionMoveDuration = 0.22f;
        [SerializeField, Min(0f)] private float selectionFrameInset;

        private TMP_Text _rainbowTitle;
        private bool _rainbowEnabled;
        private RectTransform _selectionFrame;
        private RectTransform _selectionTarget;
        private Material _selectionMaterial;
        private Vector2 _selectionPositionVelocity;
        private Vector2 _selectionSizeVelocity;

        public void SetSelectedEnding(int selectedIndex)
        {
            if (endingTexts.Count != AchievementEndingText.EndingCount)
                throw new InvalidOperationException("Novel ending text pairs must contain exactly 11 entries (A-K).");
            if (selectedIndex < 0 || selectedIndex >= endingTexts.Count)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            _selectionTarget = endingTexts[selectedIndex].Root;
            EnsureSelectionFrame();
        }

        public void SetAchievements(ISet<string> reachedEndingIds, bool showDescriptions)
        {
            if (endingTexts.Count != AchievementEndingText.EndingCount)
                throw new InvalidOperationException("Novel ending text pairs must contain exactly 11 entries (A-K).");

            var extraStageCleared = reachedEndingIds.Contains("Ending_K");
            for (var index = 0; index < endingTexts.Count; index++)
            {
                var unlocked = reachedEndingIds.Contains($"Ending_{(char)('A' + index)}");
                var isExtraStageEnding = index == AchievementEndingText.RainbowEndingIndex;
                var showDescription = showDescriptions && (!isExtraStageEnding || extraStageCleared);
                endingTexts[index].SetText(index, unlocked, showDescription);
            }

            _rainbowTitle = endingTexts[AchievementEndingText.RainbowEndingIndex].TitleText;
            _rainbowEnabled = extraStageCleared;
        }

        private void Update()
        {
            UpdateSelectionFrame();

            if (!_rainbowEnabled || _rainbowTitle == null)
                return;

            _rainbowTitle.ForceMeshUpdate();
            var textInfo = _rainbowTitle.textInfo;
            var visibleIndex = 0;
            var visibleCount = Mathf.Max(1, textInfo.characterCount);
            for (var index = 0; index < textInfo.characterCount; index++)
            {
                var character = textInfo.characterInfo[index];
                if (!character.isVisible)
                    continue;

                var colors = textInfo.meshInfo[character.materialReferenceIndex].colors32;
                var color = (Color32)Color.HSVToRGB(
                    Mathf.Repeat(Time.unscaledTime * rainbowSpeed + visibleIndex / (float)visibleCount, 1f),
                    0.85f,
                    1f);
                for (var vertex = 0; vertex < 4; vertex++)
                    colors[character.vertexIndex + vertex] = color;
                visibleIndex++;
            }

            _rainbowTitle.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void OnDestroy()
        {
            if (_selectionMaterial != null)
                Destroy(_selectionMaterial);
        }

        private void EnsureSelectionFrame()
        {
            if (_selectionFrame != null || _selectionTarget == null)
                return;

            var materialTemplate = Resources.Load<Material>("CurrentEndingFrame");
            if (materialTemplate == null)
            {
                Debug.LogError("[NovelAchievementUIView] CurrentEndingFrame material was not found.");
                return;
            }

            var frameObject = new GameObject(
                "CurrentEndingFrame",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            _selectionFrame = frameObject.GetComponent<RectTransform>();
            _selectionFrame.SetParent(_selectionTarget.parent, false);
            _selectionFrame.anchorMin = _selectionFrame.anchorMax = new Vector2(0.5f, 0.5f);
            _selectionFrame.pivot = new Vector2(0.5f, 0.5f);
            _selectionFrame.SetAsFirstSibling();

            frameObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var image = frameObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            _selectionMaterial = new Material(materialTemplate) { name = "Current Ending Frame (Runtime)" };
            image.material = _selectionMaterial;

            Canvas.ForceUpdateCanvases();
            GetSelectionTargetGeometry(out var position, out var size);
            _selectionFrame.anchoredPosition = position;
            _selectionFrame.sizeDelta = size;
            _selectionMaterial.SetVector("_RectSize", new Vector4(size.x, size.y, 0f, 0f));
        }

        private void UpdateSelectionFrame()
        {
            if (_selectionFrame == null || _selectionTarget == null)
                return;

            GetSelectionTargetGeometry(out var targetPosition, out var targetSize);
            _selectionFrame.anchoredPosition = Vector2.SmoothDamp(
                _selectionFrame.anchoredPosition,
                targetPosition,
                ref _selectionPositionVelocity,
                selectionMoveDuration,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            _selectionFrame.sizeDelta = Vector2.SmoothDamp(
                _selectionFrame.sizeDelta,
                targetSize,
                ref _selectionSizeVelocity,
                selectionMoveDuration,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            var frameSize = _selectionFrame.rect.size;
            _selectionMaterial.SetVector("_RectSize", new Vector4(frameSize.x, frameSize.y, 0f, 0f));
        }

        private void GetSelectionTargetGeometry(out Vector2 position, out Vector2 size)
        {
            var parent = _selectionTarget.parent as RectTransform;
            var worldCenter = _selectionTarget.TransformPoint(_selectionTarget.rect.center);
            position = parent.InverseTransformPoint(worldCenter);
            size = new Vector2(
                Mathf.Max(0f, parent.rect.width - selectionFrameInset * 2f),
                Mathf.Max(0f, _selectionTarget.rect.height - selectionFrameInset * 2f));
        }
    }
}
