using System;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using UnityEngine.UI;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class TitleDefaultUIPresenter : IInitializable, IDisposable
    {
        private readonly SceneTransitionUseCase _useCase;
        private readonly ApplicationStopUseCase _appStopUseCase; // TODO: 仮実装（ポップアップウィンドウでも出しておく）
        private readonly TitleDefaultUIView _view;

        private readonly CompositeDisposable _disposables = new();

        public TitleDefaultUIPresenter(SceneTransitionUseCase useCase, ApplicationStopUseCase appStopUseCase, TitleDefaultUIView view)
        {
            _useCase = useCase;
            _appStopUseCase = appStopUseCase;
            _view = view;
        }

        public void Initialize()
        { 
            BindTransition(_view.StartButton,       _useCase.LoadNovelSceneAsync);
            BindTransition(_view.OptionButton,      _useCase.LoadOptionSceneAsync);
            BindTransition(_view.AchievementButton, _useCase.LoadAchievementSceneAsync);
            BindTransition(_view.ExitButton,        _appStopUseCase.StopApplication);
        }

        private void BindTransition(Button button, Action transitionFunc)
        {
            button.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(1.0f))
                .Subscribe(_ =>
                {
                    HandleTransitionAsync(transitionFunc);
                })
                .AddTo(_disposables);
        }
        
        private void HandleTransitionAsync(Action action)
        {
            _view.SetInteractable(false);
            try 
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Transition failed: {e}");
                _view.SetInteractable(true);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}