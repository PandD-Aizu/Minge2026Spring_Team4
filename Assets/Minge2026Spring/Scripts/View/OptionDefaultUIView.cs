using R3;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class OptionDefaultUIView : MonoBehaviour
    {
        [Header("音量スライダー")]
        [SerializeField] public Slider mainVolumeSlider;
        [SerializeField] public Slider bgmVolumeSlider;
        [SerializeField] public Slider seVolumeSlider;
        [SerializeField] public Slider voiceVolumeSlider;

        [Header("タイトルに戻るボタン")] 
        [SerializeField] public Button backButton;

        [Header("セーブデータ削除ボタン")]
        [SerializeField] public Button saveDeleteButton;

        private GameObject _saveDeleteConfirmation;
        private Button _languageButton;
        private TMP_Text _languageButtonText;

        private void Awake()
        {
            EnsureVoiceVolumeSlider();
            CreateLanguageSelector();
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void EnsureVoiceVolumeSlider()
        {
            if (voiceVolumeSlider != null || seVolumeSlider == null)
                return;

            var sliderObject = Instantiate(seVolumeSlider.gameObject, seVolumeSlider.transform.parent);
            sliderObject.name = "VoiceVolumeSlider";
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            if (sliderRect != null)
                sliderRect.anchoredPosition += Vector2.down * 100f;
            voiceVolumeSlider = sliderObject.GetComponent<Slider>();

            var seLabel = GameObject.Find("SEVolumeText");
            if (seLabel != null)
            {
                var voiceLabel = Instantiate(seLabel, seLabel.transform.parent);
                voiceLabel.name = "VoiceVolumeText";
                var labelRect = voiceLabel.GetComponent<RectTransform>();
                if (labelRect != null)
                    labelRect.anchoredPosition += Vector2.down * 100f;
                var label = voiceLabel.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    RemoveClonedLocalizers(voiceLabel);
                    UILocalization.Bind(label, "Options", "voice_volume");
                }
            }
        }

        private void CreateLanguageSelector()
        {
            if (saveDeleteButton == null || _languageButton != null)
                return;

            _languageButton = Instantiate(saveDeleteButton, saveDeleteButton.transform.parent);
            _languageButton.name = "LanguageButton";
            _languageButton.onClick.RemoveAllListeners();
            RemoveClonedLocalizers(_languageButton.gameObject);

            var sourceRect = saveDeleteButton.GetComponent<RectTransform>();
            var rect = _languageButton.GetComponent<RectTransform>();
            rect.anchorMin = sourceRect.anchorMin;
            rect.anchorMax = sourceRect.anchorMax;
            rect.pivot = sourceRect.pivot;
            rect.sizeDelta = sourceRect.sizeDelta;
            rect.anchoredPosition = new Vector2(0f, sourceRect.anchoredPosition.y);

            _languageButtonText = _languageButton.GetComponentInChildren<TMP_Text>();
            if (_languageButtonText != null)
            {
                _languageButtonText.enableAutoSizing = true;
                _languageButtonText.fontSizeMin = 18f;
                _languageButtonText.fontSizeMax = 36f;
            }

            _languageButton.onClick.AddListener(ToggleLocale);
            RefreshLanguageSelector();
        }

        private void ToggleLocale()
        {
            var nextLocale = UILocalization.CurrentLocaleCode == UILocalization.JapaneseLocaleCode
                ? UILocalization.EnglishLocaleCode
                : UILocalization.JapaneseLocaleCode;
            UILocalization.SelectLocale(nextLocale);
        }

        private void OnLocaleChanged(Locale _) => RefreshLanguageSelector();

        private void RefreshLanguageSelector()
        {
            if (_languageButtonText == null)
                return;

            var languageNameKey = UILocalization.CurrentLocaleCode == UILocalization.JapaneseLocaleCode
                ? "language_name_ja"
                : "language_name_en";
            var languageName = UILocalization.Get("Options", languageNameKey);
            _languageButtonText.text = UILocalization.Get("Options", "language_current", languageName);
        }

        public void ShowSaveDeleteConfirmation(Action onConfirm)
        {
            if (_saveDeleteConfirmation == null)
                _saveDeleteConfirmation = CreateSaveDeleteConfirmation(onConfirm);
            _saveDeleteConfirmation.SetActive(true);
        }

        private GameObject CreateSaveDeleteConfirmation(Action onConfirm)
        {
            var canvas = saveDeleteButton.GetComponentInParent<Canvas>();
            var overlay = CreateImageObject("SaveDeleteConfirmation", canvas.transform, new Color(0f, 0f, 0f, 0.7f));
            StretchToParent(overlay.GetComponent<RectTransform>());
            var dialog = CreateImageObject("Dialog", overlay.transform, new Color(0.12f, 0.12f, 0.12f, 1f));
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(720f, 360f);

            var message = Instantiate(saveDeleteButton.GetComponentInChildren<TMP_Text>(), dialog.transform);
            message.name = "WarningText";
            RemoveClonedLocalizers(message.gameObject);
            UILocalization.Bind(message, "Options", "delete_warning");
            message.alignment = TextAlignmentOptions.Center;
            message.fontSize = 34f;
            var messageRect = message.rectTransform;
            messageRect.anchorMin = new Vector2(0.08f, 0.42f);
            messageRect.anchorMax = new Vector2(0.92f, 0.9f);
            messageRect.offsetMin = messageRect.offsetMax = Vector2.zero;

            var confirmButton = CreateDialogButton("ConfirmButton", "confirm_delete", dialog.transform, new Vector2(-170f, -115f));
            var cancelButton = CreateDialogButton("CancelButton", "cancel", dialog.transform, new Vector2(170f, -115f));
            confirmButton.onClick.AddListener(() => { onConfirm(); overlay.SetActive(false); });
            cancelButton.onClick.AddListener(() => overlay.SetActive(false));
            return overlay;
        }

        private Button CreateDialogButton(string objectName, string entryKey, Transform parent, Vector2 position)
        {
            var button = Instantiate(saveDeleteButton, parent);
            button.name = objectName;
            button.onClick.RemoveAllListeners();
            RemoveClonedLocalizers(button.gameObject);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 80f);
            rect.anchoredPosition = position;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
                UILocalization.Bind(text, "Options", entryKey);
            return button;
        }

        private static void RemoveClonedLocalizers(GameObject root)
        {
            foreach (var localizer in root.GetComponentsInChildren<LocalizeStringEvent>(true))
            {
                localizer.enabled = false;
                Destroy(localizer);
            }
        }

        private static GameObject CreateImageObject(string objectName, Transform parent, Color color)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
