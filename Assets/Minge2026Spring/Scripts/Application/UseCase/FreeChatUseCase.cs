using System;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class FreeChatUseCase
    {
        private readonly ILLMProvider _llmProvider;

        public FreeChatUseCase(ILLMProvider llmProvider)
        {
            _llmProvider = llmProvider;
        }

        /// <summary>
        /// LLMにプロンプトを送信して、ストリーミングで返答を受け取る
        /// </summary>
        /// <param name="systemPrompt">システムのプロンプト</param>
        /// <param name="userInput">ユーザーの入力</param>
        /// <param name="onToken">トークンが生成された時のコールバック</param>
        public async UniTask GenerateResponseAsync(string systemPrompt, string userInput, Action<string> onToken)
        {
            string prompt = $"{systemPrompt}\nUser: {userInput}\nAssistant:";

            await _llmProvider.GenerateAsync(prompt, maxNewTokens: 200, onToken: onToken, withHistory: true);
        }

        /// <summary>
        /// 対話履歴をリセットする
        /// </summary>
        public void ClearHistory()
        {
            _llmProvider.ClearHistory();
        }

        /// <summary>
        /// 生成をキャンセルする
        /// </summary>
        public async UniTask CancelAsync()
        {
            await _llmProvider.CancelAndWaitAsync();
        }
    }
}