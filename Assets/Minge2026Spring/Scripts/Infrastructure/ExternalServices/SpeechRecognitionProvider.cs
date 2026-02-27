using System;
using System.Runtime.InteropServices;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class SpeechRecognitionProvider : ISpeechRecognitionProvider
    {
        [DllImport("SpeechRecognitionPlugin")]
        private static extern IntPtr recognize_speech(IntPtr audio, int length);
        
        [DllImport("SpeechRecognitionPlugin")]
        private static extern void free_string(IntPtr ptr);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpeechRecognitionProvider()
        {
            
        }
        
        /// <summary>
        /// 音声認識を行う
        /// </summary>
        /// <param name="audio">録音した音声</param>
        /// <param name="sampleLength">サンプルの長さ</param>
        /// <returns>文字列</returns>
        public string ProcessSpeechRecognition(IntPtr audio, int sampleLength)
        {
            // RustからCの文字列ポインタを受け取る
            IntPtr resultPtr = recognize_speech(audio, sampleLength);
            if (resultPtr == IntPtr.Zero)
            {
                return string.Empty;
            }
            
            // C#のstringに変換
            string recognizedText = Marshal.PtrToStringUTF8(resultPtr);
            
            // Rust側のメモリを解放
            free_string(resultPtr);
            
            return recognizedText;
        }
    }
}