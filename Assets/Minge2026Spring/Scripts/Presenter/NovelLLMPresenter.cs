using System;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelLLMPresenter : IInitializable, IDisposable
    {
        private readonly GenerateTextUseCase _generateTextUseCase;
        private readonly DialogueWindowView _dialogueWindowView;
        
        public NovelLLMPresenter(GenerateTextUseCase generateTextUseCase, DialogueWindowView dialogueWindowView)
        {
            _generateTextUseCase = generateTextUseCase;
            _dialogueWindowView = dialogueWindowView;
        }
        
        public async void Initialize()
        {
            string userInput = "全力でほめてください";
            string output = await _generateTextUseCase.GenerateTextAsync(userInput);

            _dialogueWindowView.SetText(output);
        }

        public void Dispose()
        {
            
        }
    }
}