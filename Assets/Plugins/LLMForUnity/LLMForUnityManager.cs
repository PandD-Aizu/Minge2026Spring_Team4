using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public static class LLMForUnityManager
{
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const string DLL = "LLMForUnity";
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    private const string DLL = "LLMForUnity_OSX";
    #else
    private const string DLL = "LLMForUnity_Linux";
    #endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogCallback(int level, IntPtr msg, UIntPtr len, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool TokenCallback(IntPtr ptr, UIntPtr len, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DoneCallback(int errorCode, IntPtr userData);

    [StructLayout(LayoutKind.Sequential)]
    public struct SamplingParams
    {
        public float Temperature;
        public float TopP;
        public int TopK;
        public float RepeatPenalty;

        public static SamplingParams Default => new SamplingParams()
        {
            Temperature = 0.8f,
            TopP = 0.95f,
            TopK = 40,
            RepeatPenalty = 1.1f,
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerfStats
    {
        public double PrefillMs;
        public double DecodeMs;
        public uint TokensGenerated;
        public double TokensPerSec;
    }

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void set_log_callback(LogCallback cb, IntPtr userData);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int init(string modelPath, uint nCtx);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int swap_model(string modelPath);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int set_sampling_params(ref SamplingParams p);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int clear_history();
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr generate(string prompt, int maxNewTokens);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int generate_async(
        string prompt, int maxNewTokens,
        TokenCallback tokenCb, DoneCallback doneCb, IntPtr userData);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr generate_with_history(string prompt, int maxNewTokens);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int generate_async_with_history(
        string prompt, int maxNewTokens,
        TokenCallback tokenCb, DoneCallback doneCb, IntPtr userData);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void cancel();
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_perf_stats(out PerfStats stats);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr ptr);
 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_last_error_message();
    
    /* --- 公開API --- */
    private static readonly object _generationStateLock = new object();
    private static LogCallback _logCb;
    private static TokenCallback _tokenCb;
    private static DoneCallback _doneCb;
    private static Action<string> _currentOnToken;
    private static TaskCompletionSource<bool> _activeTcs;
    private static TaskCompletionSource<bool> _generationTcs;
    
    private static readonly TokenCallback _nativeTokenCb = OnTokenReceivedNative;
    private static readonly DoneCallback _nativeDoneCb = OnGenerationDoneNative;

    private class GenerationTaskState
    {
        public Action<string> OnToken;
        public TaskCompletionSource<bool> Tcs;
    }

    /// <summary>
    /// ログコールバックを登録して、Rust側のログをUnityのコンソールに出力するように設定
    /// </summary>
    public static void SetupLogging()
    {
        _logCb = (level, msg, len, _) =>
        {
            string text = Marshal.PtrToStringUTF8(msg, (int)len);
            switch (level)
            {
                case 2: Debug.LogError($"[LLMForUnity] {text}"); break;
                case 1: Debug.LogWarning($"[LLMForUnity] {text}"); break;
                default: Debug.Log($"[LLMForUnity] {text}"); break;
            }
        };
        
        set_log_callback(_logCb, IntPtr.Zero);
    }

    /// <summary>
    /// モデルを初期化する
    /// </summary>
    /// <param name="modelPath">モデルへのパス</param>
    /// <param name="nCtx">トークン数</param>
    /// <returns>true: 成功</returns>
    public static bool Init(string modelPath, uint nCtx = 2048)
    {
        int code = init(modelPath, nCtx);
        if (code != 0)
        {
            Debug.LogError($"[LLMForUnity] Failed to initialize model: {GetLastError()} (code={code})");
        }

        return code == 0;
    }

    /// <summary>
    /// 使用中のモデルを別のモデルに変更する
    /// </summary>
    /// <param name="modelPath">モデルへのパス</param>
    /// <returns>true: 成功</returns>
    public static bool SwapModel(string modelPath)
    {
        int code = swap_model(modelPath);
        if (code != 0)
        {
            Debug.LogError($"[LLMForUnity] Failed to swap model: {GetLastError()} (code={code})");
        }

        return code == 0;
    }

    /// <summary>
    /// サンプリングパラメータを設定
    /// </summary>
    /// <param name="p">サンプリングパラメータ</param>
    /// <returns>true: 成功</returns>
    public static bool SetSampling(SamplingParams p)
    {
        int code = set_sampling_params(ref p);
        return code == 0;
    }

    /// <summary>
    /// 履歴をクリア
    /// </summary>
    /// <returns>true: 成功</returns>
    public static bool ClearHistory()
    {
        int code = clear_history();
        return code == 0;
    }

    /// <summary>
    /// テキスト生成をキャンセル
    /// </summary>
    public static void Cancel()
    {
        try
        {
            cancel();
        }
        catch (EntryPointNotFoundException)
        {
            Debug.LogWarning("[LLMForUnity] Cancel is not supported by the current native plugin.");
        }
    }

    /// <summary>
    /// 生成中であればキャンセルを送信し、短時間だけ完了を待つ
    /// </summary>
    /// <param name="timeoutMs">待機タイムアウト(ms)</param>
    public static async Task CancelAndWaitAsync(int timeoutMs = 1500)
    {
        Task generationTask = _activeTcs?.Task;
        Cancel();

        if (generationTask != null && !generationTask.IsCompleted)
        {
            await Task.WhenAny(generationTask, Task.Delay(timeoutMs));
        }
    }

    /// <summary>
    /// 直前の統計を取得する
    /// </summary>
    /// <returns>統計</returns>
    public static PerfStats GetPerfStats()
    {
        get_perf_stats(out PerfStats stats);
        return stats;
    }

    /// <summary>
    /// 最後のえらーメッセージを取得する
    /// </summary>
    /// <returns>エラーメッセージ</returns>
    public static string GetLastError()
    {
        IntPtr ptr = get_last_error_message();
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        string msg = Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        free_string(ptr);
        return msg;
    }
    
    [AOT.MonoPInvokeCallback(typeof(TokenCallback))]
    private static bool OnTokenReceivedNative(IntPtr ptr, UIntPtr len, IntPtr userData)
    {
        if (userData == IntPtr.Zero) 
            return false;
        
        var handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is GenerationTaskState state)
        {
            string piece = Marshal.PtrToStringUTF8(ptr, (int)(uint)len);
            state.OnToken?.Invoke(piece);
            return true;
        }
        
        return false;
    }

    [AOT.MonoPInvokeCallback(typeof(DoneCallback))]
    private static void OnGenerationDoneNative(int errorCode, IntPtr userData)
    {
        if (userData == IntPtr.Zero) 
            return;

        var handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is GenerationTaskState state)
        {
            state.Tcs?.TrySetResult(errorCode == 0);
        }
        
        handle.Free();
    }

    public static Task<bool> GenerateAsync(
        string prompt, int maxNewTokens, Action<string> onToken, bool withHistory = false)
    {
        var state = new GenerationTaskState
        {
            OnToken = onToken,
            Tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        
        GCHandle handle = GCHandle.Alloc(state);
        IntPtr userData = GCHandle.ToIntPtr(handle);

        _activeTcs = state.Tcs;

        int result = withHistory
            ? generate_async_with_history(prompt, maxNewTokens, _nativeTokenCb, _nativeDoneCb, userData)
            : generate_async(prompt, maxNewTokens, _nativeTokenCb, _nativeDoneCb, userData);

        if (result != 0)
        {
            Debug.LogError($"[LLMForUnity] Failed to start: {GetLastError()}");
            handle.Free();
            return Task.FromResult(false);
        }

        return state.Tcs.Task;
    }
}
