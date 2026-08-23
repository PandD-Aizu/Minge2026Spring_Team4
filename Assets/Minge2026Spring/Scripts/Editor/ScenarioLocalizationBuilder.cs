using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Minge2026Spring.Scripts.Editor
{
    /// <summary>
    /// シナリオ原稿からUnity Localizationの3テーブルを再現し、
    /// Chapter JSONと翻訳データの参照整合性を検査する。
    /// </summary>
    public static class ScenarioLocalizationBuilder
    {
        private const string SourcePath =
            "Assets/Minge2026Spring/Localization/Scenario/ScenarioLocalizationSource.json";

        private const string TableDirectory =
            "Assets/Minge2026Spring/Localization/Scenario";

        private const string ChapterDataDirectory =
            "Assets/Minge2026Spring/ChapterData";

        private static readonly Regex JapaneseCharacterPattern =
            new("[\\u3040-\\u30ff\\u3400-\\u9fff]", RegexOptions.Compiled);

        [Serializable]
        private sealed class ScenarioLocalizationSource
        {
            public ScenarioLocalizationEntry[] characters;
            public ScenarioLocalizationEntry[] dialogue;
            public ScenarioLocalizationEntry[] choices;
        }

        [Serializable]
        private sealed class ScenarioLocalizationEntry
        {
            public string key;
            public string japanese;
            public string english;
        }

        [MenuItem("Tools/Minge2026Spring/Localization/Rebuild Scenario Tables")]
        public static void RebuildScenarioTables()
        {
            var source = LoadAndValidateSource();
            var japaneseLocale = LocalizationEditorSettings.GetLocale("ja");
            var englishLocale = LocalizationEditorSettings.GetLocale("en");
            if (japaneseLocale is null || englishLocale is null)
                throw new InvalidOperationException(
                    "Japanese (ja) and English (en) Locales must exist before rebuilding scenario tables.");

            Directory.CreateDirectory(TableDirectory);
            UpdateCollection("Characters", source.characters, japaneseLocale.Identifier, englishLocale.Identifier);
            UpdateCollection("Dialogue", source.dialogue, japaneseLocale.Identifier, englishLocale.Identifier);
            UpdateCollection("Choices", source.choices, japaneseLocale.Identifier, englishLocale.Identifier);

            AssetDatabase.SaveAssets();
            ValidateScenarioLocalization();
            Debug.Log("[ScenarioLocalization] Rebuilt Characters, Dialogue, and Choices tables.");
        }

        [MenuItem("Tools/Minge2026Spring/Localization/Validate Scenario Localization")]
        public static void ValidateScenarioLocalization()
        {
            var source = LoadAndValidateSource();
            var characterKeys = source.characters.Select(entry => entry.key).ToHashSet(StringComparer.Ordinal);
            var dialogueKeys = source.dialogue.Select(entry => entry.key).ToHashSet(StringComparer.Ordinal);
            var choiceKeys = source.choices.Select(entry => entry.key).ToHashSet(StringComparer.Ordinal);
            var referencedCharacterKeys = new HashSet<string>(StringComparer.Ordinal);
            var referencedDialogueKeys = new HashSet<string>(StringComparer.Ordinal);
            var referencedChoiceKeys = new HashSet<string>(StringComparer.Ordinal);
            // NovelSceneの固定NameTextが直接参照する表示名。
            referencedCharacterKeys.Add("character.reno");

            var jsonFiles = Directory.GetFiles(ChapterDataDirectory, "*.json", SearchOption.AllDirectories);
            if (jsonFiles.Length != 19)
                throw new InvalidOperationException(
                    $"Expected 19 Chapter JSON files, but found {jsonFiles.Length}.");

            foreach (var path in jsonFiles)
            {
                var json = File.ReadAllText(path);
                RejectLegacyTextFields(path, json);

                var chapter = JsonUtility.FromJson<Chapter>(json);
                if (chapter?.blocks is null)
                    throw new InvalidOperationException($"Chapter has no blocks: {path}");

                foreach (var block in chapter.blocks)
                {
                    if (string.IsNullOrWhiteSpace(block.blockId))
                        throw new InvalidOperationException($"A block has no blockId: {path}");

                    foreach (var dialogue in block.dialogues ?? Array.Empty<Dialogue>())
                    {
                        RequireReference(path, block.blockId, "speaker", dialogue.speakerKey,
                            characterKeys, referencedCharacterKeys);
                        RequireReference(path, block.blockId, "message", dialogue.messageKey,
                            dialogueKeys, referencedDialogueKeys);
                    }

                    foreach (var choice in block.choices ?? Array.Empty<Choice>())
                    {
                        RequireReference(path, block.blockId, "choice", choice.choiceTextKey,
                            choiceKeys, referencedChoiceKeys);
                    }
                }
            }

            RequireNoOrphans("Characters", characterKeys, referencedCharacterKeys);
            RequireNoOrphans("Dialogue", dialogueKeys, referencedDialogueKeys);
            RequireNoOrphans("Choices", choiceKeys, referencedChoiceKeys);
            ValidateBuiltCollectionIfPresent("Characters", source.characters);
            ValidateBuiltCollectionIfPresent("Dialogue", source.dialogue);
            ValidateBuiltCollectionIfPresent("Choices", source.choices);

            Debug.Log(
                $"[ScenarioLocalization] Validation passed: {jsonFiles.Length} JSON files, " +
                $"{dialogueKeys.Count} dialogue entries, {choiceKeys.Count} choices, " +
                $"{characterKeys.Count} characters.");
        }

        private static ScenarioLocalizationSource LoadAndValidateSource()
        {
            var sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(SourcePath);
            if (sourceAsset is null)
                throw new FileNotFoundException("Scenario localization source was not found.", SourcePath);

            var source = JsonUtility.FromJson<ScenarioLocalizationSource>(sourceAsset.text);
            if (source is null)
                throw new InvalidOperationException("Scenario localization source is invalid JSON.");

            source.characters ??= Array.Empty<ScenarioLocalizationEntry>();
            source.dialogue ??= Array.Empty<ScenarioLocalizationEntry>();
            source.choices ??= Array.Empty<ScenarioLocalizationEntry>();

            ValidateEntries("Characters", source.characters);
            ValidateEntries("Dialogue", source.dialogue);
            ValidateEntries("Choices", source.choices);
            return source;
        }

        private static void ValidateEntries(string tableName, IReadOnlyCollection<ScenarioLocalizationEntry> entries)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.key) ||
                    string.IsNullOrEmpty(entry.japanese) ||
                    string.IsNullOrEmpty(entry.english))
                {
                    throw new InvalidOperationException($"{tableName} contains an empty key or translation.");
                }

                if (!keys.Add(entry.key))
                    throw new InvalidOperationException($"{tableName} contains duplicate key: {entry.key}");

                if (JapaneseCharacterPattern.IsMatch(entry.english))
                    throw new InvalidOperationException(
                        $"{tableName}/{entry.key} still contains Japanese characters in English.");
            }
        }

        private static void UpdateCollection(
            string tableName,
            IReadOnlyCollection<ScenarioLocalizationEntry> entries,
            LocaleIdentifier japaneseLocale,
            LocaleIdentifier englishLocale)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName) ??
                             LocalizationEditorSettings.CreateStringTableCollection(
                                 tableName,
                                 TableDirectory,
                                 new List<UnityEngine.Localization.Locale>
                                 {
                                     LocalizationEditorSettings.GetLocale(japaneseLocale),
                                     LocalizationEditorSettings.GetLocale(englishLocale)
                                 });

            var japaneseTable = collection.GetTable(japaneseLocale) as StringTable ??
                                collection.AddNewTable(japaneseLocale) as StringTable;
            var englishTable = collection.GetTable(englishLocale) as StringTable ??
                               collection.AddNewTable(englishLocale) as StringTable;

            if (japaneseTable is null || englishTable is null)
                throw new InvalidOperationException($"Could not create locale tables for {tableName}.");

            var sourceKeys = entries.Select(entry => entry.key).ToHashSet(StringComparer.Ordinal);
            var obsoleteKeys = collection.SharedData.Entries
                .Select(entry => entry.Key)
                .Where(key => !sourceKeys.Contains(key))
                .ToArray();
            foreach (var obsoleteKey in obsoleteKeys)
                collection.RemoveEntry(obsoleteKey);

            foreach (var entry in entries)
            {
                SetEntry(japaneseTable, entry.key, entry.japanese);
                SetEntry(englishTable, entry.key, entry.english);
            }

            EditorUtility.SetDirty(japaneseTable);
            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(collection.SharedData);
            collection.RefreshAddressables();
        }

        private static void SetEntry(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
        }

        private static void RejectLegacyTextFields(string path, string json)
        {
            if (Regex.IsMatch(json, "\\\"(speaker|message|choiceText)\\\"\\s*:"))
                throw new InvalidOperationException($"Legacy localized text field remains in {path}.");
        }

        private static void RequireReference(
            string path,
            string blockId,
            string valueName,
            string key,
            HashSet<string> sourceKeys,
            ISet<string> referencedKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException($"{path}/{blockId} has an empty {valueName} key.");
            if (!sourceKeys.Contains(key))
                throw new InvalidOperationException($"Missing source entry for {path}/{blockId}: {key}");
            referencedKeys.Add(key);
        }

        private static void RequireNoOrphans(
            string tableName,
            HashSet<string> sourceKeys,
            HashSet<string> referencedKeys)
        {
            var orphan = sourceKeys.FirstOrDefault(key => !referencedKeys.Contains(key));
            if (orphan is not null)
                throw new InvalidOperationException($"{tableName} contains unreferenced key: {orphan}");
        }

        private static void ValidateBuiltCollectionIfPresent(
            string tableName,
            IReadOnlyCollection<ScenarioLocalizationEntry> sourceEntries)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection is null)
                return;

            var expectedKeys = sourceEntries.Select(entry => entry.key).ToHashSet(StringComparer.Ordinal);
            var actualKeys = collection.SharedData.Entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
            if (!expectedKeys.SetEquals(actualKeys))
                throw new InvalidOperationException($"{tableName} table keys do not match the source data.");

            foreach (var localeCode in new[] { "ja", "en" })
            {
                if (collection.GetTable(localeCode) is not StringTable table)
                    throw new InvalidOperationException($"{tableName} has no {localeCode} table.");

                foreach (var key in expectedKeys)
                {
                    if (string.IsNullOrEmpty(table.GetEntry(key)?.LocalizedValue))
                        throw new InvalidOperationException($"{tableName}/{key} is empty for {localeCode}.");
                }
            }
        }
    }
}
