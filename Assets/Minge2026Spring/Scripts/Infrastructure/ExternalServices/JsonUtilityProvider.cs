using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class JsonUtilityProvider : IJsonUtilityProvider
    {
        /// <summary>
        /// JsonファイルをAddressablesから読み込んで、任意のオブジェクトに変換する
        /// </summary>
        /// <param name="addressableJsonKey">読み込むJsonファイル</param>
        /// <typeparam name="T">Jsonファイルを変換した後の入れ子のオブジェクトの型</typeparam>
        /// <returns>Jsonファイルのデータを内包したオブジェクト</returns>
        public T ConvertJsonToAnyObject<T>(string addressableJsonKey)
        {
            // nullチェック
            if (addressableJsonKey is null)
            {
                Debug.LogError("Addressable Key is null.");
                return default;
            }

            // AddressablesからJsonファイルを読み込む
            var jsonAsset = Addressables.LoadAssetAsync<TextAsset>(addressableJsonKey).WaitForCompletion();
            if (jsonAsset is null)
            {
                Debug.LogError($"Invalid Json Key: {addressableJsonKey}");
                return default;
            }
            
            // 任意の型に入れて返す
            var result = JsonUtility.FromJson<T>(jsonAsset.text);
            if (result is null)
            {
                Debug.LogError("Failed to convert Json: convert result is null.");
                return default;
            }
            
            // 代入可能かチェック
            if (!typeof(T).IsAssignableFrom(result.GetType()))
            {
                Debug.LogError($"Failed to convert Json: convert result type is {result.GetType()}, expected type is assignable from {typeof(T)}.");
                return default;
            }

            // 厳密な型チェック
            if (result.GetType() != typeof(T))
            {
                Debug.LogWarning($"Warning: Json converted to {result.GetType()}, expected type is {typeof(T)}.");
            }
            
            return result;
        }
        
        /// <summary>
        /// オブジェクトをJson形式の文字列に変換する
        /// </summary>
        /// <param name="data">変換するオブジェクト</param>
        /// <param name="prettyPrint">行のインデントを有効にするかどうか</param>
        /// <typeparam name="T">変換したいオブジェクトの型</typeparam>
        /// <returns>Json形式の文字列</returns>
        public string ConvertStringToJson<T>(T data, bool prettyPrint = false)
        {
            // nullチェック
            if (data is null)
            {
                Debug.LogError("Data is null.");
            }
            
            // Json形式の文字列に変換
            string result = JsonUtility.ToJson(data, prettyPrint);
            return result;
        }
    }
}