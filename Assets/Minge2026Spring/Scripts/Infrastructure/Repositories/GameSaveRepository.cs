using System;
using System.IO;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Infrastructure.tmp;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.Repositories
{
    public class GameSaveRepository : IGameSaveRepository
    {
        private const string SaveFileName = "GameSave.json";
        private const string MoraleFilePath = "I_gonna_be_the_tresure_hunter/CharactersMoraleValue.json";
        private readonly string _savePath = Path.Combine(UnityEngine.Application.persistentDataPath, SaveFileName);
        private readonly string _moraleSavePath = Path.Combine(UnityEngine.Application.streamingAssetsPath, MoraleFilePath);

        /// <inheritdoc />
        public GameSaveData Load()
        {
            if (!File.Exists(_savePath))
                return null;

            try
            {
                // 永続化されたゲーム進行をJSONから復元する
                var json = File.ReadAllText(_savePath);
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameSaveRepository] Failed to load save data: {_savePath}");
                Debug.LogException(exception);
                return null;
            }
        }

        /// <inheritdoc />
        public void Save(GameSaveData saveData)
        {
            if (saveData is null)
                return;

            try
            {
                // 一時ファイルを完成させてから本体へ置き換える
                var saveDirectory = Path.GetDirectoryName(_savePath);
                if (!string.IsNullOrEmpty(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                var json = JsonUtility.ToJson(saveData, true);
                var temporaryPath = $"{_savePath}.tmp";
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(_savePath))
                    File.Replace(temporaryPath, _savePath, null);
                else
                    File.Move(temporaryPath, _savePath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameSaveRepository] Failed to save data: {_savePath}");
                Debug.LogException(exception);
            }
        }

        /// <inheritdoc />
        public void Delete()
        {
            try
            {
                // 将来のオプション画面から利用する削除処理を集約する
                if (File.Exists(_savePath))
                    File.Delete(_savePath);

                // 外部ゲームがこのファイルを必ず読み込むため、削除ではなく
                // 初期値を書き戻す。隠しアイテムは未取得状態へ戻す。
                var resetMoraleData = new CharacterMoraleValueJson();
                var moraleDirectory = Path.GetDirectoryName(_moraleSavePath);
                if (!string.IsNullOrEmpty(moraleDirectory))
                    Directory.CreateDirectory(moraleDirectory);
                File.WriteAllText(_moraleSavePath, JsonUtility.ToJson(resetMoraleData, true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameSaveRepository] Failed to delete save data: {_savePath}");
                Debug.LogException(exception);
            }
        }
    }
}
