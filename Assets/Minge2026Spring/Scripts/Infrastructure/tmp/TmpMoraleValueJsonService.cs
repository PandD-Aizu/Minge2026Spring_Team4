using System;
using System.IO;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Domain.Entities;
using UnityEngine;

namespace Minge2026Spring.Scripts.Infrastructure.tmp
{
    [Serializable]
    public class CharacterMoraleValueJson
    {
        public int MoraleValue1;
        public int MoraleValue2;
        public int MoraleValue3;
        public int MoraleValue4;
    }

    /// <summary>
    /// Temporary JSON utility for external executables that require MoraleValue1-4 schema.
    /// Existing save flow is kept untouched.
    /// </summary>
    public class TmpMoraleValueJsonService : ITmpMoraleJsonExporter
    {
        public const string DefaultRelativePath = "I_gonna_be_the_tresure_hunter/CharactersMoraleValue.json";

        public string GetDefaultPath()
        {
            return Path.Combine(UnityEngine.Application.streamingAssetsPath, DefaultRelativePath);
        }

        public CharacterMoraleValueJson LoadRaw(string filePath = null)
        {
            var targetPath = string.IsNullOrEmpty(filePath) ? GetDefaultPath() : filePath;
            if (!File.Exists(targetPath))
            {
                return new CharacterMoraleValueJson();
            }

            var text = File.ReadAllText(targetPath);
            return JsonUtility.FromJson<CharacterMoraleValueJson>(text) ?? new CharacterMoraleValueJson();
        }

        public void SaveRaw(CharacterMoraleValueJson moraleJson, string filePath = null)
        {
            if (moraleJson == null) throw new ArgumentNullException(nameof(moraleJson));

            var targetPath = string.IsNullOrEmpty(filePath) ? GetDefaultPath() : filePath;
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var text = JsonUtility.ToJson(moraleJson, true);
            File.WriteAllText(targetPath, text);
        }

        public void SaveFromCollection(InternalParameterCollection collection, string filePath = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            var moraleJson = LoadRaw(filePath);
            moraleJson.MoraleValue1 = collection.GetParameter("CharacterA")?.Morale ?? 0;
            moraleJson.MoraleValue2 = collection.GetParameter("CharacterB")?.Morale ?? 0;
            moraleJson.MoraleValue3 = collection.GetParameter("CharacterC")?.Morale ?? 0;
            moraleJson.MoraleValue4 = collection.GetParameter("CharacterD")?.Morale ?? 0;

            SaveRaw(moraleJson, filePath);
        }

        public InternalParameterCollection LoadAsCollection(string filePath = null)
        {
            var moraleJson = LoadRaw(filePath);
            var collection = new InternalParameterCollection();

            collection.SetParameter("CharacterA", new InternalParameter { Morale = moraleJson.MoraleValue1 });
            collection.SetParameter("CharacterB", new InternalParameter { Morale = moraleJson.MoraleValue2 });
            collection.SetParameter("CharacterC", new InternalParameter { Morale = moraleJson.MoraleValue3 });
            collection.SetParameter("CharacterD", new InternalParameter { Morale = moraleJson.MoraleValue4 });

            return collection;
        }

        public void Export(MoraleDto moraleDto)
        {
            if (moraleDto == null) throw new ArgumentNullException(nameof(moraleDto));

            moraleDto.MoraleMap.TryGetValue(CharacterMoraleKeys.CharacterA, out var moraleA);
            moraleDto.MoraleMap.TryGetValue(CharacterMoraleKeys.CharacterB, out var moraleB);
            moraleDto.MoraleMap.TryGetValue(CharacterMoraleKeys.CharacterC, out var moraleC);
            moraleDto.MoraleMap.TryGetValue(CharacterMoraleKeys.CharacterD, out var moraleD);

            var moraleJson = LoadRaw();
            moraleJson.MoraleValue1 = moraleA;
            moraleJson.MoraleValue2 = moraleB;
            moraleJson.MoraleValue3 = moraleC;
            moraleJson.MoraleValue4 = moraleD;
            SaveRaw(moraleJson);
        }

    }
}

