using System.IO;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Infrastructure.DTOs;
using Minge2026Spring.Scripts.Infrastructure.ExternalServices;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.Repositories
{
    public class FMODSettingsRepository
    {
        private readonly JsonUtilityProvider _jsonUtilityProvider;
        private readonly JsonCreator _jsonCreator;
        
        private const string SettingsFileName = "FMODSettings.json";
        private string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, SettingsFileName);
        
        public float MasterVolume;
        public float BgmVolume;
        public float SeVolume;

        public FMODSettingsRepository(
            JsonUtilityProvider jsonUtilityProvider,
            JsonCreator jsonCreator)
        {
            _jsonUtilityProvider = jsonUtilityProvider;
            _jsonCreator = jsonCreator;
            
            LoadSettings().Forget();
        }

        /// <summary>
        /// 現在の設定値をjsonファイルにセーブ
        /// </summary>
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

        /// <summary>
        /// jsonファイルから設定値をロードしてプロパティに反映
        /// </summary>
        public async UniTaskVoid LoadSettings()
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[FMODSettingsRepository] Settings file not found: {filePath}");
                return;
            }

            try
            {
                string jsonText = File.ReadAllText(filePath);
                var settingsData = await _jsonUtilityProvider.ConvertJsonToAnyObjectAsync<FMODSettingsData>(jsonText);
                
                MasterVolume = settingsData.MasterVolume;
                BgmVolume = settingsData.BgmVolume;
                SeVolume = settingsData.SeVolume;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FMODSettingsRepository] Failed to load settings from file: {filePath}");
                Debug.LogError(e);
            }
        }
    }
}