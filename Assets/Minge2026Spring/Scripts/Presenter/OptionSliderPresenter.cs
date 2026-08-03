using System;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class OptionSliderPresenter : IInitializable, IDisposable
    {
        private readonly IFMODVCAService _fmodVcaService;
        private readonly SceneTransitionUseCase _sceneTransitionUseCase;
        private readonly OptionDefaultUIView _view;

        private CompositeDisposable _disposables = new ();

        public OptionSliderPresenter(OptionDefaultUIView view, IFMODVCAService fmodVcaService, SceneTransitionUseCase sceneTransitionUseCase)
        {
            _fmodVcaService = fmodVcaService;
            _sceneTransitionUseCase = sceneTransitionUseCase;
            _view = view;
        }
        
        public void Initialize()
        {
            _view.mainVolumeSlider.value = _fmodVcaService.GetMasterVolume();
            _view.bgmVolumeSlider.value = _fmodVcaService.GetBGMVolume();
            _view.seVolumeSlider.value = _fmodVcaService.GetSEVolume();
            if (_view.voiceVolumeSlider is not null)
                _view.voiceVolumeSlider.value = _fmodVcaService.GetVoiceVolume();
            
            _view.mainVolumeSlider.OnValueChangedAsObservable()
                .Subscribe(value => _fmodVcaService.SetMasterVolume(value))
                .AddTo(_disposables);
            
            _view.bgmVolumeSlider.OnValueChangedAsObservable()
                .Subscribe(value => _fmodVcaService.SetBGMVolume(value))
                .AddTo(_disposables);
            
            _view.seVolumeSlider.OnValueChangedAsObservable()
                .Subscribe(value => _fmodVcaService.SetSEVolume(value))
                .AddTo(_disposables);

            if (_view.voiceVolumeSlider is not null)
            {
                _view.voiceVolumeSlider.OnValueChangedAsObservable()
                    .Subscribe(value => _fmodVcaService.SetVoiceVolume(value))
                    .AddTo(_disposables);
            }
            
            _view.backButton.OnClickAsObservable()
                .Subscribe(_ => _sceneTransitionUseCase.LoadTitleSceneAsync())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
