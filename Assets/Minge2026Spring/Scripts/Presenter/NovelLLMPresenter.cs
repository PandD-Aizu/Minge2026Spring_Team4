using System;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using UnityEngine;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelLLMPresenter : IInitializable, IDisposable
    {
        private readonly GenerateTextUseCase _generateTextUseCase;
        private readonly ChatWindowView chatWindowView;
        
        public NovelLLMPresenter(GenerateTextUseCase generateTextUseCase, ChatWindowView chatWindowView)
        {
            _generateTextUseCase = generateTextUseCase;
            this.chatWindowView = chatWindowView;
        }
        
        public async void Initialize()
        {
            string userInput = "全力でほめてください";
            string output = await _generateTextUseCase.GenerateTextAsync(userInput);

            Debug.Log($"Generated Text: {output}");
        }

        public void Dispose()
        {
            
        }
    }
}