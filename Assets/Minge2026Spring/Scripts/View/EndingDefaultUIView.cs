using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class EndingDefaultUIView : MonoBehaviour
    {
        private const string HaibokusyaVoiceEventPath = "event:/Serif/Ryuta_敗北者じゃけぇ";

        private const int CreditSectionCount = 5;

        [Serializable]
        private struct CreditImage
        {
            [Tooltip("この番号のクレジット項目の直後へ表示します。0ならタイトルの直後です。")]
            [Min(0)] public int insertAfterSection;
            public Sprite sprite;
            [Min(1f)] public float height;
        }

        private readonly struct ExtraDialogue
        {
            public readonly string EntryKey;
            public readonly string EventPath;

            public ExtraDialogue(string entryKey, string voiceText)
            {
                EntryKey = entryKey;
                EventPath = $"event:/Serif/主人公_{voiceText}";
            }
        }

        private static readonly ExtraDialogue[] ExtraDialogues =
        {
            new("extra_dialogue.0", "ここまで全部見たんだね。"),
            new("extra_dialogue.1", "ありがとう。"),
            new("extra_dialogue.2", "君は知っているかもしれないけど、"),
            new("extra_dialogue.3", "開発には正解なんてない。"),
            new("extra_dialogue.4", "誰が悪いわけでもない。"),
            new("extra_dialogue.5", "指示に最適解もない。"),
            new("extra_dialogue.6", "実装方法の"),
            new("extra_dialogue.7", "世界観の"),
            new("extra_dialogue.8", "答えはひとつじゃない。"),
            new("extra_dialogue.9", "それに"),
            new("extra_dialogue.10", "正しさを持つのは、"),
            new("extra_dialogue.11", "君だけじゃない。"),
            new("extra_dialogue.12", "……"),
            new("extra_dialogue.13", "これ以上は語らなくてもいいかな。"),
            new("extra_dialogue.14", "ここまで遊んでくれた君なら、"),
            new("extra_dialogue.15", "きっと良い企画開発者になれる。"),
            new("extra_dialogue.16", "最後に、"),
            new("extra_dialogue.17", "ここまで遊んでくれてありがとう。"),
            new("extra_dialogue.18", "Thank you for Playing")
        };

        [Header("Scene References")]
        [SerializeField] private GameObject _speedUpButtonObject;
        [SerializeField] private GameObject _skipButtonObject;

        public Button SpeedUpButton => _speedUpButtonObject != null
            ? _speedUpButtonObject.GetComponentInChildren<Button>(true)
            : null;
        public Button SkipButton => _skipButtonObject != null
            ? _skipButtonObject.GetComponentInChildren<Button>(true)
            : null;

        [Header("Scroll Settings")]
        [SerializeField] private float _scrollSpeed = 72f;
        [SerializeField] private float _maximumFastForward = 5f;
        [SerializeField] private float _acceleration = 2.5f;
        [SerializeField] private float _deceleration = 5f;
        [SerializeField] private float _bottomReservedHeight = 100f;
        [SerializeField] private float _startPadding = 760f;
        [SerializeField] private float _endPadding = 420f;
        [SerializeField, Range(0.1f, 1f)] private float _contentWidth = 0.76f;

        [Header("Credit Settings")]
        [SerializeField] private TMP_FontAsset _fontAsset;
        [SerializeField] private Color _textOutlineColor = new(0f, 0f, 0f, 0.8f);
        [SerializeField] private Vector2 _textOutlineDistance = new(1.5f, -1.5f);
        [SerializeField] private Color _textShadowColor = new(0f, 0f, 0f, 0.9f);
        [SerializeField] private Vector2 _textShadowDistance = new(4f, -4f);
        [SerializeField] private List<CreditImage> _images = new();

        [Header("Extra Ending Dialogue")]
        [SerializeField] private float _extraDialogueFontSize = 38f;
        [SerializeField, Min(0f)] private float _extraDialogueInterval = 0.35f;
        [Header("K Ending Rainbow")]
        [SerializeField, Min(0f)] private float _rainbowSpeed = 0.2f;
        [Header("Ending Title Display")]
        [SerializeField, Min(0f)] private float _endingTitleFadeDuration = 1f;
        [SerializeField, Min(0f)] private float _endingTitleHoldDuration = 5f;

        public event Action CreditFinished;
        public event Action ExtraDialogueFinished;

        private RectTransform _viewport;
        private RectTransform _content;
        private RectTransform _finalEntry;
        private GameObject _endingTitleOverlay;
        private CanvasGroup _endingTitleCanvasGroup;
        private TextMeshProUGUI _endingTitleText;
        private TextMeshProUGUI _endingConditionText;
        private TextMeshProUGUI _endingDeathCountText;
        private RectTransform _haibokusyaMarkRect;
        private Image _haibokusyaMarkImage;
        private GameObject _extraDialogueOverlay;
        private TextMeshProUGUI _extraDialogueText;
        private TMP_FontAsset _font;
        [SerializeField] private Sprite _haibokusyaMarkSprite;
        private bool _isFastForwardPressed;
        private bool _isScrolling;
        private bool _hasFinished;
        private bool _shouldShowSkipButton;
        private float _speedMultiplier = 1f;
        private EventInstance _extraDialogueInstance;
        private int _extraDialogueIndex;
        private float _nextDialogueTime;
        private bool _isPlayingExtraDialogue;
        private bool _isWaitingForNextDialogue;
        private bool _rainbowEndingTitle;
        private bool _isShowingEndingTitle;
        private float _endingTitleSequenceStartTime;
        private string _endingTitle;
        private string _endingCondition;
        private int _endingDeathCount;
        private bool _showHaibokusyaMark;
        private bool _haibokusyaVoicePlayed;
        private bool _isInitialized;
        private int _endingIndex = -1;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (_speedUpButtonObject == null)
            {
                Debug.LogError("[EndingDefaultUIView] SpeedUpButtonObject is not assigned.");
                enabled = false;
                return;
            }

            if (SpeedUpButton == null)
            {
                Debug.LogError("[EndingDefaultUIView] A Button component was not found under SpeedUpButtonObject.");
                enabled = false;
                return;
            }

            if (_skipButtonObject != null && SkipButton == null)
                Debug.LogError("[EndingDefaultUIView] A Button component was not found under SkipButtonObject.");

            var buttonLabel = SpeedUpButton.GetComponentInChildren<TextMeshProUGUI>();
            _font = _fontAsset != null ? _fontAsset : buttonLabel.font;

            BuildCreditRoll();
            BuildEndingTitleOverlay();
            BuildExtraDialogueOverlay();
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            StopExtraDialogueInstance();
        }

        public void StartScroll()
        {
            if (_extraDialogueOverlay != null)
                _extraDialogueOverlay.SetActive(false);
            if (_viewport != null)
                _viewport.gameObject.SetActive(true);
            _speedUpButtonObject.SetActive(true);
            if (_skipButtonObject != null)
                _skipButtonObject.SetActive(_shouldShowSkipButton);
            _isScrolling = true;
        }

        public void StartExtraDialogue()
        {
            _isScrolling = false;
            _viewport.gameObject.SetActive(false);
            _speedUpButtonObject.SetActive(false);
            if (_skipButtonObject != null)
                _skipButtonObject.SetActive(false);
            _extraDialogueOverlay.SetActive(true);
            _extraDialogueIndex = 0;
            _isPlayingExtraDialogue = true;
            PlayExtraDialogue();
        }

        public void SetEndingTitle(int endingIndex, bool showHaibokusyaMark, int deathCount)
        {
            _endingIndex = endingIndex;
            _endingTitle = AchievementEndingText.FormatTitle(endingIndex);
            _endingCondition = AchievementEndingText.GetDescription(endingIndex);
            _endingDeathCount = Mathf.Max(0, deathCount);
            _rainbowEndingTitle = endingIndex == AchievementEndingText.RainbowEndingIndex;
            _showHaibokusyaMark = showHaibokusyaMark;
            ApplyEndingText();
            if (_haibokusyaMarkImage != null)
                _haibokusyaMarkImage.gameObject.SetActive(_showHaibokusyaMark);
        }

        public void SetSkipButtonVisible(bool isVisible)
        {
            _shouldShowSkipButton = isVisible;
            if (_skipButtonObject != null)
                _skipButtonObject.SetActive(isVisible);
        }

        public void UpdateScroll()
        {
            UpdateExtraDialogue();
            UpdateEndingTitleSequence();
            UpdateRainbowEndingTitle();

            if (!_isScrolling || _content == null)
                return;

            var targetMultiplier = _isFastForwardPressed ? _maximumFastForward : 1f;
            var rate = _isFastForwardPressed ? _acceleration : _deceleration;
            _speedMultiplier = Mathf.MoveTowards(_speedMultiplier, targetMultiplier, rate * Time.unscaledDeltaTime);
            _content.anchoredPosition += Vector2.up * (_scrollSpeed * _speedMultiplier * Time.unscaledDeltaTime);

            if (_hasFinished || _finalEntry == null || _viewport == null)
                return;

            var viewportCorners = new Vector3[4];
            var finalCorners = new Vector3[4];
            _viewport.GetWorldCorners(viewportCorners);
            _finalEntry.GetWorldCorners(finalCorners);
            if (finalCorners[0].y <= viewportCorners[1].y + 20f)
                return;

            _hasFinished = true;
            _isScrolling = false;
            StartEndingTitleSequence();
        }

        public void SetFastForwardPressed(bool isPressed)
        {
            _isFastForwardPressed = isPressed;
        }

        private void BuildCreditRoll()
        {
            var canvas = _speedUpButtonObject.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EndingDefaultUIView] Canvas was not found.");
                return;
            }

            var viewportObject = new GameObject("CreditViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.layer = 5;
            viewportObject.transform.SetParent(canvas.transform, false);
            viewportObject.transform.SetAsFirstSibling();
            _viewport = viewportObject.GetComponent<RectTransform>();
            _viewport.anchorMin = new Vector2(0f, 0f);
            _viewport.anchorMax = new Vector2(1f, 1f);
            _viewport.offsetMin = new Vector2(0f, _bottomReservedHeight);
            _viewport.offsetMax = Vector2.zero;

            var contentObject = new GameObject("CreditContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.layer = 5;
            contentObject.transform.SetParent(_viewport, false);
            _content = contentObject.GetComponent<RectTransform>();
            var horizontalMargin = (1f - _contentWidth) * 0.5f;
            _content.anchorMin = new Vector2(horizontalMargin, 1f);
            _content.anchorMax = new Vector2(1f - horizontalMargin, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = new Vector2(0f, -_startPadding);
            _content.sizeDelta = Vector2.zero;

            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateSpacer(_startPadding);
            CreateLocalizedText("credit.title", 52f, FontStyles.Bold, Color.white);
            CreateSpacer(70f);
            CreateImagesAt(0);
            CreateSpacer(130f);

            for (var index = 0; index < CreditSectionCount; index++)
            {
                CreateLocalizedText($"credit.{index}.title", 34f, FontStyles.Bold, Color.white);
                CreateSpacer(18f);
                CreateLocalizedText($"credit.{index}.names", 27f, FontStyles.Normal,
                    new Color(0.92f, 0.96f, 1f));
                CreateImagesAt(index + 1);
                CreateSpacer(105f);
            }

            _finalEntry = CreateLocalizedText("credit.final_message", 44f, FontStyles.Bold,
                new Color(1f, 0.92f, 0.45f)).rectTransform;
            CreateSpacer(_endPadding);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private void BuildEndingTitleOverlay()
        {
            var canvas = _speedUpButtonObject.GetComponentInParent<Canvas>();
            _endingTitleOverlay = new GameObject("EndingTitleOverlay", typeof(RectTransform), typeof(CanvasGroup));
            _endingTitleOverlay.layer = 5;
            _endingTitleOverlay.transform.SetParent(canvas.transform, false);

            var overlayRect = _endingTitleOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            _endingTitleCanvasGroup = _endingTitleOverlay.GetComponent<CanvasGroup>();
            _endingTitleCanvasGroup.alpha = 0f;
            _endingTitleCanvasGroup.interactable = false;
            _endingTitleCanvasGroup.blocksRaycasts = false;

            var textObject = new GameObject("EndingTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = 5;
            textObject.transform.SetParent(overlayRect, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.48f);
            textRect.anchorMax = new Vector2(0.92f, 0.65f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _endingTitleText = textObject.GetComponent<TextMeshProUGUI>();
            _endingTitleText.font = _font;
            _endingTitleText.fontSize = 52f;
            _endingTitleText.fontStyle = FontStyles.Bold;
            _endingTitleText.color = Color.white;
            _endingTitleText.alignment = TextAlignmentOptions.Center;
            _endingTitleText.textWrappingMode = TextWrappingModes.Normal;
            _endingTitleText.raycastTarget = false;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = _textOutlineColor;
            outline.effectDistance = _textOutlineDistance;
            outline.useGraphicAlpha = true;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = _textShadowColor;
            shadow.effectDistance = _textShadowDistance;
            shadow.useGraphicAlpha = true;

            var conditionObject = new GameObject("EndingCondition", typeof(RectTransform), typeof(TextMeshProUGUI));
            conditionObject.layer = 5;
            conditionObject.transform.SetParent(overlayRect, false);
            var conditionRect = conditionObject.GetComponent<RectTransform>();
            conditionRect.anchorMin = new Vector2(0.12f, 0.34f);
            conditionRect.anchorMax = new Vector2(0.88f, 0.48f);
            conditionRect.offsetMin = Vector2.zero;
            conditionRect.offsetMax = Vector2.zero;

            _endingConditionText = conditionObject.GetComponent<TextMeshProUGUI>();
            _endingConditionText.font = _font;
            _endingConditionText.fontSize = 30f;
            _endingConditionText.fontStyle = FontStyles.Normal;
            _endingConditionText.color = new Color(0.92f, 0.96f, 1f);
            _endingConditionText.alignment = TextAlignmentOptions.Top;
            _endingConditionText.textWrappingMode = TextWrappingModes.Normal;
            _endingConditionText.raycastTarget = false;

            var conditionOutline = conditionObject.AddComponent<Outline>();
            conditionOutline.effectColor = _textOutlineColor;
            conditionOutline.effectDistance = _textOutlineDistance;
            conditionOutline.useGraphicAlpha = true;
            var conditionShadow = conditionObject.AddComponent<Shadow>();
            conditionShadow.effectColor = _textShadowColor;
            conditionShadow.effectDistance = _textShadowDistance;
            conditionShadow.useGraphicAlpha = true;

            var deathCountObject = new GameObject("EndingDeathCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            deathCountObject.layer = 5;
            deathCountObject.transform.SetParent(overlayRect, false);
            var deathCountRect = deathCountObject.GetComponent<RectTransform>();
            deathCountRect.anchorMin = new Vector2(0.12f, 0.24f);
            deathCountRect.anchorMax = new Vector2(0.88f, 0.34f);
            deathCountRect.offsetMin = Vector2.zero;
            deathCountRect.offsetMax = Vector2.zero;

            _endingDeathCountText = deathCountObject.GetComponent<TextMeshProUGUI>();
            _endingDeathCountText.font = _font;
            _endingDeathCountText.fontSize = 26f;
            _endingDeathCountText.color = new Color(0.92f, 0.96f, 1f);
            _endingDeathCountText.alignment = TextAlignmentOptions.Top;
            _endingDeathCountText.textWrappingMode = TextWrappingModes.Normal;
            _endingDeathCountText.raycastTarget = false;

            var deathCountOutline = deathCountObject.AddComponent<Outline>();
            deathCountOutline.effectColor = _textOutlineColor;
            deathCountOutline.effectDistance = _textOutlineDistance;
            deathCountOutline.useGraphicAlpha = true;
            var deathCountShadow = deathCountObject.AddComponent<Shadow>();
            deathCountShadow.effectColor = _textShadowColor;
            deathCountShadow.effectDistance = _textShadowDistance;
            deathCountShadow.useGraphicAlpha = true;

            var markObject = new GameObject("HaibokusyaMark", typeof(RectTransform), typeof(Image));
            markObject.layer = 5;
            markObject.transform.SetParent(overlayRect, false);
            markObject.transform.SetAsFirstSibling();
            _haibokusyaMarkRect = markObject.GetComponent<RectTransform>();
            _haibokusyaMarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            _haibokusyaMarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            _haibokusyaMarkRect.pivot = new Vector2(0.5f, 0.5f);
            _haibokusyaMarkRect.anchoredPosition = new Vector2(0f, 60f);
            _haibokusyaMarkRect.sizeDelta = new Vector2(1000f, 1000f);
            _haibokusyaMarkImage = markObject.GetComponent<Image>();
            _haibokusyaMarkImage.sprite = _haibokusyaMarkSprite;
            _haibokusyaMarkImage.color = new Color(1f, 1f, 1f, 0.28f);
            _haibokusyaMarkImage.preserveAspect = true;
            _haibokusyaMarkImage.raycastTarget = false;
            markObject.SetActive(false);

            ApplyEndingText();

            _endingTitleOverlay.SetActive(false);
        }

        private void BuildExtraDialogueOverlay()
        {
            var canvas = _speedUpButtonObject.GetComponentInParent<Canvas>();
            _extraDialogueOverlay = new GameObject("ExtraEndingDialogue", typeof(RectTransform));
            _extraDialogueOverlay.layer = 5;
            _extraDialogueOverlay.transform.SetParent(canvas.transform, false);

            var overlayRect = _extraDialogueOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var textObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = 5;
            textObject.transform.SetParent(overlayRect, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.25f);
            textRect.anchorMax = new Vector2(0.9f, 0.75f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _extraDialogueText = textObject.GetComponent<TextMeshProUGUI>();
            _extraDialogueText.font = _font;
            _extraDialogueText.fontSize = _extraDialogueFontSize;
            _extraDialogueText.fontStyle = FontStyles.Bold;
            _extraDialogueText.color = Color.white;
            _extraDialogueText.alignment = TextAlignmentOptions.Center;
            _extraDialogueText.textWrappingMode = TextWrappingModes.Normal;
            _extraDialogueText.raycastTarget = false;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = _textOutlineColor;
            outline.effectDistance = _textOutlineDistance;
            outline.useGraphicAlpha = true;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = _textShadowColor;
            shadow.effectDistance = _textShadowDistance;
            shadow.useGraphicAlpha = true;

            _extraDialogueOverlay.SetActive(false);
        }

        private void UpdateExtraDialogue()
        {
            if (!_isPlayingExtraDialogue)
                return;

            if (_isWaitingForNextDialogue)
            {
                if (Time.unscaledTime < _nextDialogueTime)
                    return;

                _isWaitingForNextDialogue = false;
                _extraDialogueIndex++;
                if (_extraDialogueIndex >= ExtraDialogues.Length)
                {
                    _isPlayingExtraDialogue = false;
                    ExtraDialogueFinished?.Invoke();
                    return;
                }

                PlayExtraDialogue();
                return;
            }

            if (!_extraDialogueInstance.isValid())
            {
                WaitForNextDialogue();
                return;
            }

            _extraDialogueInstance.getPlaybackState(out var playbackState);
            if (playbackState != PLAYBACK_STATE.STOPPED)
                return;

            StopExtraDialogueInstance();
            WaitForNextDialogue();
        }

        private void PlayExtraDialogue()
        {
            var dialogue = ExtraDialogues[_extraDialogueIndex];
            _extraDialogueText.text = UILocalization.Get("Ending", dialogue.EntryKey);
            _extraDialogueInstance = RuntimeManager.CreateInstance(dialogue.EventPath);
            if (!_extraDialogueInstance.isValid())
            {
                Debug.LogError($"[EndingDefaultUIView] Extra ending dialogue event was not found: {dialogue.EventPath}");
                WaitForNextDialogue();
                return;
            }

            _extraDialogueInstance.start();
        }

        private void WaitForNextDialogue()
        {
            _nextDialogueTime = Time.unscaledTime + _extraDialogueInterval;
            _isWaitingForNextDialogue = true;
        }

        private void StopExtraDialogueInstance()
        {
            if (!_extraDialogueInstance.isValid())
                return;

            _extraDialogueInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _extraDialogueInstance.release();
            _extraDialogueInstance.clearHandle();
        }

        private void StartEndingTitleSequence()
        {
            _viewport.gameObject.SetActive(false);
            _speedUpButtonObject.SetActive(false);
            if (_skipButtonObject != null)
                _skipButtonObject.SetActive(false);

            ApplyEndingText();
            _haibokusyaMarkImage.gameObject.SetActive(_showHaibokusyaMark);
            _endingTitleCanvasGroup.alpha = 0f;
            _endingTitleOverlay.SetActive(true);
            PlayHaibokusyaVoice();
            _endingTitleSequenceStartTime = Time.unscaledTime;
            _isShowingEndingTitle = true;
        }

        private void UpdateEndingTitleSequence()
        {
            if (!_isShowingEndingTitle)
                return;

            var elapsed = Time.unscaledTime - _endingTitleSequenceStartTime;
            _endingTitleCanvasGroup.alpha = _endingTitleFadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / _endingTitleFadeDuration);

            if (elapsed < _endingTitleFadeDuration + _endingTitleHoldDuration)
                return;

            _isShowingEndingTitle = false;
            CreditFinished?.Invoke();
        }

        private void ApplyEndingText()
        {
            if (_endingTitleText != null && !string.IsNullOrEmpty(_endingTitle))
            {
                _endingTitleText.text = _endingTitle;
                _endingTitleText.ForceMeshUpdate();
            }
            if (_endingConditionText != null && !string.IsNullOrEmpty(_endingCondition))
                _endingConditionText.text = _endingCondition;
            if (_endingDeathCountText != null)
                _endingDeathCountText.text = UILocalization.Get("Ending", "death_count", _endingDeathCount);
        }

        private void OnLocaleChanged(Locale _)
        {
            if (_endingIndex >= 0)
            {
                _endingTitle = AchievementEndingText.FormatTitle(_endingIndex);
                _endingCondition = AchievementEndingText.GetDescription(_endingIndex);
                ApplyEndingText();
            }

            if (_isPlayingExtraDialogue && _extraDialogueText != null &&
                _extraDialogueIndex >= 0 && _extraDialogueIndex < ExtraDialogues.Length)
            {
                _extraDialogueText.text = UILocalization.Get(
                    "Ending", ExtraDialogues[_extraDialogueIndex].EntryKey);
            }
        }

        private void PlayHaibokusyaVoice()
        {
            if (!_showHaibokusyaMark || _haibokusyaVoicePlayed)
                return;

            _haibokusyaVoicePlayed = true;
            var instance = RuntimeManager.CreateInstance(HaibokusyaVoiceEventPath);
            if (!instance.isValid())
            {
                Debug.LogError($"[EndingDefaultUIView] FMOD event was not found: {HaibokusyaVoiceEventPath}");
                return;
            }

            instance.start();
            instance.release();
        }

        private void UpdateRainbowEndingTitle()
        {
            if (!_rainbowEndingTitle || _endingTitleText == null || !_endingTitleText.isActiveAndEnabled)
                return;

            _endingTitleText.ForceMeshUpdate();
            var textInfo = _endingTitleText.textInfo;
            var visibleIndex = 0;
            var visibleCount = Mathf.Max(1, textInfo.characterCount);
            for (var index = 0; index < textInfo.characterCount; index++)
            {
                var character = textInfo.characterInfo[index];
                if (!character.isVisible)
                    continue;

                var colors = textInfo.meshInfo[character.materialReferenceIndex].colors32;
                var color = (Color32)Color.HSVToRGB(
                    Mathf.Repeat(Time.unscaledTime * _rainbowSpeed + visibleIndex / (float)visibleCount, 1f),
                    0.85f,
                    1f);
                for (var vertex = 0; vertex < 4; vertex++)
                    colors[character.vertexIndex + vertex] = color;
                visibleIndex++;
            }

            _endingTitleText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void CreateImagesAt(int sectionIndex)
        {
            foreach (var creditImage in _images)
            {
                if (creditImage.insertAfterSection != sectionIndex || creditImage.sprite == null)
                    continue;

                CreateSpacer(35f);
                var imageObject = new GameObject("CreditImage", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                imageObject.layer = 5;
                imageObject.transform.SetParent(_content, false);

                var image = imageObject.GetComponent<Image>();
                image.sprite = creditImage.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;

                var layout = imageObject.GetComponent<LayoutElement>();
                layout.preferredHeight = creditImage.height > 0f ? creditImage.height : 240f;
                layout.flexibleWidth = 1f;
            }
        }

        private TextMeshProUGUI CreateText(string value, float fontSize, FontStyles style, Color color)
        {
            var textObject = new GameObject("CreditText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.layer = 5;
            textObject.transform.SetParent(_content, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = _textOutlineColor;
            outline.effectDistance = _textOutlineDistance;
            outline.useGraphicAlpha = true;

            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = _textShadowColor;
            shadow.effectDistance = _textShadowDistance;
            shadow.useGraphicAlpha = true;

            var layout = textObject.GetComponent<LayoutElement>();
            layout.minHeight = fontSize * (value.Contains("\n") ? 2.6f : 1.5f);
            return text;
        }

        private TextMeshProUGUI CreateLocalizedText(string entryKey, float fontSize, FontStyles style, Color color)
        {
            var text = CreateText(UILocalization.Get("Ending", entryKey), fontSize, style, color);
            UILocalization.Bind(text, "Ending", entryKey);
            return text;
        }

        private void CreateSpacer(float height)
        {
            var spacer = new GameObject("CreditSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.layer = 5;
            spacer.transform.SetParent(_content, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
