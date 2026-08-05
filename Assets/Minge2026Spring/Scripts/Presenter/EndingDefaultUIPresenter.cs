using System;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class EndingDefaultUIPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private readonly EndingUseCase _endingUseCase;
        private readonly IGameSaveRepository _gameSaveRepository;
        private readonly EndingDefaultUIView _view;
        private SkipButtonHoldNotifier _holdNotifier;
        private bool _isTransitioning;

        public EndingDefaultUIPresenter(
            SceneTransitionUseCase sceneTransitionUseCase,
            EndingUseCase endingUseCase,
            IGameSaveRepository gameSaveRepository,
            EndingDefaultUIView view)
        {
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _endingUseCase = endingUseCase;
            _gameSaveRepository = gameSaveRepository;
            _view = view;
        }

        public void Initialize()
        {
            _holdNotifier = _view.SpeedUpButton.GetComponent<SkipButtonHoldNotifier>();
            if (_holdNotifier == null)
                _holdNotifier = _view.SpeedUpButton.gameObject.AddComponent<SkipButtonHoldNotifier>();

            _holdNotifier.Pressed += OnFastForwardPressed;
            _holdNotifier.Released += OnFastForwardReleased;
            if (_view.SkipButton != null)
                _view.SkipButton.onClick.AddListener(OnSkipClicked);
            _view.CreditFinished += OnCreditFinished;
            _view.SetSkipButtonVisible((_gameSaveRepository.Load()?.endingClearCount ?? 0) >= 2);
            _view.SetEndingTitle(_endingUseCase.GetEndingTitle());
            if (_endingUseCase.IsExtraStageEnding())
            {
                _view.ExtraDialogueFinished += OnExtraDialogueFinished;
                _view.StartExtraDialogue();
            }
            else
            {
                _view.StartScroll();
            }
        }

        public void Tick()
        {
            _view.UpdateScroll();
        }

        public void Dispose()
        {
            if (_holdNotifier != null)
            {
                _holdNotifier.Pressed -= OnFastForwardPressed;
                _holdNotifier.Released -= OnFastForwardReleased;
            }

            if (_view.SkipButton != null)
                _view.SkipButton.onClick.RemoveListener(OnSkipClicked);
            _view.CreditFinished -= OnCreditFinished;
            _view.ExtraDialogueFinished -= OnExtraDialogueFinished;
        }

        private void OnFastForwardPressed() => _view.SetFastForwardPressed(true);

        private void OnFastForwardReleased() => _view.SetFastForwardPressed(false);

        private void OnSkipClicked() => TransitionToTitle();

        private void OnCreditFinished() => TransitionToTitle();

        private void OnExtraDialogueFinished() => _view.StartScroll();

        private void TransitionToTitle()
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            _sceneTransitionUseCase.LoadTitleSceneAsync();
        }
    }
}
