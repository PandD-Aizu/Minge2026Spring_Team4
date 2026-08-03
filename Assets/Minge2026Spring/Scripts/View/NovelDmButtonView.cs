using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    /// <summary>
    /// ノベルシーン左上にDMボタンを生成するView
    /// </summary>
    public sealed class NovelDmButtonView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("DM機能UIで使用するTextMeshProフォント。未設定時は既定フォントを使用")]
        private TMP_FontAsset dmFont;

        public event Action DmClicked;
        public event Action<string> CharacterDmClicked;
        public event Action DmBackClicked;

        private Button _button;
        private Transform _leftPanel;
        private GameObject _dmMenuObject;

        private static readonly (string AssetKey, string DisplayName)[] DmCharacters =
        {
            ("DM_Milu", "milu"),
            ("DM_Kashiwamochi", "かしわもち"),
            ("DM_Ryuta", "Ryuta"),
            ("DM_Got", "ごっと")
        };

        /// <summary>
        /// 左パネルへDiscord風のDMボタンを追加する
        /// </summary>
        private void Start()
        {
            var leftPanel = GameObject.Find("LeftPanel");
            if (leftPanel is null)
            {
                Debug.LogWarning("[NovelDmButtonView] LeftPanel was not found.");
                return;
            }

            _leftPanel = leftPanel.transform;
            CreateDmButton();
        }

        /// <summary>
        /// DMメニューボタンを生成する
        /// </summary>
        private void CreateDmButton()
        {
            var buttonObject = CreateButton("DmButton", "DM  メッセージ", 24f);
            buttonObject.transform.SetParent(_leftPanel, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.08f, 1f);
            rectTransform.anchorMax = new Vector2(0.92f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -24f);
            rectTransform.sizeDelta = new Vector2(0f, 64f);

            _button = buttonObject.GetComponent<Button>();
            _button.onClick.AddListener(OpenDmMenu);
        }

        /// <summary>
        /// 左パネルを個人別DM一覧へ切り替える
        /// </summary>
        private void OpenDmMenu()
        {
            if (_dmMenuObject is not null)
                return;

            _button.interactable = false;
            _button.gameObject.SetActive(false);
            DmClicked?.Invoke();

            _dmMenuObject = new GameObject("DmCharacterMenu", typeof(RectTransform));
            _dmMenuObject.transform.SetParent(_leftPanel, false);

            var menuRect = _dmMenuObject.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.08f, 1f);
            menuRect.anchorMax = new Vector2(0.92f, 1f);
            menuRect.pivot = new Vector2(0.5f, 1f);
            menuRect.anchoredPosition = new Vector2(0f, -24f);
            menuRect.sizeDelta = new Vector2(0f, 320f);

            var layout = _dmMenuObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            CreateBackButton();

            foreach (var character in DmCharacters)
                CreateCharacterButton(character.AssetKey, character.DisplayName);
        }

        /// <summary>
        /// 通常チャットへ戻るボタンを生成する
        /// </summary>
        private void CreateBackButton()
        {
            var buttonObject = CreateButton("DmBackButton", "← チャットに戻る", 20f);
            buttonObject.transform.SetParent(_dmMenuObject.transform, false);

            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 56f;
            layoutElement.preferredHeight = 56f;

            buttonObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                CloseDmMenu();
                DmBackClicked?.Invoke();
            });
        }

        /// <summary>
        /// DM一覧を閉じて通常チャット用のボタンを復元する
        /// </summary>
        private void CloseDmMenu()
        {
            if (_dmMenuObject is null)
                return;

            Destroy(_dmMenuObject);
            _dmMenuObject = null;
            _button.gameObject.SetActive(true);
            _button.interactable = true;
        }

        /// <summary>
        /// 個人別DMボタンを生成する
        /// </summary>
        private void CreateCharacterButton(string assetKey, string displayName)
        {
            var buttonObject = CreateButton($"DmButton_{assetKey}", displayName, 22f);
            buttonObject.transform.SetParent(_dmMenuObject.transform, false);

            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 64f;
            layoutElement.preferredHeight = 64f;

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                CharacterDmClicked?.Invoke(assetKey);
            });
        }

        /// <summary>
        /// DMボタンの共通UIを生成する
        /// </summary>
        private GameObject CreateButton(string objectName, string text, float fontSize)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 64f);

            var background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.18f, 0.20f, 0.24f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            // ボタンラベルを生成する
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = dmFont is not null ? dmFont : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;

            return buttonObject;
        }

        /// <summary>
        /// ボタンを一度押した後は重複表示を防ぐ
        /// </summary>
        public void Disable()
        {
            if (_button is not null)
                _button.interactable = false;
        }
    }
}
