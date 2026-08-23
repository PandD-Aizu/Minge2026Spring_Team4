using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Application.UseCase;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace Minge2026Spring.Scripts.View
{
    /// <summary>
    /// 既存のMenuButtonにアタッチしてメニュー画面を生成するView
    /// </summary>
    public sealed class NovelMenuButtonView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("メニュー画面で使用するフォント。未設定時は既定フォントを使用")]
        private TMP_FontAsset menuFont;

        private static readonly Color32 OverlayColor = new(5, 5, 6, 190);
        private static readonly Color32 WindowColor = new(18, 18, 20, 255);
        private static readonly Color32 HeaderColor = new(18, 18, 20, 255);
        private static readonly Color32 RowColor = new(54, 54, 54, 255);
        private static readonly Color32 TrackColor = new(35, 35, 35, 255);
        private static readonly Color32 AccentColor = new(102, 102, 102, 255);
        private static readonly Color32 AccentHoverColor = new(76, 76, 76, 255);
        private static readonly Color32 PrimaryTextColor = new(245, 245, 245, 255);
        private static readonly Color32 SecondaryTextColor = new(205, 205, 205, 255);
        private static readonly Color32 MutedTextColor = new(150, 150, 150, 255);
        private static readonly Color32 SuccessColor = new(59, 165, 93, 255);

        private SceneTransitionUseCase _sceneTransitionUseCase;
        private ChatUseCase _chatUseCase;
        private MoraleUseCase _moraleUseCase;
        private IFMODVCAService _fmodVcaService;
        private Button _menuButton;
        private Transform _menuRoot;
        private GameObject _menuWindow;
        private GameObject _optionWindow;
        private Sprite _roundedSprite;
        private Texture2D _roundedTexture;

        /// <summary>
        /// シーン遷移と音量設定の依存を注入する
        /// </summary>
        /// <param name="sceneTransitionUseCase">シーン遷移ユースケース</param>
        /// <param name="chatUseCase">会話進行ユースケース</param>
        /// <param name="moraleUseCase">やる気度ユースケース</param>
        /// <param name="fmodVcaService">音量設定サービス</param>
        [Inject]
        private void Construct(SceneTransitionUseCase sceneTransitionUseCase, ChatUseCase chatUseCase,
            MoraleUseCase moraleUseCase, IFMODVCAService fmodVcaService)
        {
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _chatUseCase = chatUseCase;
            _moraleUseCase = moraleUseCase;
            _fmodVcaService = fmodVcaService;
        }

        /// <summary>
        /// 既存MenuButtonのクリックイベントを登録する
        /// </summary>
        private void Start()
        {
            _menuButton = GetComponent<Button>();
            if (_menuButton is null)
            {
                Debug.LogWarning("[NovelMenuButtonView] Button component was not found");
                return;
            }

            var hudDynamic = GameObject.Find("HUD_Dynamic");
            if (hudDynamic is null)
            {
                Debug.LogError("[NovelMenuButtonView] HUD_Dynamic was not found");
                return;
            }

            _menuRoot = hudDynamic.transform;

            _menuButton.onClick.AddListener(OpenMenu);
        }

        /// <summary>
        /// 簡易メニューを中央に生成する
        /// </summary>
        private void OpenMenu()
        {
            if (_menuWindow is not null)
                return;

            _menuButton.interactable = false;
            _menuWindow = CreatePanel("NovelMenuWindow", _menuRoot, WindowColor,
                Vector2.zero, new Vector2(460f, 430f));

            // メニュー項目を縦方向へ整理
            var layout = _menuWindow.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 36, 36);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel("menu.title", 30f, 46f);
            CreateMenuItem("menu.return_to_title", ReturnToTitle);
            CreateMenuItem("menu.options", OpenOption);
            CreateMenuItem("menu.back", CloseMenu);
        }

        /// <summary>
        /// タイトルシーンへ遷移する
        /// </summary>
        private void ReturnToTitle()
        {
            _sceneTransitionUseCase.LoadTitleSceneAsync();
        }

        /// <summary>
        /// ゲーム画面上へ音量設定モーダルを表示する
        /// </summary>
        private void OpenOption()
        {
            if (_optionWindow is not null || _fmodVcaService is null)
                return;

            _menuWindow.SetActive(false);

            // 背景を暗くしてゲーム画面との視覚的な階層を作る
            _optionWindow = CreateStretchObject("NovelOptionOverlay", _menuRoot);
            var overlayImage = _optionWindow.AddComponent<Image>();
            overlayImage.color = OverlayColor;
            var overlayButton = _optionWindow.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.targetGraphic = overlayImage;
            overlayButton.onClick.AddListener(CloseOption);

            // Discord風のチャコールカラーで設定ウィンドウを構成する
            var window = CreatePanel("NovelOptionWindow", _optionWindow.transform, WindowColor,
                Vector2.zero, new Vector2(780f, 620f));
            var windowBlocker = window.AddComponent<Button>();
            windowBlocker.transition = Selectable.Transition.None;
            windowBlocker.targetGraphic = window.GetComponent<Image>();
            windowBlocker.onClick.AddListener(DoNothing);

            CreateOptionHeader(window.transform);
            var sectionLabel = CreateText("SectionLabel", window.transform, string.Empty, 13f, FontStyles.Bold,
                MutedTextColor, new Vector2(-216f, 170f), new Vector2(220f, 24f), TextAlignmentOptions.Left);
            UILocalization.Bind(sectionLabel, "NovelUI", "audio.section");

            // 4種類の音量を同じ情報設計で並べる
            CreateVolumeRow(window.transform, "Master", "audio.master.title", "audio.master.description",
                110f, _fmodVcaService.GetMasterVolume(), _fmodVcaService.SetMasterVolume);
            CreateVolumeRow(window.transform, "BGM", "audio.bgm.title", "audio.bgm.description",
                15f, _fmodVcaService.GetBGMVolume(), _fmodVcaService.SetBGMVolume);
            CreateVolumeRow(window.transform, "SE", "audio.se.title", "audio.se.description",
                -80f, _fmodVcaService.GetSEVolume(), _fmodVcaService.SetSEVolume);
            CreateVolumeRow(window.transform, "Voice", "audio.voice.title", "audio.voice.description",
                -175f, _fmodVcaService.GetVoiceVolume(), _fmodVcaService.SetVoiceVolume);

            CreateOptionFooter(window.transform);
        }

        /// <summary>
        /// オプション画面のヘッダーを生成する
        /// </summary>
        /// <param name="parent">ウィンドウのTransform</param>
        private void CreateOptionHeader(Transform parent)
        {
            var header = CreatePanel("Header", parent, HeaderColor,
                new Vector2(0f, 254f), new Vector2(780f, 112f));
            var title = CreateText("Title", header.transform, string.Empty, 28f, FontStyles.Bold,
                PrimaryTextColor, new Vector2(-114f, 18f), new Vector2(480f, 42f), TextAlignmentOptions.Left);
            UILocalization.Bind(title, "NovelUI", "audio.title");
            var description = CreateText("Description", header.transform, string.Empty, 14f, FontStyles.Normal,
                SecondaryTextColor, new Vector2(-94f, -23f), new Vector2(520f, 28f), TextAlignmentOptions.Left);
            UILocalization.Bind(description, "NovelUI", "audio.description");

            // 右上からもすぐ閉じられる導線を用意
            var closeButton = CreateButton("CloseButton", "×", 27f, RowColor, AccentHoverColor);
            closeButton.transform.SetParent(header.transform, false);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(340f, 0f), new Vector2(48f, 48f));
            closeButton.GetComponent<Button>().onClick.AddListener(CloseOption);
        }

        /// <summary>
        /// 音量設定の1行を生成する
        /// </summary>
        private void CreateVolumeRow(Transform parent, string objectName, string titleKey, string descriptionKey,
            float y, float initialValue, UnityAction<float> onValueChanged)
        {
            var row = CreatePanel($"VolumeRow_{objectName}", parent, RowColor,
                new Vector2(0f, y), new Vector2(700f, 86f));
            var title = CreateText("Title", row.transform, string.Empty, 18f, FontStyles.Bold,
                PrimaryTextColor, new Vector2(-216f, 14f), new Vector2(220f, 28f), TextAlignmentOptions.Left);
            UILocalization.Bind(title, "NovelUI", titleKey);
            var description = CreateText("Description", row.transform, string.Empty, 13f, FontStyles.Normal,
                MutedTextColor, new Vector2(-206f, -18f), new Vector2(240f, 24f), TextAlignmentOptions.Left);
            UILocalization.Bind(description, "NovelUI", descriptionKey);

            var slider = CreateSlider(row.transform, initialValue);
            var valueBadge = CreatePanel("ValueBadge", row.transform, TrackColor,
                new Vector2(298f, 0f), new Vector2(72f, 36f));
            var valueText = CreateText("Value", valueBadge.transform,
                $"{Mathf.RoundToInt(initialValue * 100f)}%", 14f, FontStyles.Bold,
                PrimaryTextColor, Vector2.zero, new Vector2(72f, 36f), TextAlignmentOptions.Center);

            // 表示値とFMOD設定を同じイベントで更新
            slider.onValueChanged.AddListener(value =>
            {
                valueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
                onValueChanged.Invoke(value);
            });
        }

        /// <summary>
        /// 自動保存表示と戻るボタンを生成する
        /// </summary>
        /// <param name="parent">ウィンドウのTransform</param>
        private void CreateOptionFooter(Transform parent)
        {
            var divider = CreatePanel("FooterDivider", parent, new Color32(54, 55, 61, 255),
                new Vector2(0f, -222f), new Vector2(700f, 1f));
            divider.GetComponent<Image>().raycastTarget = false;

            var statusDot = CreatePanel("SaveStatus", parent, SuccessColor,
                new Vector2(-326f, -266f), new Vector2(10f, 10f));
            statusDot.GetComponent<Image>().raycastTarget = false;
            var saveMessage = CreateText("SaveMessage", parent, string.Empty, 13f, FontStyles.Normal,
                MutedTextColor, new Vector2(-156f, -266f), new Vector2(300f, 28f), TextAlignmentOptions.Left);
            UILocalization.Bind(saveMessage, "NovelUI", "audio.auto_save");

            var backButton = CreateButton("BackButton", string.Empty, 15f,
                new Color32(49, 51, 56, 255), new Color32(66, 68, 75, 255));
            backButton.transform.SetParent(parent, false);
            UILocalization.Bind(backButton.GetComponentInChildren<TMP_Text>(), "NovelUI", "audio.back_to_menu");
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(274f, -266f), new Vector2(176f, 46f));
            backButton.GetComponent<Button>().onClick.AddListener(CloseOption);

            var restartButton = CreateButton("RestartButton", string.Empty, 15f,
                new Color32(91, 61, 61, 255), new Color32(116, 74, 74, 255));
            restartButton.transform.SetParent(parent, false);
            UILocalization.Bind(restartButton.GetComponentInChildren<TMP_Text>(), "NovelUI", "audio.restart");
            SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(70f, -266f), new Vector2(176f, 46f));
            restartButton.GetComponent<Button>().onClick.AddListener(RestartNovel);
        }

        /// <summary>
        /// 現在の周回とやる気度を初期化してNovelSceneを先頭から読み直す
        /// </summary>
        private void RestartNovel()
        {
            _chatUseCase.ResetProgressForRestart();
            _moraleUseCase.ResetForRestart();
            _sceneTransitionUseCase.LoadNovelSceneAsync();
        }

        /// <summary>
        /// オプション画面を閉じて簡易メニューへ戻る
        /// </summary>
        private void CloseOption()
        {
            if (_optionWindow is not null)
                Destroy(_optionWindow);

            _optionWindow = null;
            if (_menuWindow is not null)
                _menuWindow.SetActive(true);
        }

        /// <summary>
        /// 簡易メニューを閉じてゲーム画面へ戻る
        /// </summary>
        private void CloseMenu()
        {
            if (_menuWindow is not null)
                Destroy(_menuWindow);

            _menuWindow = null;
            if (_menuButton is not null)
                _menuButton.interactable = true;
        }

        /// <summary>
        /// メニュー項目を生成する
        /// </summary>
        private void CreateMenuItem(string entryKey, UnityAction action)
        {
            var buttonObject = CreateButton($"MenuItem_{entryKey}", string.Empty, 22f,
                RowColor, AccentHoverColor);
            buttonObject.transform.SetParent(_menuWindow.transform, false);
            UILocalization.Bind(buttonObject.GetComponentInChildren<TMP_Text>(), "NovelUI", entryKey);

            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 58f;
            layoutElement.preferredHeight = 58f;
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
        }

        /// <summary>
        /// 簡易メニューのタイトルを生成する
        /// </summary>
        private void CreateLabel(string entryKey, float fontSize, float height)
        {
            var labelObject = new GameObject("MenuTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(_menuWindow.transform, false);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            ConfigureText(label, string.Empty, fontSize, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.Center);
            UILocalization.Bind(label, "NovelUI", entryKey);

            var layoutElement = labelObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
        }

        /// <summary>
        /// Discord風の細い音量スライダーを生成する
        /// </summary>
        private Slider CreateSlider(Transform parent, float value)
        {
            var sliderObject = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(82f, 0f), new Vector2(360f, 34f));

            var background = CreatePanel("Track", sliderObject.transform, TrackColor,
                Vector2.zero, new Vector2(360f, 10f));
            background.GetComponent<Image>().raycastTarget = false;

            var fillArea = CreateStretchObject("Fill Area", sliderObject.transform);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(5f, 12f);
            fillAreaRect.offsetMax = new Vector2(-5f, -12f);
            var fill = CreateStretchObject("Fill", fillArea.transform);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = AccentColor;
            fillImage.sprite = GetRoundedSprite();
            fillImage.type = Image.Type.Sliced;
            fillImage.raycastTarget = false;

            var handleArea = CreateStretchObject("Handle Slide Area", sliderObject.transform);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);
            var handle = CreatePanel("Handle", handleArea.transform, PrimaryTextColor,
                Vector2.zero, new Vector2(12f, 20f));

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.colors = CreateColorBlock(PrimaryTextColor, Color.white, AccentHoverColor);
            slider.value = Mathf.Clamp01(value);
            return slider;
        }

        /// <summary>
        /// 指定色の角丸パネルを生成する
        /// </summary>
        private GameObject CreatePanel(string objectName, Transform parent, Color color, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            SetRect(panel.GetComponent<RectTransform>(), position, size);

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            return panel;
        }

        /// <summary>
        /// 親全体へ広がるRectTransformを生成する
        /// </summary>
        private static GameObject CreateStretchObject(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
        }

        /// <summary>
        /// 指定位置へTextMeshProラベルを生成する
        /// </summary>
        private TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize,
            FontStyles fontStyle, Color color, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            SetRect(labelObject.GetComponent<RectTransform>(), position, size);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            ConfigureText(label, text, fontSize, fontStyle, color, alignment);
            return label;
        }

        /// <summary>
        /// TextMeshProの共通表示設定を適用する
        /// </summary>
        private void ConfigureText(TMP_Text label, string text, float fontSize, FontStyles fontStyle,
            Color color, TextAlignmentOptions alignment)
        {
            label.text = text;
            label.font = menuFont is not null ? menuFont : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = alignment;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
        }

        /// <summary>
        /// メニュー用ボタンを生成する
        /// </summary>
        private GameObject CreateButton(string objectName, string text, float fontSize,
            Color normalColor, Color highlightedColor)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            var background = buttonObject.GetComponent<Image>();
            background.color = normalColor;
            background.sprite = GetRoundedSprite();
            background.type = Image.Type.Sliced;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = CreateColorBlock(Color.white, highlightedColor, AccentColor);

            var labelObject = CreateStretchObject("Label", buttonObject.transform);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(label, text, fontSize, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.Center);
            return buttonObject;
        }

        /// <summary>
        /// ボタンとスライダーの状態色を生成する
        /// </summary>
        private static ColorBlock CreateColorBlock(Color normal, Color highlighted, Color pressed)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = highlighted,
                pressedColor = pressed,
                selectedColor = highlighted,
                disabledColor = MutedTextColor,
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        /// <summary>
        /// 中央基準でRectTransformの位置とサイズを設定する
        /// </summary>
        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// ランタイムUI用の角丸スプライトを取得する
        /// </summary>
        private Sprite GetRoundedSprite()
        {
            if (_roundedSprite is not null)
                return _roundedSprite;

            const int textureSize = 32;
            const float radius = 8f;
            _roundedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "NovelMenuRoundedTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            // 四隅のみ円弧で切り抜いた白テクスチャを生成
            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var cornerX = x < radius ? radius : x >= textureSize - radius ? textureSize - radius - 1f : x;
                    var cornerY = y < radius ? radius : y >= textureSize - radius ? textureSize - radius - 1f : y;
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                    _roundedTexture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
                }
            }

            _roundedTexture.Apply();
            _roundedSprite = Sprite.Create(_roundedTexture, new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8f, 8f, 8f, 8f));
            _roundedSprite.name = "NovelMenuRoundedSprite";
            return _roundedSprite;
        }

        /// <summary>
        /// ウィンドウ内のクリックを受け止める
        /// </summary>
        private static void DoNothing()
        {
        }

        /// <summary>
        /// クリックイベントとランタイム生成物を解放する
        /// </summary>
        private void OnDestroy()
        {
            if (_menuButton is not null)
                _menuButton.onClick.RemoveListener(OpenMenu);

            if (_roundedSprite is not null)
                Destroy(_roundedSprite);
            if (_roundedTexture is not null)
                Destroy(_roundedTexture);
        }
    }
}
