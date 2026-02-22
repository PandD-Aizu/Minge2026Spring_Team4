using System.Threading;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Minge2026Spring.Scripts.View
{
    public class ChatWindowView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] public Transform scrollViewContentTransform;

        [Header("Prefabs")] 
        [SerializeField] public AssetReference chatPrefab;
        [SerializeField] public AssetReference choicePrefab;
        
        /// <summary>
        /// スクロールビューに新しい会話オブジェクトを追加する
        /// </summary>
        /// <param name="chapterBlock">章のブロック会話データ</param>
        /// <param name="token">キャンセルトークン</param>>
        public async UniTask AddNewChatObject(ChapterBlock chapterBlock, CancellationToken token)
        {
            // 待機時間を考慮しながら、UIを順に表示していく
            foreach (var dialogue in chapterBlock.dialogues)
            {
                var chatHandle = Addressables.InstantiateAsync(chatPrefab, scrollViewContentTransform);
                await chatHandle.ToUniTask(cancellationToken: token);
                var chatObject = chatHandle.Result;
                var chatUIView = chatObject.GetComponent<ChatUIView>();
                chatUIView.SetData(dialogue);

                if (dialogue.waitingTime > 0)
                    await UniTask.WaitForSeconds(dialogue.waitingTime, cancellationToken: token);
            }

            // 選択肢があれば、最後に表示する
            if (chapterBlock.choices is not null && chapterBlock.choices.Length > 0)
            {
                var choiceHandle = Addressables.InstantiateAsync(choicePrefab, scrollViewContentTransform);
                await choiceHandle.ToUniTask(cancellationToken: token);
                var choiceObject = choiceHandle.Result;
                var choiceUIView = choiceObject.GetComponent<ChoiceUIView>();
                choiceUIView.SetData(chapterBlock.choices);
            }
        }
    }
}