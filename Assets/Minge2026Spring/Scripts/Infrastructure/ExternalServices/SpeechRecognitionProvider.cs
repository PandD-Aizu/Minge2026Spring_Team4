using System;
using System.IO;
using System.Runtime.InteropServices;
using Minge2026Spring.Scripts.Application.Interface;
using VoiceRecognition;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class SpeechRecognitionProvider : ISpeechRecognitionProvider, IDisposable
    {
        private readonly WhisperManager _whisperManager;
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpeechRecognitionProvider()
        {
            _whisperManager = new WhisperManager();
            ConstructPaths(out string modelPath, out string tokenizerPath, out string configPath);
            _whisperManager.Initialize(modelPath, tokenizerPath, configPath);
        }
        
        /// <summary>
        /// 音声認識を行う
        /// </summary>
        /// <param name="sound">FMODのSoundオブジェクト</param>
        /// <returns>文字列</returns>
        public string Transcribe(FMOD.Sound sound)
        {
            // PCMのサンプル数を取得
            sound.getLength(out uint lengthPCM, FMOD.TIMEUNIT.PCM);
            
            // メモリをロックしてポインタを取得
            sound.@lock(0, lengthPCM * 4, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            
            // ポインタからfloat配列に変換
            float[] audioData = new float[lengthPCM];
            
            // 要素数に変換
            int floatCount1 = (int)(len1 / 4);
            Marshal.Copy(ptr1, audioData, 0, floatCount1);
            
            // ポインタが二つの場合
            if (ptr2 != IntPtr.Zero && len2 > 0)
            {
                int floatCount2 = (int)(len2 / 4);
                Marshal.Copy(ptr2, audioData, floatCount1, floatCount2);
            }
            
            // メモリのロックを解除
            sound.unlock(ptr1, ptr2, len1, len2);
            
            // Rust側に配列を渡して文字列を取得
            string text = _whisperManager.Transcribe(audioData);
            return text;
        }

        /// <summary>
        /// パスを構築する
        /// </summary>
        /// <param name="modelPath">モデルデータへのパス</param>
        /// <param name="tokenizerPath">tokenizerへのパス</param>
        /// <param name="configPath">コンフィグへのパス</param>
        private void ConstructPaths(out string modelPath, out string tokenizerPath, out string configPath)
        {
            string basePath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Whisper");
            modelPath = Path.Combine(basePath, "model.safetensors");
            tokenizerPath = Path.Combine(basePath, "tokenizer.json");
            configPath = Path.Combine(basePath, "config.json");
        }

        public void Dispose()
        {
            _whisperManager.Dispose();
        }
    }
}