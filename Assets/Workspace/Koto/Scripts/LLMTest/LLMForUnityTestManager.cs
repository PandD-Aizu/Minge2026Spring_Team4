using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Workspace.Koto.Scripts.LLMTest
{
    public class LlmForUnityTestManager : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField, Tooltip("StreamingAssets からの相対パス")]
        private string modelRelativePath = "Qwen3.5_2B/Qwen3.5-2B-Q4_K_M.gguf";

        [SerializeField, Min(128)]
        private uint contextSize = 2048;

        [Header("Sampling")]
        [SerializeField]
        private float temperature = 0.8f;

        [SerializeField, Range(0f, 1f)]
        private float topP = 0.95f;

        [SerializeField, Min(1)]
        private int topK = 40;

        [SerializeField, Min(1f)]
        private float repeatPenalty = 1.1f;

        [Header("Prompt")]
        [SerializeField, TextArea(4, 8)]
        private string prompt = "こんにちは。あなたは誰ですか？短く自己紹介してください。";

        [SerializeField, Min(1)]
        private int maxNewTokens = 128;

        [SerializeField]
        private bool withHistory;

        [Header("Runtime")]
        [SerializeField]
        private bool runOnStart = true;

        [SerializeField]
        private bool showOverlay = true;

        [SerializeField, Min(1f)]
        private float noResponseWarningSeconds = 10f;

        [SerializeField, TextArea(3, 6)]
        private string statusMessage = "待機中";

        [SerializeField, TextArea(8, 20)]
        private string generatedText = string.Empty;

        private readonly object responseLock = new object();
        private readonly StringBuilder responseBuilder = new StringBuilder();

        private bool isGenerating;
        private bool isShuttingDown;
        private bool isInitialized;
        private bool hasReceivedFirstToken;
        private bool firstTokenStatusShown;
        private string initializedModelPath = string.Empty;
        private uint initializedContextSize;
        private double lastInitSeconds;
        private string latestResponse = string.Empty;
        private Vector2 scrollPosition;

        private void Start()
        {
            if (runOnStart)
            {
                RunTest();
            }
        }

        private void Update()
        {
            lock (responseLock)
            {
                generatedText = latestResponse;
            }

            if (hasReceivedFirstToken && !firstTokenStatusShown)
            {
                statusMessage = "応答を受信中...";
                firstTokenStatusShown = true;
            }
        }

        [ContextMenu("Run LLM Test")]
        public void RunTest()
        {
            if (isShuttingDown)
            {
                Debug.LogWarning("[LLMTest] 終了処理中のため実行できません。");
                return;
            }

            if (isGenerating)
            {
                Debug.LogWarning("[LLMTest] すでに生成中です。");
                return;
            }

            RunTestSafelyAsync();
        }

        private async void RunTestSafelyAsync()
        {
            try
            {
                await RunTestAsync();
            }
            catch (DllNotFoundException ex)
            {
                statusMessage = $"ネイティブ DLL が見つかりません: {ex.Message}";
                Debug.LogException(ex);
                isGenerating = false;
            }
            catch (EntryPointNotFoundException ex)
            {
                statusMessage = $"ネイティブ関数が見つかりません: {ex.Message}";
                Debug.LogException(ex);
                isGenerating = false;
            }
            catch (Exception ex)
            {
                statusMessage = $"予期しない例外が発生しました: {ex.Message}";
                Debug.LogException(ex);
                isGenerating = false;
            }
        }

        [ContextMenu("Cancel LLM Test")]
        public async void CancelTest()
        {
            if (!isGenerating)
            {
                return;
            }

            statusMessage = "キャンセル要求を送信しました。";
            await LLMForUnityManager.CancelAndWaitAsync();
            isGenerating = false;
        }

        private async void OnDisable()
        {
            await CleanupOnExitAsync("OnDisable");
        }

        private async void OnDestroy()
        {
            await CleanupOnExitAsync("OnDestroy");
        }

        private async Task CleanupOnExitAsync(string source)
        {
            if (isShuttingDown)
            {
                return;
            }

            isShuttingDown = true;

            if (isGenerating)
            {
                statusMessage = "終了処理中...";
                Debug.Log($"[LLMTest] {source}: 実行中タスクを停止します。");
                await LLMForUnityManager.CancelAndWaitAsync();
            }

            isGenerating = false;
        }

        private async Task RunTestAsync()
        {
            isGenerating = true;
            statusMessage = "初期化中...";
            hasReceivedFirstToken = false;
            firstTokenStatusShown = false;

            lock (responseLock)
            {
                responseBuilder.Clear();
                latestResponse = string.Empty;
            }

            Debug.Log("[LLMTest] SetupLogging を呼び出します。");
            LLMForUnityManager.SetupLogging();
            Debug.Log("[LLMTest] SetupLogging 完了。");

            string modelPath = Path.Combine(Application.streamingAssetsPath, modelRelativePath);
            if (!File.Exists(modelPath))
            {
                statusMessage = $"モデルが見つかりません: {modelPath}";
                Debug.LogError($"[LLMTest] {statusMessage}");
                isGenerating = false;
                return;
            }

            bool reusedInitialization = isInitialized
                && initializedModelPath == modelPath
                && initializedContextSize == contextSize;

            if (reusedInitialization)
            {
                statusMessage = $"初期化済みモデルを再利用します ({lastInitSeconds:F2} 秒で初期化済み)";
                Debug.Log($"[LLMTest] 初期化をスキップ: {Path.GetFileName(modelPath)} は既に読み込み済みです。");
            }
            else
            {
                await Task.Yield();

                var initStopwatch = System.Diagnostics.Stopwatch.StartNew();
                Debug.Log($"[LLMTest] モデル初期化開始: {modelPath} (nCtx={contextSize})");

                if (!LLMForUnityManager.Init(modelPath, contextSize))
                {
                    initStopwatch.Stop();
                    statusMessage = $"モデル初期化に失敗しました ({initStopwatch.Elapsed.TotalSeconds:F2} 秒): {LLMForUnityManager.GetLastError()}";
                    Debug.LogError($"[LLMTest] {statusMessage}");
                    isGenerating = false;
                    return;
                }

                initStopwatch.Stop();
                isInitialized = true;
                initializedModelPath = modelPath;
                initializedContextSize = contextSize;
                lastInitSeconds = initStopwatch.Elapsed.TotalSeconds;
                statusMessage = $"初期化完了 ({lastInitSeconds:F2} 秒)";
                Debug.Log($"[LLMTest] モデル初期化完了: {Path.GetFileName(modelPath)} ({lastInitSeconds:F2} 秒)");
            }

            var sampling = new LLMForUnityManager.SamplingParams
            {
                Temperature = temperature,
                TopP = topP,
                TopK = topK,
                RepeatPenalty = repeatPenalty,
            };

            if (!LLMForUnityManager.SetSampling(sampling))
            {
                Debug.LogWarning("[LLMTest] サンプリング設定の反映に失敗しました。既定値のまま続行する可能性があります。");
            }

            statusMessage = "生成中...";
            Debug.Log($"[LLMTest] 生成開始: {prompt}");

            bool success;
            try
            {
                Task<bool> generateTask = LLMForUnityManager.GenerateAsync(
                    prompt,
                    maxNewTokens,
                    token =>
                    {
                        lock (responseLock)
                        {
                            responseBuilder.Append(token);
                            latestResponse = responseBuilder.ToString();
                            hasReceivedFirstToken = true;
                        }
                    },
                    withHistory);

                Task completedTask = await Task.WhenAny(generateTask, Task.Delay(TimeSpan.FromSeconds(noResponseWarningSeconds)));
                if (completedTask != generateTask)
                {
                    statusMessage = $"生成要求は送信済みですが、{noResponseWarningSeconds:F0} 秒経っても応答がありません。";
                    Debug.LogWarning($"[LLMTest] generate_async 呼び出し後、{noResponseWarningSeconds:F0} 秒経過してもトークンまたは完了通知がありません。");
                }

                success = await generateTask;
            }
            catch (Exception ex)
            {
                statusMessage = $"例外が発生しました: {ex.Message}";
                Debug.LogException(ex);
                isGenerating = false;
                return;
            }

            if (success)
            {
                var stats = LLMForUnityManager.GetPerfStats();
                statusMessage = $"生成完了: {stats.TokensGenerated} tokens, {stats.TokensPerSec:F2} tok/s";
                lock (responseLock)
                {
                    generatedText = latestResponse;
                }

                Debug.Log($"[LLMTest] 生成成功\n{generatedText}");
                Debug.Log($"[LLMTest] Perf: prefill={stats.PrefillMs:F1}ms decode={stats.DecodeMs:F1}ms tok/s={stats.TokensPerSec:F2}");
            }
            else
            {
                statusMessage = $"生成失敗: {LLMForUnityManager.GetLastError()}";
                Debug.LogError($"[LLMTest] {statusMessage}");
            }

            isGenerating = false;
        }

        private void OnGUI()
        {
            if (!showOverlay)
            {
                return;
            }

            const int width = 520;
            GUILayout.BeginArea(new Rect(16, 16, width, 420), GUI.skin.box);
            GUILayout.Label("LLMForUnity Test", GUI.skin.label);
            GUILayout.Label($"Model: {modelRelativePath}");
            GUILayout.Label($"Status: {statusMessage}");
            GUILayout.Label($"Initialized: {(isInitialized ? "Yes" : "No")}");

            GUILayout.Space(8);
            GUILayout.Label("Prompt");
            prompt = GUILayout.TextArea(prompt, GUILayout.Height(80));

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUI.enabled = !isGenerating;
            if (GUILayout.Button("Run", GUILayout.Height(30)))
            {
                RunTest();
            }

            GUI.enabled = isGenerating;
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                CancelTest();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Output");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(220));
            GUILayout.TextArea(generatedText, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

