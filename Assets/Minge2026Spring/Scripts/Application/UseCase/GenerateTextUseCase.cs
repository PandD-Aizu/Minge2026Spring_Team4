using System.Threading;
using Cysharp.Threading.Tasks;
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

        public string GenerateText(string userInput)
        {
            return _llmProvider.SendRequest(userInput);
        }
    }
}