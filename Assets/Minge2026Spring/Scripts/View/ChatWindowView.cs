using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
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
        [SerializeField] public StudioEventEmitter notificationEmitter;

        [Header("Prefabs")]
        [SerializeField] public AssetReference chatHeaderPrefab;
        [SerializeField] public AssetReference chatPrefab;
        [SerializeField] public AssetReference choicePrefab;

        private static readonly string[] ChatIconAddresses =
        {
            "player_icon",
            "goddo_icon",
            "ryuta_icon",
            "milu_icon",
            "kashiwamochi_icon"
        };

        private readonly Dictionary<string, Sprite> _chatIconCache = new();
        private readonly List<AsyncOperationHandle<Sprite>> _chatIconHandles = new();
        private UniTask _chatIconPreloadTask;
        private IFMODVoiceService _voiceService;

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
                var shouldSkip = shouldSkipDelays?.Invoke() == true;
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
        }

        private static async UniTask WaitForDialogueDelayAsync(
            float waitingTime,
            CancellationToken token,
            Func<bool> shouldSkipDelays)
        {
            if (shouldSkipDelays?.Invoke() == true)
                return;

            const float stepSeconds = 0.1f;
            var remaining = waitingTime;

            while (remaining > 0f)
            {
                if (shouldSkipDelays?.Invoke() == true)
                    return;

                var waitSeconds = Mathf.Min(stepSeconds, remaining);
                await UniTask.WaitForSeconds(waitSeconds, cancellationToken: token);
                remaining -= waitSeconds;
            }
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
