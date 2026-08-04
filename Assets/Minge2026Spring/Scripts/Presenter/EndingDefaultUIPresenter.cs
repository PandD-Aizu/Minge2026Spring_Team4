using System;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class EndingDefaultUIPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private readonly EndingUseCase _endingUseCase;
        private readonly EndingDefaultUIView _view;
        private SkipButtonHoldNotifier _holdNotifier;
        private bool _isTransitioning;

        public EndingDefaultUIPresenter(SceneTransitionUseCase sceneTransitionUseCase, EndingUseCase endingUseCase, EndingDefaultUIView view)
        {
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _endingUseCase = endingUseCase;
            _view = view;
        }

        public void Initialize()
        {
            _holdNotifier = _view.backButton.GetComponent<SkipButtonHoldNotifier>();
            if (_holdNotifier == null)
                _holdNotifier = _view.backButton.gameObject.AddComponent<SkipButtonHoldNotifier>();

            _holdNotifier.Pressed += OnFastForwardPressed;
            _holdNotifier.Released += OnFastForwardReleased;
            _view.CreditFinished += OnCreditFinished;
            _view.SetEndingTitle(_endingUseCase.GetEndingTitle());
            _view.StartScroll();
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

            _view.CreditFinished -= OnCreditFinished;
        }

        private void OnFastForwardPressed() => _view.SetFastForwardPressed(true);

        private void OnFastForwardReleased() => _view.SetFastForwardPressed(false);

        private void OnCreditFinished()
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            _sceneTransitionUseCase.LoadTitleSceneAsync();
        }
    }
}
