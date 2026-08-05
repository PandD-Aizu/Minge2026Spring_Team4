using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class EndingDefaultUIView : MonoBehaviour
    {
        [Serializable]
        private struct CreditSection
        {
            public string title;
            [TextArea] public string names;

            public CreditSection(string title, string names)
            {
                this.title = title;
                this.names = names;
            }
        }

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
            public readonly string Text;
            public readonly string EventPath;

            public ExtraDialogue(string text)
            {
                Text = text;
                EventPath = $"event:/Serif/主人公_{text}";
            }
        }

        private static readonly ExtraDialogue[] ExtraDialogues =
        {
            new("ここまで全部見たんだね。"),
            new("ありがとう。"),
            new("君は知っているかもしれないけど、"),
            new("開発には正解なんてない。"),
            new("誰が悪いわけでもない。"),
            new("指示に最適解もない。"),
            new("実装方法の"),
            new("世界観の"),
            new("答えはひとつじゃない。"),
            new("それに"),
            new("正しさを持つのは、"),
            new("君だけじゃない。"),
            new("……"),
            new("これ以上は語らなくてもいいかな。"),
            new("ここまで遊んでくれた君なら、"),
            new("きっと良い企画開発者になれる。"),
            new("最後に、"),
            new("ここまで遊んでくれてありがとう。"),
            new("Thank you for Playing")
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
        [SerializeField] private string _title = "I GONNA BE THE TREASURE HUNTER";
        [SerializeField] private string _finalMessage = "THANK YOU FOR PLAYING!";
        [SerializeField] private Color _textOutlineColor = new(0f, 0f, 0f, 0.8f);
        [SerializeField] private Vector2 _textOutlineDistance = new(1.5f, -1.5f);
        [SerializeField] private Color _textShadowColor = new(0f, 0f, 0f, 0.9f);
        [SerializeField] private Vector2 _textShadowDistance = new(4f, -4f);
        [SerializeField] private List<CreditSection> _credits = new()
        {
            new CreditSection("PLANNING / DIRECTOR", "主人公"),
            new CreditSection("PROGRAMMERS", "Ryuta\nごっと"),
            new CreditSection("2D ART", "Milu"),
            new CreditSection("SOUND", "かしわもち"),
            new CreditSection("SPECIAL THANKS", "企画開発部\nPLAYERS")
        };
        [SerializeField] private List<CreditImage> _images = new();

        [Header("Extra Ending Dialogue")]
        [SerializeField] private float _extraDialogueFontSize = 38f;
        [SerializeField, Min(0f)] private float _extraDialogueInterval = 0.35f;

        public event Action CreditFinished;
        public event Action ExtraDialogueFinished;

        private RectTransform _viewport;
        private RectTransform _content;
        private RectTransform _finalEntry;
        private TextMeshProUGUI _endingTitleText;
        private GameObject _extraDialogueOverlay;
        private TextMeshProUGUI _extraDialogueText;
        private TMP_FontAsset _font;
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

        private void Awake()
        {
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
            BuildExtraDialogueOverlay();
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

        public void SetEndingTitle(string endingTitle)
        {
            if (_endingTitleText != null)
                _endingTitleText.text = endingTitle;
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
            CreditFinished?.Invoke();
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
            CreateText(_title, 52f, FontStyles.Bold, Color.white);
            CreateSpacer(70f);
            _endingTitleText = CreateText(string.Empty, 42f, FontStyles.Bold, Color.white);
            CreateImagesAt(0);
            CreateSpacer(130f);

            for (var index = 0; index < _credits.Count; index++)
            {
                var credit = _credits[index];
                CreateText(credit.title, 34f, FontStyles.Bold, Color.white);
                CreateSpacer(18f);
                CreateText(credit.names, 27f, FontStyles.Normal, new Color(0.92f, 0.96f, 1f));
                CreateImagesAt(index + 1);
                CreateSpacer(105f);
            }

            _finalEntry = CreateText(_finalMessage, 44f, FontStyles.Bold, new Color(1f, 0.92f, 0.45f)).rectTransform;
            CreateSpacer(_endPadding);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
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
            _extraDialogueText.text = dialogue.Text;
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

        private void CreateSpacer(float height)
        {
            var spacer = new GameObject("CreditSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.layer = 5;
            spacer.transform.SetParent(_content, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
