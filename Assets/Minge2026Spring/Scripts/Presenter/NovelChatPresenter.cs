using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
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
        private readonly ITmpMoraleJsonExporter _tmpMoraleJsonExporter;
        private readonly ProcessUseCase _processUseCase;
        private readonly ChatWindowView _chatWindowView;
        private SkipButtonHoldNotifier _skipButtonHoldNotifier;
        
        private CompositeDisposable _disposables = new();
        private SkipMode _skipMode = SkipMode.None; // TODO: リファクタする

        public NovelChatPresenter(
            ChatUseCase chatUseCase,
            FreeChatUseCase freeChatUseCase,
            MoraleUseCase moraleUseCase,
            ITmpMoraleJsonExporter tmpMoraleJsonExporter,
            ProcessUseCase processUseCase,
            ChatWindowView chatWindowView)
        {
            _chatUseCase = chatUseCase;
            _freeChatUseCase = freeChatUseCase;
            _moraleUseCase = moraleUseCase;
            _tmpMoraleJsonExporter = tmpMoraleJsonExporter;
            _processUseCase = processUseCase;
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
                        SaveMoraleToTmpJson();
                        
                        var path = Path.Combine(
                            "I_gonna_be_the_tresure_hunter",
                            "I_wanna_Siv3D.exe"
                        );
                        _processUseCase.StartProcess(path);
                    }
                })
                .AddTo(_disposables);
            
            // 章の会話データをロードする
            _chatUseCase.LoadChapter("Chapter1").Forget();
        }

        public void Dispose()
        {
            if (_skipButtonHoldNotifier is not null)
            {
                _skipButtonHoldNotifier.Pressed -= StartSkipToInput;
                _skipButtonHoldNotifier.Released -= StopSkipToInput;
            }

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

            _skipButtonHoldNotifier = _chatWindowView.skipButton.GetComponent<SkipButtonHoldNotifier>();
            if (_skipButtonHoldNotifier is null)
                _skipButtonHoldNotifier = _chatWindowView.skipButton.gameObject.AddComponent<SkipButtonHoldNotifier>();

            _skipButtonHoldNotifier.Pressed += StartSkipToInput;
            _skipButtonHoldNotifier.Released += StopSkipToInput;
        }

        /// <summary>
        /// 選択肢やLLM入力など、次の入力待ちイベント到達までスキップする。
        /// </summary>
        public void StartSkipToInput()
        {
            if (_skipMode == SkipMode.ToNextInputRequired)
                return;

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
                shouldSkipDelays: IsSkipToInputActive);

            if (IsSkipToInputActive() && IsUserInputRequiredBlock(block))
            {
                StopSkipToInput();
                return;
            }

            if (!ShouldAutoAdvance(block))
                return;

            if (block.waitingTime > 0)
                await WaitWithSkipAsync(block.waitingTime, token);

            _chatUseCase.MoveToNextBlock();
        }

        private bool IsSkipToInputActive()
        {
            return _skipMode == SkipMode.ToNextInputRequired;
        }

        private async UniTask WaitWithSkipAsync(float waitingTime, CancellationToken token)
        {
            if (IsSkipToInputActive())
                return;

            const float stepSeconds = 0.1f;
            var remaining = waitingTime;
            while (remaining > 0f)
            {
                if (IsSkipToInputActive())
                    return;

                var waitSeconds = Mathf.Min(stepSeconds, remaining);
                await UniTask.WaitForSeconds(waitSeconds, cancellationToken: token);
                remaining -= waitSeconds;
            }
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
            foreach (var moraleDelta in choice.GetMoraleDeltas())
            {
                if (moraleDelta.Delta == 0)
                    continue;

                _moraleUseCase.AddMorale(moraleDelta.CharacterId, moraleDelta.Delta);
            }

            Debug.Log($"[NovelChatPresenter] Applied morale changes from choice index {choiceIndex}");
        }

        // TODO: TMP
        private void SaveMoraleToTmpJson()
        {
            var moraleDto = _moraleUseCase.GetMoraleDto();
            _tmpMoraleJsonExporter.Export(moraleDto);
            Debug.Log("[NovelChatPresenter] Exported morale JSON at chapter end.");
        }
    }
}