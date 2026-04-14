using System;
using System.IO;
using System.Text;
using Codice.CM.Common.Merge;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Infrastructure.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;
using UnityEngine.Networking;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class LLMProvider : ILLMProvider, IDisposable
    {
        public LLMProvider()
        {
            // ログを流すために初期化
            LLMForUnityManager.SetupLogging();
            
            // モデルのパスを構築し初期化
            ConstructPaths(out string modelPath);
            
            // StreamingAssets内のモデルを読み込む
            bool isInitialized = LLMForUnityManager.Init(modelPath);
            if (!isInitialized)
            {
                Debug.LogError($"[LLMProvider] Failed to initialize LLMForUnityManager.\nmodelPath = {modelPath}");
            }
        }

        /// <inheritdoc/>
        public UniTask<bool> GenerateAsync(string prompt, int maxNewTokens, Action<string> onToken,
            bool withHisotry = false)
        {
            return LLMForUnityManager.GenerateAsync(prompt, maxNewTokens, onToken, withHisotry).AsUniTask();
        }

        /// <inheritdoc/>
        public UniTask CancelAndWaitAsync(int timeoutMs = 1500)
        {
            return LLMForUnityManager.CancelAndWaitAsync(timeoutMs).AsUniTask();
        }

        /// <inheritdoc/>
        public bool ClearHistory()
        {
            return LLMForUnityManager.ClearHistory();
        }

        private void ConstructPaths(out string modelPath)
        {
            string basePath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Qwen3.5_2B");
            modelPath = Path.Combine(basePath, "Qwen3.5-2B-Q5_K_M.gguf");
        }

        public void Dispose()
        {
            CancelAndWaitAsync(0).Forget();
        }
    }
}