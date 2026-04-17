using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class ChatWindowView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] public Transform scrollViewContentTransform;
        [SerializeField] public ScrollRect scrollRect;
        [SerializeField] public Button skipButton;

        [Header("Prefabs")] 
        [SerializeField] public AssetReference chatPrefab;
        [SerializeField] public AssetReference choicePrefab;
        
        /// <summary>
        /// スクロールビューに新しい会話オブジェクトを追加する
        /// </summary>
        /// <param name="chapterBlock">章のブロック会話データ</param>
        /// <param name="token">キャンセルトークン</param>>
        /// <param name="onChoiceSelected">選択肢押下時コールバック</param>
        /// <param name="skipDelays">trueの場合は会話の待機時間をスキップする</param>
        public async UniTask AddNewChatObject(
            ChapterBlock chapterBlock,
            CancellationToken token,
            Action<int> onChoiceSelected = null,
            bool skipDelays = false)
        {
            await RenderDialoguesAsync(chapterBlock, token, skipDelays);
            await RenderChoicesAsync(chapterBlock, token, onChoiceSelected);
        }

        private async UniTask RenderDialoguesAsync(ChapterBlock chapterBlock, CancellationToken token, bool skipDelays)
        {
            if (chapterBlock.dialogues is null || chapterBlock.dialogues.Length == 0)
                return;

            // 待機時間を考慮しながら、UIを順に表示していく
            foreach (var dialogue in chapterBlock.dialogues)
            {
                var chatHandle = Addressables.InstantiateAsync(chatPrefab, scrollViewContentTransform);
                await chatHandle.ToUniTask(cancellationToken: token);
                var chatObject = chatHandle.Result;
                var chatUIView = chatObject.GetComponent<ChatUIView>();
                chatUIView.SetData(dialogue).Forget();

                ScrollToBottom();

                if (!skipDelays && dialogue.waitingTime > 0)
                    await UniTask.WaitForSeconds(dialogue.waitingTime, cancellationToken: token);
            }
        }

        private async UniTask RenderChoicesAsync(ChapterBlock chapterBlock, CancellationToken token, Action<int> onChoiceSelected)
        {
            // 選択肢があれば、最後に表示する
            if (chapterBlock.choices is null || chapterBlock.choices.Length == 0)
                return;

            var choiceHandle = Addressables.InstantiateAsync(choicePrefab, scrollViewContentTransform);
            await choiceHandle.ToUniTask(cancellationToken: token);
            var choiceObject = choiceHandle.Result;
            var choiceUIView = choiceObject.GetComponent<ChoiceUIView>();
            choiceUIView.SetData(chapterBlock.choices, onChoiceSelected);

            ScrollToBottom();
        }

        /// <summary>
        /// 一番下までスクロールする
        /// </summary>
        private void ScrollToBottom()
        {
            if (scrollRect is null)
                return;

            // キャンバスの描画を更新
            Canvas.ForceUpdateCanvases();

            // 一番下まで移動
            scrollRect
                .DOVerticalNormalizedPos(0f, 0.3f)
                .SetEase(Ease.OutQuart);
        }
    }
}