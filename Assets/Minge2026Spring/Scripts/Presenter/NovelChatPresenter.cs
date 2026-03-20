using System;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelChatPresenter : IInitializable, IDisposable
    {
        private readonly ChatUseCase _chatUseCase;
        private readonly FreeChatUseCase _freeChatUseCase;
        private readonly MoraleUseCase _moraleUseCase;
        private readonly ChatWindowView _chatWindowView;
        
        private CompositeDisposable _disposables = new();

        public NovelChatPresenter(
            ChatUseCase chatUseCase,
            FreeChatUseCase freeChatUseCase,
            MoraleUseCase moraleUseCase,
            ChatWindowView chatWindowView)
        {
            _chatUseCase = chatUseCase;
            _freeChatUseCase = freeChatUseCase;
            _moraleUseCase = moraleUseCase;
            _chatWindowView = chatWindowView;
        }
        
        public void Initialize()
        {
            _moraleUseCase.OnGameStart();

            // 章の会話ブロックの変化を監視して、チャットウィンドウに反映させる
            _chatUseCase.CurrentChapterBlock
                .Where(block => block is not null)
                .SubscribeAwait(async (block, token) =>
                {
                    // チャットウィンドウに新しいチャットオブジェクトを追加
                    await _chatWindowView.AddNewChatObject(block, token, choiceIndex =>
                    {
                        ApplyChoiceMorale(block, choiceIndex);
                        _chatUseCase.MoveToNextBlock(choiceIndex);
                    });

                    // // FreeChatブロックがあれば、FreeChatUseCaseを使う
                    // if (block.freeChats != null && block.freeChats.Length > 0)
                    // {
                    //     await ProcessFreeChatBlockAsync(block.freeChats[0], token);
                    // }
                    
                    // 遷移待ちと次ブロックへの移動
                    if (block.choices is null || block.choices.Length == 0)
                    {
                        if (block.waitingTime > 0)
                            await UniTask.WaitForSeconds(block.waitingTime, cancellationToken: token);

                        // 次のチャットブロックへ移動する
                        _chatUseCase.MoveToNextBlock();
                    }
                })
                .AddTo(_disposables);
            
            // 章の会話データをロードする
            _chatUseCase.LoadChapter("Chapter1").Forget();
        }

        // private async UniTask ProcessFreeChatBlockAsync(FreeChat freeChatData, CancellationToken token)
        // {
        //     
        // }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private void ApplyChoiceMorale(ChapterBlock block, int choiceIndex)
        {
            if (block.choices is null || choiceIndex < 0 || choiceIndex >= block.choices.Length)
                return;

            var choice = block.choices[choiceIndex];
            _moraleUseCase.AddMorale("CharacterA", choice.characterAMoraleDelta);
            _moraleUseCase.AddMorale("CharacterB", choice.characterBMoraleDelta);
            _moraleUseCase.AddMorale("CharacterC", choice.characterCMoraleDelta);
            _moraleUseCase.AddMorale("CharacterD", choice.characterDMoraleDelta);

            Debug.Log($"[NovelChatPresenter] Applied morale changes from choice index {choiceIndex}");
        }
    }
}