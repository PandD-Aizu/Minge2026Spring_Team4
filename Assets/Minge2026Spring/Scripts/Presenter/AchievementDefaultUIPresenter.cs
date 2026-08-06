using System;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class AchievementDefaultUIPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private readonly AchievementUseCase _achievementUseCase;
        private readonly AchievementDefaultUIView _view;
        private readonly CompositeDisposable _disposables = new();
        private bool _hasObservedExtraStageRunning;

        public AchievementDefaultUIPresenter(SceneTransitionUseCase sceneTransitionUseCase, AchievementUseCase achievementUseCase, AchievementDefaultUIView view)
        {
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _achievementUseCase = achievementUseCase;
            _view = view;
        }

        public void Initialize()
        {
            _view.CaptureUnlockedTexts();
            RefreshAchievements();
            var hiddenItemStatus = _achievementUseCase.GetHiddenItemStatus();
            var showHiddenItemDescriptions = _achievementUseCase.GetEndingClearCount() > 0;
            _view.SetHiddenItemStatus(
                hiddenItemStatus.GetItem1,
                hiddenItemStatus.GetItem2,
                showHiddenItemDescriptions);
            var extraStageUnlocked = _achievementUseCase.IsExtraStageUnlocked(
                _achievementUseCase.GetReachedEndingIds());
            _view.SetExtraStageUnlocked(extraStageUnlocked);

            _achievementUseCase.IsExtraStageRunning
                .Skip(1)
                .Subscribe(HandleExtraStageProcessState)
                .AddTo(_disposables);

            _view.BackButton.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => _sceneTransitionUseCase.LoadTitleSceneAsync())
                .AddTo(_disposables);
            _view.ExtraStageButton.OnClickAsObservable()
                .Where(_ => extraStageUnlocked)
                .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => _achievementUseCase.StartExtraStage())
                .AddTo(_disposables);
        }

        public void Tick() => _achievementUseCase.CheckExtraStageProcessIsRunning();

        public void Dispose() => _disposables.Dispose();

        private void RefreshAchievements()
        {
            var reachedEndingIds = _achievementUseCase.GetReachedEndingIds();
            var endingStatistics = _achievementUseCase.GetEndingPlayStatistics();
            _view.SetHaibokusyaFlags(_achievementUseCase.GetHaibokusyaFlags());
            var hasReachedEnding = reachedEndingIds.Count > 0;
            long totalClearTimeSeconds = 0;
            long totalDeathCount = 0;
            for (var index = 0; index < AchievementUseCase.EndingCount; index++)
            {
                var unlocked = reachedEndingIds.Contains(AchievementUseCase.GetEndingId(index));
                var showDescription = hasReachedEnding && index != AchievementUseCase.EndingCount - 1;
                _view.SetEndingUnlocked(index, unlocked, showDescription);
                _view.SetEndingStatistics(
                    index,
                    endingStatistics[index].ClearTimeSeconds,
                    endingStatistics[index].DeathCount);
                totalClearTimeSeconds += endingStatistics[index].ClearTimeSeconds;
                totalDeathCount += endingStatistics[index].DeathCount;
            }

            _view.SetTotalStatistics(totalClearTimeSeconds, totalDeathCount);
        }

        private void HandleExtraStageProcessState(bool isRunning)
        {
            if (isRunning)
            {
                _hasObservedExtraStageRunning = true;
                return;
            }

            if (!_hasObservedExtraStageRunning)
                return;

            _hasObservedExtraStageRunning = false;
            if (_achievementUseCase.TryRecordExtraStageClear())
            {
                RefreshAchievements();
                _sceneTransitionUseCase.LoadResultSceneAsync();
            }
        }
    }
}
