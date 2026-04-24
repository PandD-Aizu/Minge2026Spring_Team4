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
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private bool _hasObservedRunning;

        private CompositeDisposable _disposables = new ();

        public NovelProcessPresenter(ProcessUseCase processUseCase, SceneTransitionUseCase sceneTransitionUseCase)
        {
            _processUseCase = processUseCase;
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

                    // 起動済みプロセスが停止した時だけリザルトへ遷移する。
                    if (_hasObservedRunning)
                    {
                        _hasObservedRunning = false;
                        _sceneTransitionUseCase.LoadResultSceneAsync();
                    }
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
    }
}