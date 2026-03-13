using System;
using FMODUnity;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class FMODVCAService : IDisposable
    {
        private readonly FMODSettingsRepository _settingsRepository;
        
        private FMOD.Studio.VCA _masterVCA = RuntimeManager.GetVCA("vca:/Master");
        private FMOD.Studio.VCA _bgmVCA = RuntimeManager.GetVCA("vca:/BGM");
        private FMOD.Studio.VCA _seVCA = RuntimeManager.GetVCA("vca:/SE");

        public FMODVCAService(FMODSettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }
        
        public void SetMasterVolume(float volume)
        {
            _masterVCA.setVolume(Mathf.Clamp01(volume));
        }
        
        public void SetBGMVolume(float volume)
        {
            _bgmVCA.setVolume(Mathf.Clamp01(volume));
        }
        
        public void SetSEVolume(float volume)
        {
            _seVCA.setVolume(Mathf.Clamp01(volume));
        }

        public void Dispose()
        {
            _masterVCA.clearHandle();
            _bgmVCA.clearHandle();
            _seVCA.clearHandle();
        }
    }
}