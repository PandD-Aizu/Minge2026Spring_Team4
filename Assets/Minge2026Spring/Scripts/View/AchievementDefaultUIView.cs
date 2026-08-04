using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class AchievementDefaultUIView : MonoBehaviour
    {
        [Serializable]
        public class EndingTextPair
        {
            [SerializeField] private TMP_Text titleText;
            [SerializeField] private TMP_Text descriptionText;
            private string _unlockedTitle;
            private string _unlockedDescription;

            public RectTransform Root => titleText.transform.parent as RectTransform;

            public void CaptureUnlockedText()
            {
                _unlockedTitle = titleText.text;
                _unlockedDescription = descriptionText.text;
            }

            public void SetUnlocked(bool unlocked, bool showDescription)
            {
                titleText.text = unlocked ? _unlockedTitle : "???";
                descriptionText.text = showDescription ? _unlockedDescription : "???";
            }

            public void ConfigureLayout(float titleFontSize, float descriptionFontSize)
            {
                ConfigureText(titleText, new Vector2(0f, 0.48f), Vector2.one, titleFontSize);
                ConfigureText(descriptionText, Vector2.zero, new Vector2(1f, 0.48f), descriptionFontSize);
            }

            private static void ConfigureText(TMP_Text text, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
            {
                var rectTransform = text.rectTransform;
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.localScale = Vector3.one;

                text.enableAutoSizing = false;
                text.fontSize = fontSize;
                text.alignment = TextAlignmentOptions.Center;
            }
        }

        [Header("戻るボタン")]
        [SerializeField] public Button BackButton;
        [Header("エンディング実績（AからJの順）")]
        [SerializeField] private List<EndingTextPair> endingTexts = new();
        [Header("Extra Stage")]
        [SerializeField] private Button extraStageButton;
        [SerializeField] private TMP_Text extraStageButtonText;
        [Header("円形レイアウト")]
        [SerializeField] private Vector2 endingEllipseRadius = new(650f, 380f);
        [SerializeField] private float endingEllipseOffsetY;
        [SerializeField] private Vector2 endingPanelSize = new(280f, 90f);
        [Header("フォントサイズ")]
        [SerializeField, Min(1f)] private float endingTitleFontSize = 26f;
        [SerializeField, Min(1f)] private float endingDescriptionFontSize = 16f;
        [SerializeField, Min(1f)] private float extraStageFontSize = 32f;
        private string _unlockedExtraStageText;

        public Button ExtraStageButton => extraStageButton;

        public void CaptureUnlockedTexts()
        {
            if (endingTexts.Count != 10)
                throw new InvalidOperationException("Ending text pairs must contain exactly 10 entries (A-J).");

            foreach (var endingText in endingTexts) endingText.CaptureUnlockedText();
            _unlockedExtraStageText = extraStageButtonText.text;
            ArrangeEndingPanels();
            extraStageButtonText.enableAutoSizing = false;
            extraStageButtonText.fontSize = extraStageFontSize;
        }

        public void SetEndingUnlocked(int index, bool unlocked, bool showDescription) =>
            endingTexts[index].SetUnlocked(unlocked, showDescription);

        public void SetExtraStageUnlocked(bool unlocked)
        {
            extraStageButton.interactable = unlocked;
            extraStageButtonText.text = unlocked ? _unlockedExtraStageText : "???";
        }

        private void ArrangeEndingPanels()
        {
            var parent = endingTexts[0].Root.parent as RectTransform;
            var center = (Vector2)parent.InverseTransformPoint(extraStageButton.transform.position);
            center.y += endingEllipseOffsetY;
            var circleEndingCount = endingTexts.Count - 1;
            var angleStep = 360f / circleEndingCount;

            for (var index = 0; index < circleEndingCount; index++)
            {
                var angle = (90f - angleStep * index) * Mathf.Deg2Rad;
                var root = endingTexts[index].Root;
                root.anchorMin = new Vector2(0.5f, 0.5f);
                root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = endingPanelSize;
                root.anchoredPosition = center + new Vector2(
                    Mathf.Cos(angle) * endingEllipseRadius.x,
                    Mathf.Sin(angle) * endingEllipseRadius.y);
                root.localScale = Vector3.one;
                endingTexts[index].ConfigureLayout(endingTitleFontSize, endingDescriptionFontSize);
            }

            var lastEnding = endingTexts[^1].Root;
            lastEnding.anchorMin = new Vector2(0.5f, 0.5f);
            lastEnding.anchorMax = new Vector2(0.5f, 0.5f);
            lastEnding.pivot = new Vector2(0.5f, 0.5f);
            lastEnding.sizeDelta = endingPanelSize;
            lastEnding.anchoredPosition = new Vector2(0f, 150f);
            lastEnding.localScale = Vector3.one;
            endingTexts[^1].ConfigureLayout(endingTitleFontSize, endingDescriptionFontSize);
        }
    }
}
