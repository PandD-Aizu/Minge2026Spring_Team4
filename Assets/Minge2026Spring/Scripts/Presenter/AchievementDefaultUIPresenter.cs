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
        private readonly AchievementUseCase _achievementUseCase;
        private readonly AchievementDefaultUIView _view;
        private readonly CompositeDisposable _disposables = new();

        public AchievementDefaultUIPresenter(SceneTransitionUseCase sceneTransitionUseCase, AchievementUseCase achievementUseCase, AchievementDefaultUIView view)
        {
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _achievementUseCase = achievementUseCase;
            _view = view;
        }

        public void Initialize()
        {
            _view.CaptureUnlockedTexts();
            var reachedEndingIds = _achievementUseCase.GetReachedEndingIds();
            var hasReachedEnding = reachedEndingIds.Count > 0;
            for (var index = 0; index < AchievementUseCase.EndingCount; index++)
            {
                var unlocked = reachedEndingIds.Contains(AchievementUseCase.GetEndingId(index));
                var showDescription = hasReachedEnding && index != AchievementUseCase.EndingCount - 1;
                _view.SetEndingUnlocked(index, unlocked, showDescription);
            }

            var allEndingsReached = _achievementUseCase.AreAllEndingsReached(reachedEndingIds);
            _view.SetExtraStageUnlocked(allEndingsReached);

            _view.BackButton.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => _sceneTransitionUseCase.LoadTitleSceneAsync())
                .AddTo(_disposables);
            _view.ExtraStageButton.OnClickAsObservable()
                .Where(_ => allEndingsReached)
                .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => _achievementUseCase.StartExtraStage())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
