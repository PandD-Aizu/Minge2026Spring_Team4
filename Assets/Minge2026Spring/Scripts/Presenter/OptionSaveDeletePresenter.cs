using System;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.View;
using R3;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class OptionSaveDeletePresenter : IInitializable, IDisposable
    {
        private readonly OptionDefaultUIView _view;
        private readonly IGameSaveRepository _gameSaveRepository;
        private readonly CompositeDisposable _disposables = new();

        public OptionSaveDeletePresenter(OptionDefaultUIView view, IGameSaveRepository gameSaveRepository)
        {
            _view = view;
            _gameSaveRepository = gameSaveRepository;
        }

        public void Initialize()
        {
            _view.LanguageSelected += SaveLanguage;
            _view.saveDeleteButton.OnClickAsObservable()
                .Subscribe(_ => _view.ShowSaveDeleteConfirmation(_gameSaveRepository.Delete))
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _view.LanguageSelected -= SaveLanguage;
            _disposables.Dispose();
        }

        private void SaveLanguage(string languageType)
        {
            var saveData = _gameSaveRepository.Load() ?? new GameSaveData();
            saveData.languageType = languageType;
            _gameSaveRepository.Save(saveData);
        }
    }
}
