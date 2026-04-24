using System;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class AchievementDefaultUIPresenter : IInitializable, IDisposable
    {
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private readonly AchievementDefaultUIView _view;

        private CompositeDisposable _disposables = new();
        
        public AchievementDefaultUIPresenter(SceneTransitionUseCase sceneTransitionUseCase, AchievementDefaultUIView view)
        {
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _view = view;
        }

        public void Initialize()
        {
            _view.BackButton.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => _sceneTransitionUseCase.LoadTitleSceneAsync())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}