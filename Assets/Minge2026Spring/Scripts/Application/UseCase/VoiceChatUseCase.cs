using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class VoiceChatUseCase
    {
        private readonly IFMODAudioInputProvider _fmodAudioInputProvider;
        private readonly ISpeechRecognitionProvider _speechRecognitionProvider;

        public VoiceChatUseCase()
        {
            
        }
    }
}