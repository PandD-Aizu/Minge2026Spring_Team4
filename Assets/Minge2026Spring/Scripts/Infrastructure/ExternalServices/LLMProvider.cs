using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class LLMProvider : ILLMProvider, IDisposable
    {
        private readonly bool _isInitialized;

        public LLMProvider()
        {
            try
            {
#if !ENABLE_IL2CPP
                // IL2CPP builds can fail on native callback marshaling here.
                LLMForUnityManager.SetupLogging();
#endif
            
                // モデルのパスを構築し初期化
                ConstructPaths(out string modelPath);
            
                // StreamingAssets内のモデルを読み込む
                _isInitialized = LLMForUnityManager.Init(modelPath);
                if (!_isInitialized)
                {
                    Debug.LogError($"[LLMProvider] Failed to initialize LLMForUnityManager. modelPath = {modelPath}");
                }
            }
            catch (Exception e)
            {
                _isInitialized = false;
                Debug.LogError($"[LLMProvider] Initialization failed. Free chat features are disabled.\n{e}");
            }
        }

        /// <inheritdoc/>
        public UniTask<bool> GenerateAsync(string prompt, int maxNewTokens, Action<string> onToken,
            bool withHisotry = false)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[LLMProvider] GenerateAsync called before successful initialization.");
                return UniTask.FromResult(false);
            }

            return LLMForUnityManager.GenerateAsync(prompt, maxNewTokens, onToken, withHisotry).AsUniTask();
        }

        /// <inheritdoc/>
        public UniTask CancelAndWaitAsync(int timeoutMs = 1500)
        {
            if (!_isInitialized)
                return UniTask.CompletedTask;

            return LLMForUnityManager.CancelAndWaitAsync(timeoutMs).AsUniTask();
        }

        /// <inheritdoc/>
        public bool ClearHistory()
        {
            if (!_isInitialized)
                return false;

            return LLMForUnityManager.ClearHistory();
        }

        private void ConstructPaths(out string modelPath)
        {
            string basePath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Qwen3.5_2B");
            modelPath = Path.Combine(basePath, "Qwen3.5-2B-Q5_K_M.gguf");
        }

        public void Dispose()
        {
            if (!_isInitialized)
                return;

            CancelAndWaitAsync(0).Forget();
        }
    }
}