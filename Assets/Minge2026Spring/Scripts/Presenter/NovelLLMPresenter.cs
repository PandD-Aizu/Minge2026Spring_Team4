using System;
using Minge2026Spring.Scripts.Application.UseCase;
using UnityEngine;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelLLMPresenter : IInitializable, IDisposable
    {
        private readonly GenerateTextUseCase _generateTextUseCase;
        
        public NovelLLMPresenter(GenerateTextUseCase generateTextUseCase)
        {
            _generateTextUseCase = generateTextUseCase;
        }
        
        public void Initialize()
        {
            string userInput = "全力でほめてください";
            string output = "";
            output = _generateTextUseCase.GenerateText(userInput);
            
            Debug.Log($"Generated Text: {output}");
        }

        public void Dispose()
        {
            
        }
    }
}