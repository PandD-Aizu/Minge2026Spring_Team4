using System;
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
                .Where(isRunning => !isRunning)
                .Subscribe(_ => _sceneTransitionUseCase.LoadResultSceneAsync())
                .AddTo(_disposables);
        }
        
        public void Tick()
        {
            Debug.Log($"Process is running: {_processUseCase.IsProcessRunning.CurrentValue}");
            _processUseCase.CheckProcessIsRunning();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}