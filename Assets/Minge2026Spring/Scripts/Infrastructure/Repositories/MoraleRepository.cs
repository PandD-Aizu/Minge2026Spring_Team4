using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Domain.Entities;
using Minge2026Spring.Scripts.Domain.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.Repositories
{
    public class MoraleRepository : IInternalParameterRepository
    {
        private readonly IJsonUtilityProvider _jsonUtilityProvider;
        private readonly IJsonFileCreator _jsonFileCreator;
        
        private readonly string _savePath =
            Path.Combine(UnityEngine.Application.persistentDataPath, "InternalParameters.json");

        public MoraleRepository(IJsonUtilityProvider jsonUtilityProvider, IJsonFileCreator jsonFileCreator)
        {
            _jsonUtilityProvider = jsonUtilityProvider;
            _jsonFileCreator = jsonFileCreator;
        }
        
        public async UniTask<InternalParameterCollection> LoadAsync()
        {
            if (!IsSaveDataExists())
            {
                UnityEngine.Debug.Log("[MoraleRepository] No Save data. Returning default values");
                return new InternalParameterCollection();
            }

            try
            {
                var obj = await _jsonUtilityProvider.ConvertJsonToAnyObjectAsync<InternalParameterCollection>(_savePath);
                return obj ?? new InternalParameterCollection();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MoraleRepository] Failed to load data from {_savePath}. Returning default values. Exception: {e}");
                return new InternalParameterCollection();
            }
        }

        public void SaveAsync(InternalParameterCollection collection)
        {
            try
            {
                var jsonText = _jsonUtilityProvider.ConvertAnyObjectToJsonAsync(collection, true);
                _jsonFileCreator.CreateTextToJsonFile(_savePath, jsonText);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MoraleRepository] Failed to save data to {_savePath}. Exception: {e}");
            }
        }

        public bool IsSaveDataExists()
        {
            return File.Exists(_savePath);
        }
    }
}