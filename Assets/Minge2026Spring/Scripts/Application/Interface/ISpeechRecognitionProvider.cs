using System;
using System.Runtime.InteropServices;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface ISpeechRecognitionProvider
    {
        public string ProcessSpeechRecognition(IntPtr audio, int sampleLength);
    }
}