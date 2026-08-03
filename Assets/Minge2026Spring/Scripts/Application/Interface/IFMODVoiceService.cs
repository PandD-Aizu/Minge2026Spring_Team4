using System.Threading;
using Cysharp.Threading.Tasks;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface IFMODVoiceService
    {
        bool IsPlaying { get; }

        void Play(string eventPath);

        UniTask WaitUntilFinished(CancellationToken cancellationToken);

        void StopCurrentVoice(bool allowFadeOut = false);
    }
}
