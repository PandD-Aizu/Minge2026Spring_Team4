using System;
using System.IO;
using System.Threading;
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
        // TODO: リファクタする
        private enum SkipMode
        {
            None,
            ToNextInputRequired
        }

        private readonly ChatUseCase _chatUseCase;
        private readonly FreeChatUseCase _freeChatUseCase;
        private readonly MoraleUseCase _moraleUseCase;
        private readonly GameStarterUseCase _gameStarterUseCase;
        private readonly ChatWindowView _chatWindowView;
        
        private CompositeDisposable _disposables = new();
        private SkipMode _skipMode = SkipMode.None; // TODO: リファクタする

        public NovelChatPresenter(
            ChatUseCase chatUseCase,
            FreeChatUseCase freeChatUseCase,
            MoraleUseCase moraleUseCase,
            GameStarterUseCase gameStarterUseCase,
            ChatWindowView chatWindowView)
        {
            _chatUseCase = chatUseCase;
            _freeChatUseCase = freeChatUseCase;
            _moraleUseCase = moraleUseCase;
            _gameStarterUseCase = gameStarterUseCase;
            _chatWindowView = chatWindowView;
        }
        
        public void Initialize()
        {
            _moraleUseCase.OnGameStart();

            BindSkipButton();

            // 章の会話ブロックの変化を監視して、チャットウィンドウに反映させる
            _chatUseCase.CurrentChapterBlock
                .Where(block => block is not null)
                .SubscribeAwait(async (block, token) => await HandleChapterBlockAsync(block, token))
                .AddTo(_disposables);
            
            // 章の終了を監視して、アイワナを起動する
            _chatUseCase.IsChapterEnded
                .Skip(1)
                .Where(isEnded => isEnded)
                .Subscribe(isEnded =>
                {
                    if (isEnded)
                    {
                        var path = Path.Combine(UnityEngine.Application.streamingAssetsPath, "I_gonna_be_the_tresure_hunter/I_wanna_Siv3D.exe");
                        _gameStarterUseCase.StartGame(path);
                    }
                })
                .AddTo(_disposables);
            
            // 章の会話データをロードする
            _chatUseCase.LoadChapter("Chapter1").Forget();
        }

        public void Dispose()
        {
            StopSkipToInput();
            _disposables.Dispose();
        }

        private void BindSkipButton()
        {
            if (_chatWindowView.skipButton is null)
            {
                Debug.LogWarning("[NovelChatPresenter] skipButton is not assigned.");
                return;
            }

            _chatWindowView.skipButton
                .OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ => StartSkipToInput())
                .AddTo(_disposables);
        }

        /// <summary>
        /// 選択肢やLLM入力など、次の入力待ちイベント到達までスキップする。
        /// </summary>
        public void StartSkipToInput()
        {
            _skipMode = SkipMode.ToNextInputRequired;
            Debug.Log("[NovelChatPresenter] Skip-to-input mode enabled.");
        }

        /// <summary>
        /// スキップを停止する。
        /// </summary>
        public void StopSkipToInput()
        {
            _skipMode = SkipMode.None;
        }

        private async UniTask HandleChapterBlockAsync(ChapterBlock block, CancellationToken token)
        {
            await _chatWindowView.AddNewChatObject(
                block,
                token,
                choiceIndex =>
                {
                    // 手動選択が発生した時点でスキップは不要になるため解除する
                    StopSkipToInput();
                    ApplyChoiceMorale(block, choiceIndex);
                    _chatUseCase.MoveToNextBlock(choiceIndex);
                },
                skipDelays: IsSkipToInputActive());

            if (IsSkipToInputActive() && IsUserInputRequiredBlock(block))
            {
                StopSkipToInput();
                return;
            }

            if (!ShouldAutoAdvance(block))
                return;

            if (!IsSkipToInputActive() && block.waitingTime > 0)
                await UniTask.WaitForSeconds(block.waitingTime, cancellationToken: token);

            _chatUseCase.MoveToNextBlock();
        }

        private bool IsSkipToInputActive()
        {
            return _skipMode == SkipMode.ToNextInputRequired;
        }

        private static bool ShouldAutoAdvance(ChapterBlock block)
        {
            return !IsUserInputRequiredBlock(block);
        }

        private static bool IsUserInputRequiredBlock(ChapterBlock block)
        {
            var hasChoices = block.choices is { Length: > 0 };
            var hasFreeChats = block.freeChats is { Length: > 0 };
            var isLlmNode = block.nodeType == ChapterNodeType.LLM;

            return hasChoices || hasFreeChats || isLlmNode;
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