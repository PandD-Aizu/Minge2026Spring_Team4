using Cysharp.Threading.Tasks;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface ILLMProvider
    {
        public UniTask<string> SendRequestAsync(string userInput);
    }
}
