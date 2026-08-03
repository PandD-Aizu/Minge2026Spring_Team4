using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public sealed class FMODVoiceService : IFMODVoiceService, IDisposable
    {
        private EventInstance _currentInstance;

        public bool IsPlaying
        {
            get
            {
                if (!_currentInstance.isValid())
                    return false;

                _currentInstance.getPlaybackState(out var state);
                return state != PLAYBACK_STATE.STOPPED;
            }
        }

        public void Play(string eventPath)
        {
            StopCurrentVoice(false);

            if (string.IsNullOrWhiteSpace(eventPath))
                return;

            try
            {
                var eventReference = RuntimeManager.PathToEventReference(eventPath);
                if (eventReference.IsNull)
                {
                    Debug.LogWarning($"[FMODVoiceService] Invalid voice event path: {eventPath}");
                    return;
                }

                _currentInstance = RuntimeManager.CreateInstance(eventReference);
                _currentInstance.start();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FMODVoiceService] Failed to play voice event: {eventPath}");
                Debug.LogException(exception);
                ReleaseCurrentInstance();
            }
        }

        public async UniTask WaitUntilFinished(CancellationToken cancellationToken)
        {
            try
            {
                while (IsPlaying)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                ReleaseCurrentInstance();
            }
        }

        public void StopCurrentVoice(bool allowFadeOut = false)
        {
            if (!_currentInstance.isValid())
                return;

            _currentInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            ReleaseCurrentInstance();
        }

        private void ReleaseCurrentInstance()
        {
            if (!_currentInstance.isValid())
                return;

            _currentInstance.release();
            _currentInstance.clearHandle();
        }

        public void Dispose()
        {
            StopCurrentVoice(false);
        }
    }
}
