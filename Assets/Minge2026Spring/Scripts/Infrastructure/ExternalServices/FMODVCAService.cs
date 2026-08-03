using System;
using FMODUnity;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class FMODVCAService : IFMODVCAService, IDisposable
    {
        private readonly IFMODSettingsRepository _settingsRepository;
        
        private readonly FMOD.Studio.VCA _masterVCA;
        private readonly FMOD.Studio.VCA _bgmVCA;
        private readonly FMOD.Studio.VCA _seVCA;
        private readonly FMOD.Studio.VCA _voiceVCA;

        public FMODVCAService(IFMODSettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
            _masterVCA = RuntimeManager.GetVCA("vca:/Master");
            _bgmVCA = RuntimeManager.GetVCA("vca:/BGM");
            _seVCA = RuntimeManager.GetVCA("vca:/SE");

            // Voice VCAが未登録のバンクでも他チャンネルの設定を利用可能にする
            try
            {
                _voiceVCA = RuntimeManager.GetVCA("vca:/Voice");
            }
            catch (VCANotFoundException)
            {
                Debug.LogWarning("[FMODVCAService] Voice VCA was not found, volume is saved without applying to FMOD");
            }

            _settingsRepository.LoadSettingsAsync();
        }
        
        /// <inheritdoc/>
        public void SetMasterVolume(float volume)
        {
            _masterVCA.setVolume(Mathf.Clamp01(volume));
            _settingsRepository.MasterVolume = volume;
            _settingsRepository.SaveSettings();
        }
        
        /// <inheritdoc/>
        public void SetBGMVolume(float volume)
        {
            _bgmVCA.setVolume(Mathf.Clamp01(volume));
            _settingsRepository.BgmVolume = volume;
            _settingsRepository.SaveSettings();
        }
        
        /// <inheritdoc/>
        public void SetSEVolume(float volume)
        {
            _seVCA.setVolume(Mathf.Clamp01(volume));
            _settingsRepository.SeVolume = volume;
            _settingsRepository.SaveSettings();
        }

        /// <inheritdoc/>
        public void SetVoiceVolume(float volume)
        {
            if (_voiceVCA.isValid())
                _voiceVCA.setVolume(Mathf.Clamp01(volume));

            _settingsRepository.VoiceVolume = volume;
            _settingsRepository.SaveSettings();
        }
        
        public float GetMasterVolume() => _settingsRepository.MasterVolume;
        public float GetBGMVolume() => _settingsRepository.BgmVolume;
        public float GetSEVolume() => _settingsRepository.SeVolume;
        /// <inheritdoc/>
        public float GetVoiceVolume() => _settingsRepository.VoiceVolume;

        public void Dispose()
        {
            _masterVCA.clearHandle();
            _bgmVCA.clearHandle();
            _seVCA.clearHandle();
            if (_voiceVCA.isValid())
                _voiceVCA.clearHandle();
        }
    }
}
