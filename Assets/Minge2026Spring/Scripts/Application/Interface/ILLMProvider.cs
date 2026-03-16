using System;
using Cysharp.Threading.Tasks;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface ILLMProvider
    {
        /// <summary>
        /// LLMによるテキスト生成を非同期で行います。
        /// </summary>
        /// <param name="prompt">プロンプト</param>
        /// <param name="maxNewTokens">最大生成トークン数</param>
        /// <param name="onToken">トークンが生成されるたびに呼ばれるコールバック</param>
        /// <param name="withHistory">会話履歴を保持するかどうか</param>
        /// <returns>生成が正常に完了したかどうか</returns>
        UniTask<bool> GenerateAsync(string prompt, int maxNewTokens, Action<string> onToken, bool withHistory = false);
        
        /// <summary>
        /// 現在のテキスト生成をキャンセルする
        /// </summary>
        /// <param name="timeoutMs">タイムアウトする時間</param>
        /// <returns></returns>
        UniTask CancelAndWaitAsync(int timeoutMs = 1500);
        
        /// <summary>
        /// 会話履歴をクリアする
        /// </summary>
        /// <returns>クリアできたかどうか</returns>
        bool ClearHistory();
    }
}
