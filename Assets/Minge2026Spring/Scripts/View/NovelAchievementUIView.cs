using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

            public void SetText(int index, bool unlocked, bool showDescription)
            {
                titleText.text = unlocked ? AchievementEndingText.FormatTitle(index) : "???";
                descriptionText.text = showDescription ? AchievementEndingText.GetDescription(index) : "???";
                titleText.color = Color.white;
            }
        }

        [Header("エンディング実績（AからKの順）")]
        [SerializeField] private List<EndingTextPair> endingTexts = new();
        [Header("Kエンド虹色")]
        [SerializeField, Min(0f)] private float rainbowSpeed = 0.2f;

        private TMP_Text _rainbowTitle;
        private bool _rainbowEnabled;

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
    }
}
