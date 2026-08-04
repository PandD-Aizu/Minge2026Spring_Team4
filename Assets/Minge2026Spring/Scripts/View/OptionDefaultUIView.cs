using R3;
using TMPro;
using System;
using UnityEngine;
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

        private void Awake()
        {
            if (voiceVolumeSlider is not null || seVolumeSlider is null)
                return;

            var sliderObject = Instantiate(seVolumeSlider.gameObject, seVolumeSlider.transform.parent);
            sliderObject.name = "VoiceVolumeSlider";
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            if (sliderRect is not null)
                sliderRect.anchoredPosition += Vector2.down * 100f;
            voiceVolumeSlider = sliderObject.GetComponent<Slider>();

            var seLabel = GameObject.Find("SEVolumeText");
            if (seLabel is not null)
            {
                var voiceLabel = Instantiate(seLabel, seLabel.transform.parent);
                voiceLabel.name = "VoiceVolumeText";
                var labelRect = voiceLabel.GetComponent<RectTransform>();
                if (labelRect is not null)
                    labelRect.anchoredPosition += Vector2.down * 100f;
                var label = voiceLabel.GetComponent<TextMeshProUGUI>();
                if (label is not null)
                    label.text = "Voice Volume";
            }
        }
        public void ShowSaveDeleteConfirmation(Action onConfirm)
        {
            if (_saveDeleteConfirmation is null)
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
            message.text = "セーブデータを削除します。\nこの操作は取り消せません。\nよろしいですか？";
            message.alignment = TextAlignmentOptions.Center;
            message.fontSize = 34f;
            var messageRect = message.rectTransform;
            messageRect.anchorMin = new Vector2(0.08f, 0.42f);
            messageRect.anchorMax = new Vector2(0.92f, 0.9f);
            messageRect.offsetMin = messageRect.offsetMax = Vector2.zero;

            var confirmButton = CreateDialogButton("ConfirmButton", "削除する", dialog.transform, new Vector2(-170f, -115f));
            var cancelButton = CreateDialogButton("CancelButton", "キャンセル", dialog.transform, new Vector2(170f, -115f));
            confirmButton.onClick.AddListener(() => { onConfirm(); overlay.SetActive(false); });
            cancelButton.onClick.AddListener(() => overlay.SetActive(false));
            return overlay;
        }

        private Button CreateDialogButton(string objectName, string label, Transform parent, Vector2 position)
        {
            var button = Instantiate(saveDeleteButton, parent);
            button.name = objectName;
            button.onClick.RemoveAllListeners();
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 80f);
            rect.anchoredPosition = position;
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text is not null) text.text = label;
            return button;
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
