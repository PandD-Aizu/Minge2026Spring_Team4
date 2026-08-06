using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelAchievementPresenter : IInitializable, System.IDisposable
    {
        private readonly AchievementUseCase _achievementUseCase;
        private readonly MoraleUseCase _moraleUseCase;
        private readonly MoraleEndingSelector _moraleEndingSelector;
        private readonly ProcessUseCase _processUseCase;
        private readonly EndingUseCase _endingUseCase;
        private readonly NovelAchievementUIView _view;
        private readonly CompositeDisposable _disposables = new();
        private bool _hasObservedExternalGame;

        public NovelAchievementPresenter(
            AchievementUseCase achievementUseCase,
            MoraleUseCase moraleUseCase,
            MoraleEndingSelector moraleEndingSelector,
            ProcessUseCase processUseCase,
            EndingUseCase endingUseCase,
            NovelAchievementUIView view)
        {
            _achievementUseCase = achievementUseCase;
            _moraleUseCase = moraleUseCase;
            _moraleEndingSelector = moraleEndingSelector;
            _processUseCase = processUseCase;
            _endingUseCase = endingUseCase;
            _view = view;
        }

        public void Initialize()
        {
            _view.SetAchievements(
                _achievementUseCase.GetReachedEndingIds(),
                _achievementUseCase.GetEndingClearCount() > 0);

            _moraleUseCase.MoraleChanged += UpdateSelectedEnding;
            _processUseCase.IsProcessRunning
                .Skip(1)
                .Subscribe(HandleExternalGameStateChanged)
                .AddTo(_disposables);
            UpdateSelectedEnding();
        }

        public void Dispose()
        {
            _moraleUseCase.MoraleChanged -= UpdateSelectedEnding;
            _disposables.Dispose();
        }

        private void UpdateSelectedEnding()
        {
            if (!_moraleUseCase.TryGetMoraleDto(out var moraleDto))
                return;

            _view.SetSelectedEnding(_moraleEndingSelector.GetEndingIndex(moraleDto));
        }

        private void HandleExternalGameStateChanged(bool isRunning)
        {
            if (isRunning)
            {
                _hasObservedExternalGame = true;
                return;
            }

            if (!_hasObservedExternalGame)
                return;

            _hasObservedExternalGame = false;
            _view.SetSelectedEnding(_endingUseCase.GetEndingIndex());
        }
    }
}
