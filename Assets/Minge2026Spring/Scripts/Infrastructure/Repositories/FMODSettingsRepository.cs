using System.IO;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Infrastructure.DTOs;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.Repositories
{
    public class FMODSettingsRepository : IFMODSettingsRepository
    {
        private readonly IJsonUtilityProvider _jsonUtilityProvider;
        private readonly IJsonFileCreator _jsonCreator;
        
        private const string SettingsFileName = "FMODSettings.json";
        private string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, SettingsFileName);
        
        public float MasterVolume { get; set; } = 1.0f;
        public float BgmVolume { get; set; } = 1.0f;
        public float SeVolume { get; set; } = 1.0f;

        public FMODSettingsRepository(
            IJsonUtilityProvider jsonUtilityProvider,
            IJsonFileCreator jsonCreator)
        {
            _jsonUtilityProvider = jsonUtilityProvider;
            _jsonCreator = jsonCreator;
            
            LoadSettingsAsync().Forget();
        }

        /// <inheritdoc/>
        public void SaveSettings()
        {
            var settingsData = new FMODSettingsData
            {
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SeVolume = SeVolume
            };

            string jsonText = _jsonUtilityProvider.ConvertAnyObjectToJsonAsync(settingsData);
            _jsonCreator.CreateTextToJsonFile(filePath, jsonText);
        }

        /// <inheritdoc/>
        public UniTaskVoid LoadSettingsAsync()
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FMODSettingsRepository] Settings file not found: {filePath}");
                return default;
            }

            try
            {
                string jsonText = File.ReadAllText(filePath);
                var settingsData = _jsonUtilityProvider.ConvertRawJsonToAnyObject<FMODSettingsData>(jsonText);
                if (settingsData == null)
                {
                    Debug.LogError($"[FMODSettingsRepository] Failed to parse settings JSON: {filePath}");
                    return default;
                }
                
                MasterVolume = settingsData.MasterVolume;
                BgmVolume = settingsData.BgmVolume;
                SeVolume = settingsData.SeVolume;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FMODSettingsRepository] Failed to load settings from file: {filePath}");
                Debug.LogError(e);
            }

            return default;
        }
    }
}