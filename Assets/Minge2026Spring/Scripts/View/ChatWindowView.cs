using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using VContainer;

namespace Minge2026Spring.Scripts.View
{
    public class ChatWindowView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] public Transform scrollViewContentTransform;
        [SerializeField] public ScrollRect scrollRect;
        [SerializeField] public Button skipButton;
        [SerializeField] public Button nextDialogueButton;
        [SerializeField] public StudioEventEmitter notificationEmitter;

        [Header("Prefabs")]
        [SerializeField] public AssetReference chatHeaderPrefab;
        [SerializeField] public AssetReference chatPrefab;
        [SerializeField] public AssetReference choicePrefab;

        [Header("Chapter Separator")]
        [SerializeField] private TMP_FontAsset separatorFont;
        [SerializeField] private float separatorFontSize = 20f;
        [SerializeField] private FontStyles separatorFontStyle = FontStyles.Bold;
        [SerializeField] private Color separatorTextColor = new(0.72f, 0.74f, 0.78f, 1f);
        [SerializeField] private Color separatorLineColor = new(0.28f, 0.30f, 0.34f, 1f);
        [SerializeField] private float separatorWidth = 1150f;
        [SerializeField] private float separatorHeight = 48f;
        [SerializeField] private float separatorLabelWidth = 180f;
        [SerializeField] private float separatorHorizontalPadding = 24f;
        [SerializeField] private float separatorLineGap = 10f;
        [SerializeField] private float separatorLineThickness = 1f;

        private static readonly string[] ChatIconAddresses =
        {
            "player_icon",
            "goddo_icon",
            "ryuta_icon",
            "milu_icon",
            "kashiwamochi_icon",
            "daittyan_icon"
        };

        private readonly Dictionary<string, Sprite> _chatIconCache = new();
        private readonly List<AsyncOperationHandle<Sprite>> _chatIconHandles = new();
        private UniTask _chatIconPreloadTask;
        private IFMODVoiceService _voiceService;
        private int _chapterSeparatorCount;
        private int _choiceSeparatorCount;
        private bool _isRenderingDialogue;
        private bool _isNextDialogueRequested;

        /// <summary>
        /// シーン開始時にチャットアイコンの先読みを開始する
        /// </summary>
        private void Awake()
        {
            // プリロード結果を保持して複数回の await に対応させる
            _chatIconPreloadTask = PreloadChatIconsAsync().Preserve();
        }

        [Inject]
        public void Construct(IFMODVoiceService voiceService)
        {
            _voiceService = voiceService;
        }

        /// <summary>
        /// スクロールビューに新しい会話オブジェクトを追加する
        /// </summary>
        /// <param name="chapterBlock">章のブロック会話データ</param>
        /// <param name="token">キャンセルトークン</param>
        /// <param name="onChoiceSelected">選択肢押下時コールバック</param>
        /// <param name="shouldSkipDelays">trueを返す間は会話の待機時間をスキップする</param>
        public async UniTask AddNewChatObject(
            ChapterBlock chapterBlock,
            CancellationToken token,
            Action<int> onChoiceSelected = null,
            Func<bool> shouldSkipDelays = null)
        {
            await _chatIconPreloadTask.AttachExternalCancellation(token);
            AddChapterSeparator(chapterBlock);
            await RenderDialoguesAsync(chapterBlock, token, shouldSkipDelays);
            await RenderChoicesAsync(chapterBlock, token, onChoiceSelected);
        }

        /// <summary>
        /// 復元対象の過去ブロックを待機と選択肢なしで表示する
        /// </summary>
        /// <param name="chapterBlock">復元する章のブロック会話データ</param>
        /// <param name="token">キャンセルトークン</param>
        public async UniTask AddHistoricalChatObject(ChapterBlock chapterBlock, CancellationToken token)
        {
            await _chatIconPreloadTask.AttachExternalCancellation(token);
            AddChapterSeparator(chapterBlock);
            await RenderDialoguesAsync(chapterBlock, token, () => true);
        }

        /// <summary>
        /// チュートリアルヘッダーをチャット履歴の先頭に表示する
        /// </summary>
        public async UniTask AddChatHeader(CancellationToken token)
        {
            var headerHandle = Addressables.InstantiateAsync(chatHeaderPrefab, scrollViewContentTransform);
            await headerHandle.ToUniTask(cancellationToken: token);
            headerHandle.Result.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// 会話メッセージを順番に生成して表示する
        /// </summary>
        private async UniTask RenderDialoguesAsync(
            ChapterBlock chapterBlock,
            CancellationToken token,
            Func<bool> shouldSkipDelays)
        {
            if (chapterBlock.dialogues is null || chapterBlock.dialogues.Length == 0)
                return;

            foreach (var dialogue in chapterBlock.dialogues)
            {
                _isRenderingDialogue = true;
                _isNextDialogueRequested = false;
                try
                {
                    var shouldSkip = ShouldSkipCurrentDialogue(shouldSkipDelays);
                    if (!shouldSkip)
                        _voiceService?.Play(dialogue.voiceEventPath);

                    if (!shouldSkip)
                        notificationEmitter.Play();
                    var chatHandle = Addressables.InstantiateAsync(chatPrefab, scrollViewContentTransform);
                    await chatHandle.ToUniTask(cancellationToken: token);
                    var chatObject = chatHandle.Result;
                    var chatUIView = chatObject.GetComponent<ChatUIView>();
                    _chatIconCache.TryGetValue(dialogue.iconId, out var iconAsset);
                    chatUIView.SetData(dialogue, iconAsset);

                    ScrollToBottom();

                    if (!shouldSkip && _voiceService is not null)
                        await _voiceService.WaitUntilFinished(token);

                    if (dialogue.waitingTime > 0)
                        await WaitForDialogueDelayAsync(dialogue.waitingTime, token, shouldSkipDelays);
                }
                finally
                {
                    _isRenderingDialogue = false;
                }
            }
        }

        /// <summary>
        /// 現在表示中の会話のボイスと待機時間を終了し、次の会話へ進める
        /// </summary>
        public void RequestNextDialogue()
        {
            if (!_isRenderingDialogue)
                return;

            _isNextDialogueRequested = true;
            StopVoice();
        }

        /// <summary>
        /// チャットで使用するアイコンをAddressablesからまとめて読み込む
        /// </summary>
        private async UniTask PreloadChatIconsAsync()
        {
            foreach (var address in ChatIconAddresses)
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(address);
                _chatIconHandles.Add(handle);

                var iconAsset = await handle.Task;
                if (iconAsset is not null)
                    _chatIconCache[address] = iconAsset;
                else
                    Debug.LogError($"[ChatWindowView] Failed to preload chat icon: {address}");
            }
        }

        public void StopVoice()
        {
            _voiceService?.StopCurrentVoice(false);
        }

        /// <summary>
        /// 右側チャット欄に表示中のメッセージをすべて削除する
        /// </summary>
        public void ClearChatObjects()
        {
            if (scrollViewContentTransform is null)
                return;

            for (var index = scrollViewContentTransform.childCount - 1; index >= 0; index--)
                Destroy(scrollViewContentTransform.GetChild(index).gameObject);

            _chapterSeparatorCount = 0;
            _choiceSeparatorCount = 0;
        }

        private void AddChapterSeparator(ChapterBlock chapterBlock)
        {
            if (scrollViewContentTransform is null || chapterBlock is null)
                return;

            var isChoice = chapterBlock.nodeType == ChapterNodeType.Choice ||
                           chapterBlock.choices is { Length: > 0 };
            var hasContent = isChoice || chapterBlock.dialogues is { Length: > 0 };
            if (!hasContent)
                return;

            var number = isChoice ? ++_choiceSeparatorCount : ++_chapterSeparatorCount;
            var labelEntry = isChoice ? "separator.choice" : "separator.chapter";
            var resolvedFont = ResolveSeparatorFont();

            var separator = new GameObject(
                $"ChapterSeparator_{(isChoice ? "Choice" : "Chapter")}_{number}",
                typeof(RectTransform),
                typeof(LayoutElement));
            separator.layer = scrollViewContentTransform.gameObject.layer;
            separator.transform.SetParent(scrollViewContentTransform, false);
            var separatorRect = separator.GetComponent<RectTransform>();
            separatorRect.sizeDelta = new Vector2(separatorWidth, separatorHeight);

            var separatorLayout = separator.GetComponent<LayoutElement>();
            separatorLayout.preferredWidth = separatorWidth;
            separatorLayout.preferredHeight = separatorHeight;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = separator.layer;
            labelObject.transform.SetParent(separator.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(separatorLabelWidth, 0f);

            var labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.font = resolvedFont;
            labelText.fontSize = separatorFontSize;
            labelText.fontStyle = separatorFontStyle;
            labelText.color = separatorTextColor;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;
            UILocalization.Bind(labelText, "NovelUI", labelEntry, number);

            var labelHalfWidthWithGap = separatorLabelWidth * 0.5f + separatorLineGap;
            AddSeparatorLine(
                separator.transform,
                "LeftLine",
                0f,
                0.5f,
                separatorHorizontalPadding,
                -labelHalfWidthWithGap);
            AddSeparatorLine(
                separator.transform,
                "RightLine",
                0.5f,
                1f,
                labelHalfWidthWithGap,
                -separatorHorizontalPadding);
        }

        private TMP_FontAsset ResolveSeparatorFont()
        {
            if (separatorFont is not null)
                return separatorFont;

            foreach (var existingText in scrollViewContentTransform.GetComponentsInChildren<TMP_Text>(true))
            {
                if (existingText.font is not null)
                    return existingText.font;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private void AddSeparatorLine(
            Transform parent,
            string objectName,
            float anchorMinX,
            float anchorMaxX,
            float leftOffset,
            float rightOffset)
        {
            var lineObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            lineObject.layer = parent.gameObject.layer;
            lineObject.transform.SetParent(parent, false);

            var lineRect = lineObject.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(anchorMinX, 0.5f);
            lineRect.anchorMax = new Vector2(anchorMaxX, 0.5f);
            var halfThickness = separatorLineThickness * 0.5f;
            lineRect.offsetMin = new Vector2(leftOffset, -halfThickness);
            lineRect.offsetMax = new Vector2(rightOffset, halfThickness);

            var lineImage = lineObject.GetComponent<Image>();
            lineImage.color = separatorLineColor;
            lineImage.raycastTarget = false;
        }

        private async UniTask WaitForDialogueDelayAsync(
            float waitingTime,
            CancellationToken token,
            Func<bool> shouldSkipDelays)
        {
            if (ShouldSkipCurrentDialogue(shouldSkipDelays))
                return;

            const float stepSeconds = 0.1f;
            var remaining = waitingTime;

            while (remaining > 0f)
            {
                if (ShouldSkipCurrentDialogue(shouldSkipDelays))
                    return;

                var waitSeconds = Mathf.Min(stepSeconds, remaining);
                await UniTask.WaitForSeconds(waitSeconds, cancellationToken: token);
                remaining -= waitSeconds;
            }
        }

        private bool ShouldSkipCurrentDialogue(Func<bool> shouldSkipDelays)
        {
            return _isNextDialogueRequested || shouldSkipDelays?.Invoke() == true;
        }

        private async UniTask RenderChoicesAsync(
            ChapterBlock chapterBlock,
            CancellationToken token,
            Action<int> onChoiceSelected)
        {
            if (chapterBlock.choices is null || chapterBlock.choices.Length == 0)
                return;

            var choiceHandle = Addressables.InstantiateAsync(choicePrefab, scrollViewContentTransform);
            await choiceHandle.ToUniTask(cancellationToken: token);
            var choiceObject = choiceHandle.Result;
            var choiceUIView = choiceObject.GetComponent<ChoiceUIView>();
            await choiceUIView.SetData(chapterBlock.choices, onChoiceSelected, token);

            ScrollToBottom();
        }

        /// <summary>
        /// 一番下までスクロールする
        /// </summary>
        private void ScrollToBottom()
        {
            if (scrollRect is null)
                return;

            Canvas.ForceUpdateCanvases();
            scrollRect
                .DOVerticalNormalizedPos(0f, 0.3f)
                .SetEase(Ease.OutQuart);
        }

        /// <summary>
        /// シーン終了時に先読みしたAddressablesの参照を解放する
        /// </summary>
        private void OnDestroy()
        {
            foreach (var handle in _chatIconHandles)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _chatIconHandles.Clear();
            _chatIconCache.Clear();
        }
    }
}
