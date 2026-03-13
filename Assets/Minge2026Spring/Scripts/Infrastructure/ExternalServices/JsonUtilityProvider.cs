using System;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class JsonUtilityProvider : IJsonUtilityProvider
    {
        /// <inheritdoc />
        public async UniTask<T> ConvertJsonToAnyObjectAsync<T>(string addressableJsonKey)
        {
            if (string.IsNullOrEmpty(addressableJsonKey))
            {
                UnityEngine.Debug.LogError($"[JsonUtilityProvider] Invalid addressable JSON key: {addressableJsonKey}");
                return default;
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(addressableJsonKey);
            var jsonAsset = await handle.Task;

            try
            {
                if (jsonAsset is null)
                {
                    UnityEngine.Debug.LogError($"[JsonUtilityProvider] Failed to load JSON asset with key: {addressableJsonKey}");
                    return default;
                }

                var result = JsonUtility.FromJson<T>(jsonAsset.text);
                if (result is null)
                {
                    UnityEngine.Debug.LogError($"[JsonUtilityProvider] Failed to parse JSON from asset with key: {addressableJsonKey}");
                    return default;
                }

                return result;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[JsonUtilityProvider] Exception occurred while converting JSON to object with key: {addressableJsonKey}");
                UnityEngine.Debug.LogError(e);
                return default;
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <inheritdoc />
        public string ConvertAnyObjectToJsonAsync<T>(T obj, bool prettyPrint = false)
        {
            if (obj is null)
            {
                UnityEngine.Debug.LogError($"[JsonUtilityProvider] Cannot convert null object to JSON.");
                return string.Empty;
            }
            
            string result = JsonUtility.ToJson(obj, prettyPrint);
            return result;
        }
    }
}