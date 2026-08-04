using System;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Application.UseCase;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelProcessPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly ProcessUseCase _processUseCase;
        private readonly ChatUseCase _chatUseCase;
        private readonly FreeChatUseCase _freeChatUseCase;
        private readonly EndingUseCase _endingUseCase;
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private bool _hasObservedRunning;
        private bool _isEndingDialogueActive;

        private CompositeDisposable _disposables = new ();

        public NovelProcessPresenter(
            ProcessUseCase processUseCase,
            ChatUseCase chatUseCase,
            FreeChatUseCase freeChatUseCase,
            EndingUseCase endingUseCase,
            SceneTransitionUseCase sceneTransitionUseCase)
        {
            _processUseCase = processUseCase;
            _chatUseCase = chatUseCase;
            _freeChatUseCase = freeChatUseCase;
            _endingUseCase = endingUseCase;
            _sceneTransitionUseCase = sceneTransitionUseCase;
        }

        public void Initialize()
        {
            _processUseCase.IsProcessRunning
                .Skip(1)
                .Subscribe(isRunning =>
                {
                    if (isRunning)
                    {
                        _hasObservedRunning = true;
                        return;
                    }

                    // 起動済みプロセスが停止した時だけエンディング会話を開始する
                    if (_hasObservedRunning)
                    {
                        _hasObservedRunning = false;
                        StartEndingDialogue();
                    }
                })
                .AddTo(_disposables);

            // エンディング会話をすべて表示した後にエンディングシーンへ遷移する
            _chatUseCase.IsChapterEnded
                .Skip(1)
                .Where(isEnded => isEnded && _isEndingDialogueActive)
                .Subscribe(_ =>
                {
                    _isEndingDialogueActive = false;
                    ResetChatProgress();
                    _sceneTransitionUseCase.LoadResultSceneAsync();
                })
                .AddTo(_disposables);
        }
        
        public void Tick()
        {
            _processUseCase.CheckProcessIsRunning();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        /// <summary>
        /// 出力値に対応するエンディング会話を開始する
        /// </summary>
        private void StartEndingDialogue()
        {
            var endingBlockId = _endingUseCase.GetEndingBlockId();
            _chatUseCase.RecordReachedEnding(endingBlockId);
            _isEndingDialogueActive = _chatUseCase.MoveToBlock(endingBlockId);

            // Ending_A自体が見つからない場合は進行不能を避けてシーン遷移する
            if (!_isEndingDialogueActive)
            {
                ResetChatProgress();
                _sceneTransitionUseCase.LoadResultSceneAsync();
            }
        }

        /// <summary>
        /// 次の周回に会話を持ち越さないよう、シナリオとLLMの履歴を破棄する。
        /// </summary>
        private void ResetChatProgress()
        {
            _chatUseCase.ResetProgressAfterClear();
            _freeChatUseCase.ClearHistory();
        }
    }
}
