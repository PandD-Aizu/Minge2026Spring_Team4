using System;
using System.Runtime.InteropServices;

namespace VoiceRecognition
{
    public class WhisperManager
    {
        [DllImport("voice_recognition_for_unity")]
        private static extern int init_whisper(string modelPath, string tokenizerPath, string configPath);

        [DllImport("voice_recognition_for_unity")]
        private static extern IntPtr transcribe_audio(float[] data, UIntPtr len);

        [DllImport("voice_recognition_for_unity")]
        private static extern void free_string(IntPtr str);

        /// <summary>
        /// モデルを初期化
        /// </summary>
        /// <param name="modelPath">modelへのパス</param>
        /// <param name="tokenizerPath">tokenizerへのパス</param>
        /// <param name="configPath">configへのパス</param>
        /// <returns>初期化が成功したかどうか</returns>
        public bool Initialize(string modelPath, string tokenizerPath, string configPath)
        {
            int result = init_whisper(modelPath, tokenizerPath, configPath);
            return result == 0;
        }

        /// <summary>
        /// 16kHzの音声データを渡し、テキストに変換して返す
        /// </summary>
        /// <param name="pcm16kHzData">16kHzの音声データ</param>
        /// <returns>認識されたテキスト</returns>
        public string Transcribe(float[] pcm16kHzData)
        {
            if (pcm16kHzData == null || pcm16kHzData.Length == 0)
                return string.Empty;
            
            // Rust側に配列を渡す
            IntPtr resultPtr = transcribe_audio(pcm16kHzData, (UIntPtr)pcm16kHzData.Length);
            if (resultPtr == IntPtr.Zero)
                return string.Empty;

            // Rustから返ってきたCの文字列をC#のstringに変換
            string resultText = Marshal.PtrToStringUTF8(resultPtr);
            
            // Rust側で確保された文字列のメモリを解放
            free_string(resultPtr);

            return resultText;
        }
    }
}