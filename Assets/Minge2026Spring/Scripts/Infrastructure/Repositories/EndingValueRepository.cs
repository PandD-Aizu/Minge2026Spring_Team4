using System;
using System.IO;
using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.Repositories
{
    public class EndingValueRepository : IEndingValueProvider
    {
        private const string RelativePath = "I_gonna_be_the_tresure_hunter/EndingValue.json";

        [Serializable]
        private class EndingValueJson
        {
            public int EndingValue;
        }

        /// <inheritdoc />
        public bool TryGetEndingValue(out int endingValue)
        {
            endingValue = default;
            var path = Path.Combine(UnityEngine.Application.streamingAssetsPath, RelativePath);

            // エンディング値の出力ファイルを検証する
            if (!File.Exists(path))
            {
                Debug.LogError($"[EndingValueRepository] Ending value file was not found: {path}");
                return false;
            }

            try
            {
                // 子プロセスが出力したJSONを読み込む
                var json = File.ReadAllText(path);
                if (json.IndexOf("\"EndingValue\"", StringComparison.Ordinal) < 0)
                {
                    Debug.LogError($"[EndingValueRepository] EndingValue field was not found: {path}");
                    return false;
                }

                var data = JsonUtility.FromJson<EndingValueJson>(json);
                if (data is null)
                {
                    Debug.LogError($"[EndingValueRepository] Failed to parse ending value JSON: {path}");
                    return false;
                }

                endingValue = data.EndingValue;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EndingValueRepository] Failed to read ending value JSON: {path}");
                Debug.LogException(exception);
                return false;
            }
        }
    }
}
