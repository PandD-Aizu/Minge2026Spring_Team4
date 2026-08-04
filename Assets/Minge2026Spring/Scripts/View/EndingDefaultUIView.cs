using System;
using System.Collections.Generic;
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

        [Header("Scene References")]
        [SerializeField] public Button backButton;

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
        [SerializeField] private string _title = "I GONNA BE THE TREASURE HUNTER";
        [SerializeField] private string _finalMessage = "THANK YOU FOR PLAYING!";
        [SerializeField] private Color _textShadowColor = new(0f, 0f, 0f, 0.7f);
        [SerializeField] private Vector2 _textShadowDistance = new(3f, -3f);
        [SerializeField] private List<CreditSection> _credits = new()
        {
            new CreditSection("PLANNING / DIRECTOR", "主人公"),
            new CreditSection("PROGRAMMERS", "Ryuta\nごっと"),
            new CreditSection("2D ART", "Milu"),
            new CreditSection("SOUND", "かしわもち"),
            new CreditSection("SPECIAL THANKS", "企画開発部\nPLAYERS")
        };
        [SerializeField] private List<CreditImage> _images = new();

        public event Action CreditFinished;

        private RectTransform _viewport;
        private RectTransform _content;
        private RectTransform _finalEntry;
        private TextMeshProUGUI _endingTitleText;
        private TMP_FontAsset _font;
        private bool _isFastForwardPressed;
        private bool _isScrolling;
        private bool _hasFinished;
        private float _speedMultiplier = 1f;

        private void Awake()
        {
            if (backButton == null)
            {
                Debug.LogError("[EndingDefaultUIView] SpeedUpButton is not assigned.");
                enabled = false;
                return;
            }

            var buttonLabel = backButton.GetComponentInChildren<TextMeshProUGUI>();
            _font = buttonLabel != null ? buttonLabel.font : null;

            BuildCreditRoll();
        }

        public void StartScroll()
        {
            _isScrolling = true;
        }

        public void SetEndingTitle(string endingTitle)
        {
            if (_endingTitleText != null)
                _endingTitleText.text = endingTitle;
        }

        public void UpdateScroll()
        {
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
            var canvas = backButton.GetComponentInParent<Canvas>();
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
            var textObject = new GameObject("CreditText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Shadow), typeof(LayoutElement));
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

            var shadow = textObject.GetComponent<Shadow>();
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
