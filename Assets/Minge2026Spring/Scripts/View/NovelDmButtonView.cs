using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    /// <summary>
    /// ノベルシーンのDMボタンとDMメニューを制御するView
    /// </summary>
    public sealed class NovelDmButtonView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("DM機能UIで使用するTextMeshProフォント。未設定時は既定フォントを使用")]
        private TMP_FontAsset dmFont;

        [SerializeField]
        [Tooltip("DM画面を開くボタン")]
        private Button dmButton;

        public event Action DmClicked;
        public event Action<string> CharacterDmClicked;
        public event Action DmBackClicked;

        private Transform _leftPanel;
        private GameObject _dmMenuObject;
        private GameObject _channelPanel;
        private GameObject _serverPanel;
        private GameObject _separatorLeft;
        private Button _sceneButtonStyleSource;

        private static readonly (string AssetKey, string IconAddress)[] DmCharacters =
        {
            ("DM_Daittyan", "Daittyan_DM_Icon"),
            ("DM_Milu", "DM_Milu_Icon"),
            ("DM_Kashiwamochi", "DM_Kashiwamochi_Icon"),
            ("DM_Ryuta", "DM_Ryuta_Icon"),
            ("DM_Got", "DM_Got_Icon")
        };

        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly List<AsyncOperationHandle<Sprite>> _spriteHandles = new();
        private UniTask _spritePreloadTask;

        /// <summary>
        /// DM UI素材の先読みを開始する
        /// </summary>
        private void Awake()
        {
            // 複数箇所から待機できるよう先読みタスクを保持する
            _spritePreloadTask = PreloadSpritesAsync().Preserve();
        }

        /// <summary>
        /// DMボタンとDMメニューを初期化する
        /// </summary>
        private void Start()
        {
            InitializeAsync().Forget();
        }

        /// <summary>
        /// 左パネルを取得してDMボタンを初期化する
        /// </summary>
        private async UniTaskVoid InitializeAsync()
        {
            var leftPanel = GameObject.Find("LeftPanel");
            if (leftPanel is null)
            {
                Debug.LogWarning("[NovelDmButtonView] LeftPanel was not found.");
                return;
            }

            _leftPanel = leftPanel.transform;
            _channelPanel = GameObject.Find("ChannelPanel");
            _serverPanel = GameObject.Find("ServerPanel");
            _separatorLeft = GameObject.Find("SeparatorLeft");
            _sceneButtonStyleSource = GameObject.Find("SkipButton")?.GetComponent<Button>();

            if (dmButton is null)
            {
                Debug.LogError("[NovelDmButtonView] Dm Button is not assigned.", this);
                return;
            }

            dmButton.onClick.AddListener(OpenDmMenu);

            // DM一覧の素材を先読みする
            await _spritePreloadTask.AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// DM UI素材をAddressablesから読み込む
        /// </summary>
        private async UniTask PreloadSpritesAsync()
        {
            var addresses = new List<string>();
            foreach (var character in DmCharacters)
                addresses.Add(character.IconAddress);

            foreach (var address in addresses)
            {
                // 解放に必要なハンドルと読み込み結果を保持する
                var handle = Addressables.LoadAssetAsync<Sprite>(address);
                _spriteHandles.Add(handle);
                var sprite = await handle.Task;

                if (sprite is not null)
                    _spriteCache[address] = sprite;
                else
                    Debug.LogError($"[NovelDmButtonView] Failed to preload sprite: {address}");
            }
        }

        /// <summary>
        /// 左パネルを個人別DM一覧へ切り替える
        /// </summary>
        private void OpenDmMenu()
        {
            if (_dmMenuObject is not null)
                return;

            dmButton.interactable = false;
            dmButton.gameObject.SetActive(false);
            SetChatNavigationActive(false);
            DmClicked?.Invoke();

            _dmMenuObject = new GameObject("DmCharacterMenu", typeof(RectTransform));
            _dmMenuObject.transform.SetParent(_leftPanel, false);

            var menuRect = _dmMenuObject.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0f, 1f);
            menuRect.anchorMax = new Vector2(1f, 1f);
            menuRect.pivot = new Vector2(0.5f, 1f);
            menuRect.anchoredPosition = Vector2.zero;
            menuRect.sizeDelta = new Vector2(0f, 320f);

            var layout = _dmMenuObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            CreateBackButton();

            foreach (var character in DmCharacters)
                CreateCharacterButton(character.AssetKey, character.IconAddress);
        }

        /// <summary>
        /// 通常チャットへ戻るボタンを生成する
        /// </summary>
        private void CreateBackButton()
        {
            var buttonObject = CreateButton("DmBackButton", "← チャットに戻る", 20f);
            buttonObject.transform.SetParent(_dmMenuObject.transform, false);

            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60f;
            layoutElement.preferredHeight = 60f;

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
            SetChatNavigationActive(true);
            dmButton.gameObject.SetActive(true);
            dmButton.interactable = true;
        }

        /// <summary>
        /// 通常チャット用の左側ナビゲーション表示を切り替える
        /// </summary>
        private void SetChatNavigationActive(bool isActive)
        {
            if (_channelPanel is not null)
                _channelPanel.SetActive(isActive);

            if (_serverPanel is not null)
                _serverPanel.SetActive(isActive);

            if (_separatorLeft is not null)
                _separatorLeft.SetActive(isActive);
        }

        /// <summary>
        /// 個人別DMボタンを生成する
        /// </summary>
        private void CreateCharacterButton(string assetKey, string iconAddress)
        {
            var buttonObject = CreateSpriteButton(
                $"DmButton_{assetKey}",
                null,
                _spriteCache[iconAddress]);
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
        /// 指定した素材を重ねたDMボタンを生成する
        /// </summary>
        private static GameObject CreateSpriteButton(string objectName, Sprite backgroundSprite, Sprite contentSprite)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 64f);

            // 背景素材がない一覧項目は透明な当たり判定として使用する
            var background = buttonObject.GetComponent<Image>();
            background.sprite = backgroundSprite;
            background.color = backgroundSprite is null ? Color.clear : Color.white;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            // 表示素材をボタン全面へ配置する
            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(Image));
            contentObject.transform.SetParent(buttonObject.transform, false);
            var content = contentObject.GetComponent<Image>();
            content.sprite = contentSprite;
            content.preserveAspect = true;
            content.raycastTarget = false;
            content.rectTransform.anchorMin = Vector2.zero;
            content.rectTransform.anchorMax = Vector2.one;
            content.rectTransform.sizeDelta = Vector2.zero;

            return buttonObject;
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

            if (_sceneButtonStyleSource is not null)
            {
                var sourceBackground = _sceneButtonStyleSource.targetGraphic as Image;
                if (sourceBackground is not null)
                {
                    background.sprite = sourceBackground.sprite;
                    background.type = sourceBackground.type;
                    background.color = sourceBackground.color;
                }

                button.transition = _sceneButtonStyleSource.transition;
                button.colors = _sceneButtonStyleSource.colors;
            }

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
            if (dmButton is not null)
                dmButton.interactable = false;
        }

        /// <summary>
        /// 読み込んだAddressablesの参照を解放する
        /// </summary>
        private void OnDestroy()
        {
            if (dmButton is not null)
                dmButton.onClick.RemoveListener(OpenDmMenu);

            foreach (var handle in _spriteHandles)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _spriteHandles.Clear();
            _spriteCache.Clear();
        }
    }
}
