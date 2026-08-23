using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Minge2026Spring.Scripts.Application.UseCase;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class AchievementDefaultUIView : MonoBehaviour
    {
        private const int EndingCount = 11;
        private const int RainbowEndingIndex = 10;
        private const string HaibokusyaMarkAddress = "HaibokusyaMark";
        private const string HaibokusyaVoiceEventPath = "event:/Serif/Ryuta_敗北者じゃけぇ";

        [Serializable]
        public class EndingTextPair
        {
            [SerializeField] private TMP_Text titleText;
            [SerializeField] private TMP_Text descriptionText;
            private string _unlockedTitle;
            private string _unlockedDescription;
            private bool _unlocked;
            private bool _showDescription;
            private int _endingIndex;

            public RectTransform Root => titleText.transform.parent as RectTransform;
            public TMP_Text TitleText => titleText;
            public string StatisticsText { get; set; }

            public void CaptureUnlockedText(int endingIndex)
            {
                _endingIndex = endingIndex;
                _unlockedTitle = AchievementEndingText.GetTitle(endingIndex);
                _unlockedDescription = AchievementEndingText.GetDescription(endingIndex);
            }

            public void SetUnlocked(bool unlocked, bool showDescription, int endingIndex)
            {
                _unlocked = unlocked;
                _showDescription = showDescription;
                _endingIndex = endingIndex;
                titleText.text = unlocked ? FormatTitle(_unlockedTitle, endingIndex) : "???";
                descriptionText.text = showDescription ? _unlockedDescription : "???";
                titleText.color = Color.white;
            }

            public void RefreshLocalizedText()
            {
                CaptureUnlockedText(_endingIndex);
                SetUnlocked(_unlocked, _showDescription, _endingIndex);
            }

            public void ConfigureLayout(float titleFontSize, float descriptionFontSize)
            {
                ConfigureText(titleText, new Vector2(0f, 0.48f), Vector2.one, titleFontSize);
                ConfigureText(descriptionText, Vector2.zero, new Vector2(1f, 0.48f), descriptionFontSize);
            }

            private static string FormatTitle(string title, int endingIndex)
            {
                if (endingIndex == RainbowEndingIndex)
                    return title;

                var color = Color.HSVToRGB(endingIndex / 10f, 0.8f, 1f);
                var colorHex = ColorUtility.ToHtmlStringRGB(color);
                var builder = new StringBuilder(title.Length + 64);
                foreach (var character in title)
                {
                    if (character is 'P' or 'D')
                        builder.Append("<size=115%><b><color=#").Append(colorHex).Append('>')
                            .Append(character).Append("</color></b></size>");
                    else
                        builder.Append(character);
                }

                return builder.ToString();
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
                text.raycastTarget = true;
            }
        }

        [Header("戻るボタン")]
        [SerializeField] public Button BackButton;
        [Header("エンディング実績（AからKの順）")]
        [SerializeField] private List<EndingTextPair> endingTexts = new();
        [Header("Extra Stage")]
        [SerializeField] private Button extraStageButton;
        [SerializeField] private TMP_Text extraStageButtonText;
        [Header("隠しアイテム")]
        [SerializeField] private Image hiddenItem1Image;
        [SerializeField] private TMP_Text hiddenItem1DescriptionText;
        [SerializeField] private Image hiddenItem2Image;
        [SerializeField] private TMP_Text hiddenItem2DescriptionText;
        [Header("円形レイアウト")]
        [SerializeField] private Vector2 endingEllipseRadius = new(650f, 380f);
        [SerializeField] private float endingEllipseOffsetY;
        [SerializeField] private Vector2 endingPanelSize = new(280f, 90f);
        [SerializeField] private Vector2 endingKPosition = new(0f, 150f);
        [Header("フォントサイズ")]
        [SerializeField, Min(1f)] private float endingTitleFontSize = 26f;
        [SerializeField, Min(1f)] private float endingDescriptionFontSize = 16f;
        [SerializeField, Min(1f)] private float extraStageFontSize = 32f;
        [Header("ホバー吹き出し")]
        [SerializeField] private Vector2 tooltipSize = new(330f, 130f);
        [SerializeField] private Vector2 extraStageTooltipSize = new(330f, 160f);
        [SerializeField] private Vector2 tooltipOffset = new(22f, 18f);
        [SerializeField] private Color tooltipColor = new(0.04f, 0.06f, 0.1f, 0.96f);
        [SerializeField] private Color tooltipBorderColor = new(0.55f, 0.85f, 1f, 0.95f);
        [SerializeField, Min(1f)] private float tooltipFontSize = 21f;
        [Header("Kエンド虹色")]
        [SerializeField, Min(0f)] private float rainbowSpeed = 0.2f;

        private string _unlockedExtraStageText;
        private TMP_Text _rainbowTitle;
        private bool _rainbowEnabled;
        private RectTransform _tooltip;
        private RectTransform _tooltipTail;
        private TMP_Text _tooltipText;
        private RectTransform _tooltipTextRect;
        private Image _tooltipHaibokusyaMarkImage;
        private Canvas _canvas;
        private TMP_Text _allPlayTimeText;
        private TMP_Text _allDeathCountText;
        private string _extraStageStatisticsText;
        private string _hiddenItem1Description;
        private string _hiddenItem2Description;
        private bool[] _haibokusyaFlags = new bool[EndingCount];
        private int _hoveredEndingIndex = -1;
        private AsyncOperationHandle<Sprite> _haibokusyaMarkHandle;
        private EventInstance _haibokusyaVoiceInstance;
        private readonly float[] _endingClearTimes = new float[EndingCount];
        private readonly int[] _endingDeathCounts = new int[EndingCount];
        private float _totalClearTime;
        private long _totalDeathCount;
        private ExtraStageProgress _extraStageProgress;
        private bool _hasExtraStageProgress;
        private bool _extraStageUnlocked;
        private bool _hasHiddenItem1;
        private bool _hasHiddenItem2;
        private bool _showHiddenDescriptions;

        public Button ExtraStageButton => extraStageButton;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnDestroy()
        {
            StopHaibokusyaVoice();

            if (_haibokusyaMarkHandle.IsValid())
                Addressables.Release(_haibokusyaMarkHandle);
        }

        public void CaptureUnlockedTexts()
        {
            if (endingTexts.Count != EndingCount)
                throw new InvalidOperationException("Ending text pairs must contain exactly 11 entries (A-K).");

            for (var index = 0; index < endingTexts.Count; index++)
                endingTexts[index].CaptureUnlockedText(index);

            _allPlayTimeText = FindSceneText("ALLPlayTimeText");
            _allDeathCountText = FindSceneText("ALLDeathCount");
            RefreshLocalizedSourceTexts();
            ArrangeEndingPanels();
            CreateTooltip();
            ConfigureHoverTargets();
            var extraStageHoverTarget = extraStageButton.gameObject.AddComponent<AchievementHoverTarget>();
            extraStageHoverTarget.Configure(ShowExtraStageTooltip, HideTooltip);
            extraStageButtonText.enableAutoSizing = false;
            extraStageButtonText.fontSize = extraStageFontSize;
            _rainbowTitle = endingTexts[RainbowEndingIndex].TitleText;
        }

        public void SetEndingUnlocked(int index, bool unlocked, bool showDescription)
        {
            endingTexts[index].SetUnlocked(unlocked, showDescription, index);
            if (index == RainbowEndingIndex)
                _rainbowEnabled = unlocked;
        }

        public void SetHaibokusyaFlags(bool[] flags)
        {
            if (flags == null || flags.Length != EndingCount)
                throw new ArgumentException("Haibokusya flags must contain exactly 11 entries (A-K).", nameof(flags));

            _haibokusyaFlags = flags;
            if (Array.Exists(flags, flag => flag))
                LoadHaibokusyaMarkAsync().Forget();
        }

        public void SetEndingStatistics(int index, float clearTimeSeconds, int deathCount)
        {
            _endingClearTimes[index] = clearTimeSeconds;
            _endingDeathCounts[index] = deathCount;
            endingTexts[index].StatisticsText = UILocalization.Get(
                "Achievements", "ending_statistics", FormatElapsedTime(clearTimeSeconds), deathCount);
        }

        public void SetTotalStatistics(float clearTimeSeconds, long deathCount)
        {
            _totalClearTime = clearTimeSeconds;
            _totalDeathCount = deathCount;
            _allPlayTimeText.text = UILocalization.Get(
                "Achievements", "total_play_time", FormatElapsedTime(clearTimeSeconds));
            _allDeathCountText.text = UILocalization.Get("Achievements", "total_deaths", deathCount);
        }

        public void SetExtraStageUnlocked(bool unlocked)
        {
            _extraStageUnlocked = unlocked;
            extraStageButton.interactable = unlocked;
            extraStageButtonText.text = unlocked ? _unlockedExtraStageText : "???";
        }

        public void SetExtraStageProgress(ExtraStageProgress progress)
        {
            _extraStageProgress = progress;
            _hasExtraStageProgress = true;
            var roomName = string.IsNullOrWhiteSpace(progress.RoomName) ? "---" : progress.RoomName;
            _extraStageStatisticsText = UILocalization.Get(
                "Achievements", "extra_statistics",
                FormatElapsedTime(progress.ElapsedPlayTimeSeconds), progress.DeathCount, roomName);
        }

        public void SetHiddenItemStatus(bool hasItem1, bool hasItem2, bool showDescriptions)
        {
            _hasHiddenItem1 = hasItem1;
            _hasHiddenItem2 = hasItem2;
            _showHiddenDescriptions = showDescriptions;
            hiddenItem1Image.color = hasItem1 ? Color.white : Color.black;
            hiddenItem2Image.color = hasItem2 ? Color.white : Color.black;
            hiddenItem1DescriptionText.text = showDescriptions ? _hiddenItem1Description : "???";
            hiddenItem2DescriptionText.text = showDescriptions ? _hiddenItem2Description : "???";
            hiddenItem1DescriptionText.gameObject.SetActive(true);
            hiddenItem2DescriptionText.gameObject.SetActive(true);
        }

        private void OnLocaleChanged(Locale _)
        {
            RefreshLocalizedSourceTexts();
            foreach (var endingText in endingTexts)
                endingText.RefreshLocalizedText();
            for (var index = 0; index < endingTexts.Count; index++)
                SetEndingStatistics(index, _endingClearTimes[index], _endingDeathCounts[index]);
            if (_allPlayTimeText != null && _allDeathCountText != null)
                SetTotalStatistics(_totalClearTime, _totalDeathCount);
            if (_hasExtraStageProgress)
                SetExtraStageProgress(_extraStageProgress);
            SetExtraStageUnlocked(_extraStageUnlocked);
            SetHiddenItemStatus(_hasHiddenItem1, _hasHiddenItem2, _showHiddenDescriptions);

            if (_tooltip != null && _tooltip.gameObject.activeSelf)
            {
                _tooltipText.text = _hoveredEndingIndex >= 0
                    ? $"ENDING {(char)('A' + _hoveredEndingIndex)}\n{endingTexts[_hoveredEndingIndex].StatisticsText}"
                    : $"EXTRA STAGE\n{_extraStageStatisticsText}";
            }
        }

        private void RefreshLocalizedSourceTexts()
        {
            _unlockedExtraStageText = UILocalization.Get("Achievements", "extra_stage");
            _hiddenItem1Description = UILocalization.Get("Achievements", "hidden.needle");
            _hiddenItem2Description = UILocalization.Get("Achievements", "hidden.warp");
        }

        private void Update()
        {
            if (!_rainbowEnabled || _rainbowTitle == null)
                return;

            AnimateRainbowTitle();
        }

        private void ArrangeEndingPanels()
        {
            var parent = endingTexts[0].Root.parent as RectTransform;
            var center = (Vector2)parent.InverseTransformPoint(extraStageButton.transform.position);
            center.y += endingEllipseOffsetY;
            const int circleEndingCount = 10;
            var angleStep = 360f / circleEndingCount;

            for (var index = 0; index < circleEndingCount; index++)
            {
                var angle = (90f - angleStep * index) * Mathf.Deg2Rad;
                ConfigureRoot(endingTexts[index].Root);
                endingTexts[index].Root.anchoredPosition = center + new Vector2(
                    Mathf.Cos(angle) * endingEllipseRadius.x,
                    Mathf.Sin(angle) * endingEllipseRadius.y);
                endingTexts[index].ConfigureLayout(endingTitleFontSize, endingDescriptionFontSize);
            }

            ConfigureRoot(endingTexts[RainbowEndingIndex].Root);
            endingTexts[RainbowEndingIndex].Root.anchoredPosition = endingKPosition;
            endingTexts[RainbowEndingIndex].ConfigureLayout(endingTitleFontSize, endingDescriptionFontSize);
        }

        private void ConfigureRoot(RectTransform root)
        {
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = endingPanelSize;
            root.localScale = Vector3.one;
        }

        private void ConfigureHoverTargets()
        {
            for (var index = 0; index < endingTexts.Count; index++)
            {
                var capturedIndex = index;
                var hoverTarget = endingTexts[index].Root.gameObject.AddComponent<AchievementHoverTarget>();
                hoverTarget.Configure(
                    eventData => ShowTooltip(capturedIndex, eventData),
                    HideTooltip);
            }
        }

        private void CreateTooltip()
        {
            _canvas = BackButton.GetComponentInParent<Canvas>();
            if (_canvas == null)
                throw new InvalidOperationException("Achievement view must be placed under a Canvas.");

            var tooltipObject = new GameObject("EndingStatisticsTooltip", typeof(RectTransform), typeof(Image), typeof(Outline));
            tooltipObject.layer = _canvas.gameObject.layer;
            tooltipObject.transform.SetParent(_canvas.transform, false);
            tooltipObject.transform.SetAsLastSibling();
            _tooltip = tooltipObject.GetComponent<RectTransform>();
            _tooltip.anchorMin = new Vector2(0.5f, 0.5f);
            _tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltip.sizeDelta = tooltipSize;

            var image = tooltipObject.GetComponent<Image>();
            image.color = tooltipColor;
            image.raycastTarget = false;
            var outline = tooltipObject.GetComponent<Outline>();
            outline.effectColor = tooltipBorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var tailObject = new GameObject("Tail", typeof(RectTransform), typeof(Image));
            tailObject.layer = _canvas.gameObject.layer;
            tailObject.transform.SetParent(_tooltip, false);
            _tooltipTail = tailObject.GetComponent<RectTransform>();
            _tooltipTail.sizeDelta = new Vector2(22f, 22f);
            _tooltipTail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var tailImage = tailObject.GetComponent<Image>();
            tailImage.color = tooltipColor;
            tailImage.raycastTarget = false;

            var textObject = new GameObject("Statistics", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = _canvas.gameObject.layer;
            textObject.transform.SetParent(_tooltip, false);
            _tooltipText = textObject.GetComponent<TextMeshProUGUI>();
            _tooltipText.font = endingTexts[0].TitleText.font;
            _tooltipText.fontSize = tooltipFontSize;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAlignmentOptions.MidlineLeft;
            _tooltipText.raycastTarget = false;
            _tooltipTextRect = _tooltipText.rectTransform;
            _tooltipTextRect.anchorMin = Vector2.zero;
            _tooltipTextRect.anchorMax = Vector2.one;
            _tooltipTextRect.offsetMin = new Vector2(22f, 12f);
            _tooltipTextRect.offsetMax = new Vector2(-18f, -12f);

            var markObject = new GameObject("HaibokusyaMark", typeof(RectTransform), typeof(Image));
            markObject.layer = _canvas.gameObject.layer;
            markObject.transform.SetParent(_tooltip, false);
            var markRect = markObject.GetComponent<RectTransform>();
            markRect.anchorMin = new Vector2(1f, 0.5f);
            markRect.anchorMax = new Vector2(1f, 0.5f);
            markRect.pivot = new Vector2(1f, 0.5f);
            markRect.anchoredPosition = new Vector2(-12f, 0f);
            markRect.sizeDelta = new Vector2(68f, 68f);
            _tooltipHaibokusyaMarkImage = markObject.GetComponent<Image>();
            _tooltipHaibokusyaMarkImage.preserveAspect = true;
            _tooltipHaibokusyaMarkImage.raycastTarget = false;
            markObject.SetActive(false);

            _tooltip.gameObject.SetActive(false);
        }

        private void ShowTooltip(int endingIndex, PointerEventData eventData)
        {
            _hoveredEndingIndex = endingIndex;
            _tooltip.sizeDelta = tooltipSize;
            _tooltipText.text = $"ENDING {(char)('A' + endingIndex)}\n{endingTexts[endingIndex].StatisticsText}";
            var showHaibokusyaMark = _haibokusyaFlags[endingIndex];
            _tooltipTextRect.offsetMax = new Vector2(showHaibokusyaMark ? -92f : -18f, -12f);
            _tooltipHaibokusyaMarkImage.gameObject.SetActive(showHaibokusyaMark && _tooltipHaibokusyaMarkImage.sprite != null);
            if (showHaibokusyaMark)
                PlayHaibokusyaVoice();
            PositionTooltip(eventData.position);
            _tooltip.gameObject.SetActive(true);
            _tooltip.SetAsLastSibling();
        }

        private void ShowExtraStageTooltip(PointerEventData eventData)
        {
            _hoveredEndingIndex = -1;
            _tooltip.sizeDelta = extraStageTooltipSize;
            _tooltipText.text = $"EXTRA STAGE\n{_extraStageStatisticsText}";
            _tooltipTextRect.offsetMax = new Vector2(-18f, -12f);
            _tooltipHaibokusyaMarkImage.gameObject.SetActive(false);
            PositionTooltip(eventData.position);
            _tooltip.gameObject.SetActive(true);
            _tooltip.SetAsLastSibling();
        }

        private void HideTooltip()
        {
            _hoveredEndingIndex = -1;
            _tooltip.gameObject.SetActive(false);
        }

        private void PlayHaibokusyaVoice()
        {
            StopHaibokusyaVoice();

            _haibokusyaVoiceInstance = RuntimeManager.CreateInstance(HaibokusyaVoiceEventPath);
            if (!_haibokusyaVoiceInstance.isValid())
            {
                Debug.LogError($"[AchievementDefaultUIView] FMOD event was not found: {HaibokusyaVoiceEventPath}");
                return;
            }

            _haibokusyaVoiceInstance.start();
        }

        private void StopHaibokusyaVoice()
        {
            if (!_haibokusyaVoiceInstance.isValid())
                return;

            _haibokusyaVoiceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _haibokusyaVoiceInstance.release();
            _haibokusyaVoiceInstance.clearHandle();
        }

        private async UniTaskVoid LoadHaibokusyaMarkAsync()
        {
            if (_tooltipHaibokusyaMarkImage == null || _haibokusyaMarkHandle.IsValid())
                return;

            try
            {
                _haibokusyaMarkHandle = Addressables.LoadAssetAsync<Sprite>(HaibokusyaMarkAddress);
                var sprite = await _haibokusyaMarkHandle.Task;
                if (this == null || sprite == null)
                    return;

                _tooltipHaibokusyaMarkImage.sprite = sprite;
                if (_hoveredEndingIndex >= 0 && _haibokusyaFlags[_hoveredEndingIndex])
                    _tooltipHaibokusyaMarkImage.gameObject.SetActive(true);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AchievementDefaultUIView] Failed to load Addressable sprite: {HaibokusyaMarkAddress}");
                Debug.LogException(exception);
            }
        }

        private void PositionTooltip(Vector2 screenPosition)
        {
            var canvasRect = _canvas.transform as RectTransform;
            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out var localPoint);

            var showOnLeft = screenPosition.x > Screen.width * 0.65f;
            var showAbove = screenPosition.y < Screen.height * 0.35f;
            _tooltip.pivot = new Vector2(showOnLeft ? 1f : 0f, showAbove ? 0f : 1f);
            _tooltipTail.anchorMin = _tooltipTail.anchorMax = new Vector2(showOnLeft ? 1f : 0f, showAbove ? 0f : 1f);
            _tooltipTail.pivot = new Vector2(0.5f, 0.5f);
            _tooltipTail.anchoredPosition = new Vector2(showOnLeft ? -8f : 8f, showAbove ? 8f : -8f);

            localPoint += new Vector2(showOnLeft ? -tooltipOffset.x : tooltipOffset.x,
                showAbove ? tooltipOffset.y : -tooltipOffset.y);
            var rect = canvasRect.rect;
            var currentTooltipSize = _tooltip.sizeDelta;
            localPoint.x = Mathf.Clamp(localPoint.x,
                rect.xMin + currentTooltipSize.x * _tooltip.pivot.x,
                rect.xMax - currentTooltipSize.x * (1f - _tooltip.pivot.x));
            localPoint.y = Mathf.Clamp(localPoint.y,
                rect.yMin + currentTooltipSize.y * _tooltip.pivot.y,
                rect.yMax - currentTooltipSize.y * (1f - _tooltip.pivot.y));
            _tooltip.anchoredPosition = localPoint;
        }

        private void AnimateRainbowTitle()
        {
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

        private static TMP_Text FindSceneText(string objectName)
        {
            var textObject = GameObject.Find(objectName);
            var text = textObject != null ? textObject.GetComponent<TMP_Text>() : null;
            if (text == null)
                throw new InvalidOperationException($"TMP text object was not found: {objectName}");
            return text;
        }

        private static string FormatElapsedTime(float totalSeconds)
        {
            var roundedSeconds = Math.Max(0L, (long)Math.Round(totalSeconds));
            var hours = roundedSeconds / 3600L;
            var minutes = roundedSeconds % 3600L / 60L;
            var seconds = roundedSeconds % 60L;
            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }
    }
}
