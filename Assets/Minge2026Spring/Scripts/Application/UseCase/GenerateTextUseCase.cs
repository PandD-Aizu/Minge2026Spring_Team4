using System.Threading;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class GenerateTextUseCase
    {
        private ILLMProvider _llmProvider;
        
        public GenerateTextUseCase(ILLMProvider llmProvider)
        {
            _llmProvider = llmProvider;
        }

        public async UniTask<string> GenerateTextAsync(string userInput)
        {
            return await _llmProvider.SendRequestAsync(userInput);
        }
    }
}