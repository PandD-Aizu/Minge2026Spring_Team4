using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using Minge2026Spring.Scripts.Infrastructure.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace Minge2026Spring.Scripts.Editor
{
    /// <summary>
    /// プロジェクトで使用するLocalizationアセットと固定UIのバインドを一括生成する。
    /// 再実行しても同じキーを更新するだけなので、翻訳変更時にも利用できる。
    /// </summary>
    public static class LocalizationProjectGenerator
    {
        private const string RootDirectory = "Assets/Minge2026Spring/Localization";
        private const string LocaleDirectory = RootDirectory + "/Locales";
        private const string TableDirectory = RootDirectory + "/String Tables";
        private const string SettingsPath = RootDirectory + "/LocalizationSettings.asset";
        private const string JapaneseLocalePath = LocaleDirectory + "/Japanese (ja).asset";
        private const string EnglishLocalePath = LocaleDirectory + "/English (en).asset";

        private static readonly string[] ScenePaths =
        {
            "Assets/Minge2026Spring/Scenes/OptionScene.unity",
            "Assets/Minge2026Spring/Scenes/NovelScene.unity",
            "Assets/Minge2026Spring/Scenes/AchievementScene.unity",
            "Assets/Minge2026Spring/Scenes/EndingScene.unity"
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/Minge2026Spring/Prefabs/ChatHeader.prefab"
        };

        private readonly struct EntryDefinition
        {
            public readonly string Key;
            public readonly string Japanese;
            public readonly string English;
            public readonly bool IsSmart;

            public EntryDefinition(string key, string japanese, string english, bool isSmart = false)
            {
                Key = key;
                Japanese = japanese;
                English = english;
                IsSmart = isSmart;
            }
        }

        private readonly struct TextReference
        {
            public readonly string Table;
            public readonly string Entry;

            public TextReference(string table, string entry)
            {
                Table = table;
                Entry = entry;
            }
        }

        private static readonly Dictionary<string, EntryDefinition[]> Tables = new()
        {
            ["Common"] = new[]
            {
                E("back", "もどる", "Back"),
                E("skip", "スキップ", "Skip"),
                E("unknown", "???", "???"),
                E("version", "バージョン {0}", "Version {0}", true)
            },
            ["Title"] = new[]
            {
                E("start", "はじめる", "Start"),
                E("options", "オプション", "Options"),
                E("achievements", "実績", "Achievements"),
                E("quit", "終了", "Quit")
            },
            ["Options"] = new[]
            {
                E("master_volume", "主音量", "Master Volume"),
                E("bgm_volume", "BGM音量", "BGM Volume"),
                E("se_volume", "SE音量", "SFX Volume"),
                E("voice_volume", "ボイス音量", "Voice Volume"),
                E("delete_save", "データを削除", "Delete Save Data"),
                E("delete_warning", "セーブデータを削除します。\nこの操作は取り消せません。\nよろしいですか？",
                    "Delete your save data?\nThis action cannot be undone.\nAre you sure?"),
                E("confirm_delete", "削除する", "Delete"),
                E("cancel", "キャンセル", "Cancel"),
                E("language_current", "言語: {0}", "Language: {0}", true),
                E("language_name_ja", "日本語", "Japanese"),
                E("language_name_en", "英語", "English")
            },
            ["NovelUI"] = new[]
            {
                E("skip", "スキップ", "Skip"),
                E("continue", "つぎにすすむ", "Continue"),
                E("direct_messages", "DMメッセージ", "Direct Messages"),
                E("online", "オンライン", "Online"),
                E("channel.minecraft", "# ミネクラ", "# minecraft"),
                E("channel.read_me", "ReadMe", "Read Me"),
                E("channel.clubroom", "部室", "Clubroom"),
                E("channel.game_recruitment", "# ゲーム募集チャンネル", "# game-recruitment"),
                E("channel.chat", "# 雑談", "# general-chat"),
                E("channel.music_bot", "# 音楽botコマンド", "# music-bot-commands"),
                E("channel.sound_study", "# サウンド勉強会", "# sound-study-group"),
                E("server.game_jam", "Onedayゲームジャム", "One-Day Game Jam"),
                E("channel.general", "# 一般", "# general"),
                E("category.study_groups", "勉強会", "Study Groups"),
                E("channel.programmers", "# プログラマ", "# programmers"),
                E("channel.artist_study", "# グラフィッカ勉強会", "# artist-study-group"),
                E("channel.sound", "# サウンド", "# sound"),
                E("channel.promotion", "# 宣伝等", "# promotion"),
                E("channel.minecraft_server", "# ミネクラサーバー", "# minecraft-server"),
                E("channel.clubroom_availability", "# 部室空き状況", "# clubroom-availability"),
                E("channel.clubroom_booking", "# 部室貸し出し状況", "# clubroom-booking"),
                E("channel.forms", "# フォーム", "# forms"),
                E("channel.requests", "# やっておいてほしいこと", "# requests"),
                E("channel.doodles", "# 落書き", "# doodles"),
                E("channel.artists", "# グラフィッカ", "# artists"),
                E("channel.pixel_art", "# ドットワンドロ", "# pixel-art-hour"),
                E("channel.text_66", "# 66テキスト", "# text-66"),
                E("channel.programmer_study", "# プログラマ勉強会", "# programmer-study-group"),
                E("category.general", "一般", "General"),
                E("channel.announcements", "# アナウンス", "# announcements"),
                E("channel.drawing", "# お絵描き", "# drawing"),
                E("channel.text_65", "# 65テキスト", "# text-65"),
                E("header.title", "#Onedayゲームジャムへようこそ", "Welcome to #One-Day Game Jam"),
                E("header.description",
                    "これはチャンネル「#Onedayゲームジャム」の始まりです。\n\n・仲間とのコミュニケーションを通じて、よりよいゲームを作ろう！！！\n・選択肢が表示されたら、ボタンをクリックして好きな選択肢を選んでね。\n・左下の歯車アイコンからメニュー画面を開けるよ。",
                    "This is the beginning of the #One-Day Game Jam channel.\n\n• Communicate with your teammates and make the best game you can!\n• When choices appear, select the option you want.\n• Open the menu from the gear icon in the bottom-left corner."),
                E("tutorial.dm", "ここを押すと\nDM画面へ！", "Open your\nDirect Messages!"),
                E("tutorial.menu", "ここを押すと\nオプション画面へ！", "Open the\noptions menu!"),
                E("dm.back_to_chat", "← チャットに戻る", "← Back to Chat"),
                E("separator.chapter", "チャプター {0}", "Chapter {0}", true),
                E("separator.choice", "選択肢 {0}", "Choice {0}", true),
                E("menu.title", "メニュー", "Menu"),
                E("menu.return_to_title", "タイトルに戻る", "Return to Title"),
                E("menu.options", "オプション", "Options"),
                E("menu.back", "戻る", "Back"),
                E("audio.title", "オーディオ設定", "Audio Settings"),
                E("audio.description", "ゲーム中のサウンドバランスを調整", "Adjust the in-game audio balance"),
                E("audio.section", "音量", "VOLUME"),
                E("audio.master.title", "全体音量", "Master Volume"),
                E("audio.master.description", "ゲーム全体の音量", "Overall game volume"),
                E("audio.bgm.title", "BGM音量", "Music Volume"),
                E("audio.bgm.description", "音楽と環境音", "Music and ambience"),
                E("audio.se.title", "SE音量", "SFX Volume"),
                E("audio.se.description", "操作音と効果音", "UI and sound effects"),
                E("audio.voice.title", "ボイス音量", "Voice Volume"),
                E("audio.voice.description", "キャラクターの音声", "Character voices"),
                E("audio.auto_save", "変更内容は自動保存されます", "Changes are saved automatically"),
                E("audio.back_to_menu", "メニューへ戻る", "Back to Menu"),
                E("audio.restart", "最初から始める", "Restart from Beginning")
            },
            ["Achievements"] = BuildAchievementEntries(),
            ["Ending"] = BuildEndingEntries()
        };

        private static readonly Dictionary<string, string> OptionBindings = new()
        {
            ["主音量"] = "master_volume",
            ["BGM音量"] = "bgm_volume",
            ["SE音量"] = "se_volume",
            ["ボイス音量"] = "voice_volume",
            ["データを削除"] = "delete_save"
        };

        private static readonly Dictionary<string, string> NovelBindings = Tables["NovelUI"]
            .Where(entry => entry.Key.StartsWith("channel.", StringComparison.Ordinal) ||
                            entry.Key.StartsWith("server.", StringComparison.Ordinal) ||
                            entry.Key.StartsWith("category.", StringComparison.Ordinal) ||
                            entry.Key is "skip" or "continue" or "direct_messages" or "online")
            .GroupBy(entry => entry.Japanese)
            .ToDictionary(group => group.Key, group => group.First().Key);

        [MenuItem("Tools/Minge/Localization/Generate UI Localization")]
        public static void Generate()
        {
            EnsureFolder(RootDirectory);
            EnsureFolder(LocaleDirectory);
            EnsureFolder(TableDirectory);

            var settings = EnsureSettings();
            var japanese = EnsureLocale(JapaneseLocalePath, "ja", "Japanese (日本語)");
            var english = EnsureLocale(EnglishLocalePath, "en", "English");
            ConfigureSettings(settings, japanese);
            GenerateTables(japanese, english);
            BindFixedTexts();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LocalizationProjectGenerator] UI localization generated successfully.");
        }

        private static LocalizationSettings EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Minge Localization Settings";
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            return settings;
        }

        private static Locale EnsureLocale(string path, string code, string displayName)
        {
            var locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                locale.name = displayName;
                AssetDatabase.CreateAsset(locale, path);
            }

            if (LocalizationEditorSettings.GetLocale(locale.Identifier) == null)
                LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void ConfigureSettings(LocalizationSettings settings, Locale japanese)
        {
            var selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new GameSaveLocaleSelector());
            selectors.Add(new SystemLocaleSelector());
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("en") });

            LocalizationSettings.Instance = settings;
            LocalizationSettings.ProjectLocale = japanese;
            LocalizationSettings.InitializeSynchronously = true;
            EditorUtility.SetDirty(settings);
        }

        private static void GenerateTables(Locale japanese, Locale english)
        {
            foreach (var tableDefinition in Tables)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(tableDefinition.Key);
                if (collection == null)
                {
                    collection = LocalizationEditorSettings.CreateStringTableCollection(
                        tableDefinition.Key,
                        TableDirectory,
                        new List<Locale> { japanese, english });
                }

                var japaneseTable = collection.GetTable(japanese.Identifier) as StringTable
                                    ?? collection.AddNewTable(japanese.Identifier) as StringTable;
                var englishTable = collection.GetTable(english.Identifier) as StringTable
                                   ?? collection.AddNewTable(english.Identifier) as StringTable;

                foreach (var definition in tableDefinition.Value)
                {
                    var japaneseEntry = japaneseTable.AddEntry(definition.Key, definition.Japanese);
                    var englishEntry = englishTable.AddEntry(definition.Key, definition.English);
                    japaneseEntry.IsSmart = definition.IsSmart;
                    englishEntry.IsSmart = definition.IsSmart;
                }

                EditorUtility.SetDirty(japaneseTable);
                EditorUtility.SetDirty(englishTable);
                EditorUtility.SetDirty(collection.SharedData);
                LocalizationEditorSettings.SetPreloadTableFlag(japaneseTable, true);
                LocalizationEditorSettings.SetPreloadTableFlag(englishTable, true);
            }
        }

        private static void BindFixedTexts()
        {
            foreach (var path in ScenePaths)
                BindScene(path);
            foreach (var path in PrefabPaths)
                BindPrefab(path);
        }

        private static void BindScene(string path)
        {
            var scene = SceneManager.GetSceneByPath(path);
            var openedForGeneration = !scene.IsValid() || !scene.isLoaded;
            if (openedForGeneration)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (TryGetReference(path, text, out var reference))
                        Bind(text, reference);
                }
            }

            EditorSceneManager.SaveScene(scene);
            if (openedForGeneration)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static void BindPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (TryGetReference(path, text, out var reference))
                        Bind(text, reference);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool TryGetReference(string assetPath, TMP_Text text, out TextReference reference)
        {
            if (text.text == "もどる")
            {
                reference = new TextReference("Common", "back");
                return true;
            }

            if (assetPath.EndsWith("OptionScene.unity", StringComparison.Ordinal))
            {
                if (OptionBindings.TryGetValue(text.text, out var optionKey))
                {
                    reference = new TextReference("Options", optionKey);
                    return true;
                }
            }
            else if (assetPath.EndsWith("NovelScene.unity", StringComparison.Ordinal))
            {
                if (text.gameObject.name == "NameText" && text.text == "れの")
                {
                    reference = new TextReference("Characters", "character.reno");
                    return true;
                }
                if (NovelBindings.TryGetValue(text.text, out var novelKey))
                {
                    reference = new TextReference("NovelUI", novelKey);
                    return true;
                }
            }
            else if (assetPath.EndsWith("EndingScene.unity", StringComparison.Ordinal))
            {
                if (text.text == "早送り＞＞")
                {
                    reference = new TextReference("Ending", "fast_forward");
                    return true;
                }
                if (text.text == "スキップ")
                {
                    reference = new TextReference("Ending", "skip");
                    return true;
                }
            }
            else if (assetPath.EndsWith("ChatHeader.prefab", StringComparison.Ordinal))
            {
                if (text.gameObject.name == "HeaderTitleText")
                {
                    reference = new TextReference("NovelUI", "header.title");
                    return true;
                }
                if (text.gameObject.name == "TutorialDescText")
                {
                    reference = new TextReference("NovelUI", "header.description");
                    return true;
                }
            }

            reference = default;
            return false;
        }

        private static void Bind(TMP_Text target, TextReference reference)
        {
            var localizer = target.GetComponent<LocalizeStringEvent>();
            if (localizer == null)
                localizer = target.gameObject.AddComponent<LocalizeStringEvent>();

            for (var index = localizer.OnUpdateString.GetPersistentEventCount() - 1; index >= 0; index--)
                UnityEventTools.RemovePersistentListener(localizer.OnUpdateString, index);

            localizer.StringReference.SetReference(reference.Table, reference.Entry);
            var setter = target.GetType().GetProperty(nameof(TMP_Text.text))?.GetSetMethod();
            if (setter == null)
                throw new InvalidOperationException($"TMP text setter was not found: {target.name}");

            var listener = Delegate.CreateDelegate(typeof(UnityAction<string>), target, setter) as UnityAction<string>;
            UnityEventTools.AddPersistentListener(localizer.OnUpdateString, listener);
            localizer.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            EditorUtility.SetDirty(target.gameObject);
            EditorUtility.SetDirty(localizer);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = path[..path.LastIndexOf('/')];
            var name = path[(path.LastIndexOf('/') + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static EntryDefinition E(string key, string japanese, string english, bool isSmart = false) =>
            new(key, japanese, english, isSmart);

        private static EntryDefinition[] BuildAchievementEntries()
        {
            var titles = new[]
            {
                "Pass and Delivery", "Passive and Drift", "Plain and Dry", "Power and Disaster",
                "Pause and Delete", "Panic and Deadlock", "Passion and Discord", "Perfect and Delight",
                "Pride and Determination", "Poor and Defect", "Planning and Development"
            };
            var japaneseDescriptions = new[]
            {
                "４人のうち３人のやる気度が高い", "プログラマ２人のやる気度が低い",
                "サウンド、グラフィッカのやる気度が低い", "ごっとのやる気度が高い",
                "途中でゲームをやめる", "ゲーム中に詰みセーブが発生した",
                "ごっと以外の３人のやる気度が高い", "4人のやる気度が高い",
                "一度も死なずにゲームクリア", "全員のやる気度が低い",
                "完全クリア\nおめでとう！！！！"
            };
            var englishDescriptions = new[]
            {
                "Three of the four members have high motivation.",
                "Both programmers have low motivation.",
                "The sound designer and artist have low motivation.",
                "Got has high motivation.",
                "Quit the game partway through.",
                "A softlock save occurred during the game.",
                "All three members except Got have high motivation.",
                "All four members have high motivation.",
                "Clear the game without dying once.",
                "Everyone has low motivation.",
                "100% Complete\nCongratulations!!!!"
            };

            var entries = new List<EntryDefinition>();
            for (var index = 0; index < titles.Length; index++)
            {
                entries.Add(E($"ending.{index}.title", titles[index], titles[index]));
                entries.Add(E($"ending.{index}.description", japaneseDescriptions[index], englishDescriptions[index]));
            }

            entries.Add(E("ending_statistics", "総プレイ時間  {0}\nデス数            {1:N0} 回",
                "Total Play Time  {0}\nDeaths           {1:N0}", true));
            entries.Add(E("total_play_time", "総プレイ時間: {0}", "Total Play Time: {0}", true));
            entries.Add(E("total_deaths", "総デス数: {0:N0} 回", "Total Deaths: {0:N0}", true));
            entries.Add(E("extra_statistics", "総プレイ時間  {0}\nデス数            {1:N0} 回\nルーム名          {2}",
                "Total Play Time  {0}\nDeaths           {1:N0}\nRoom             {2}", true));
            entries.Add(E("locked_title", "{0}. ？？？", "{0}. ???", true));
            entries.Add(E("extra_stage", "EXTRA\nSTAGE", "EXTRA\nSTAGE"));
            entries.Add(E("hidden.needle", "針を壊せるようになる！", "You can now break needles!"));
            entries.Add(E("hidden.warp", "ワープができるようになる！", "You can now warp!"));
            return entries.ToArray();
        }

        private static EntryDefinition[] BuildEndingEntries()
        {
            var entries = new List<EntryDefinition>
            {
                E("fast_forward", "早送り＞＞", "FAST FORWARD >>"),
                E("skip", "スキップ", "SKIP"),
                E("death_count", "デス数: {0:N0} 回", "Deaths: {0:N0}", true)
            };

            var japanese = new[]
            {
                "ここまで全部見たんだね。", "ありがとう。", "君は知っているかもしれないけど、",
                "開発には正解なんてない。", "誰が悪いわけでもない。", "指示に最適解もない。",
                "実装方法の", "世界観の", "答えはひとつじゃない。", "それに", "正しさを持つのは、",
                "君だけじゃない。", "……", "これ以上は語らなくてもいいかな。",
                "ここまで遊んでくれた君なら、", "きっと良い企画開発者になれる。", "最後に、",
                "ここまで遊んでくれてありがとう。", "Thank you for Playing"
            };
            var english = new[]
            {
                "You've seen everything there is to see.", "Thank you.", "You may already know this, but...",
                "There is no single right answer in game development.", "No one person is to blame.",
                "There is no perfect way to give directions.", "When it comes to implementation...",
                "...or worldbuilding...", "...there is more than one answer.", "And...", "the right answer...",
                "doesn't belong to you alone.", "...", "I think that's all I need to say.",
                "If you've played this far...", "I'm sure you'll become a great game planner.", "And finally...",
                "Thank you for playing all the way to the end.", "Thank you for playing."
            };
            for (var index = 0; index < japanese.Length; index++)
                entries.Add(E($"extra_dialogue.{index}", japanese[index], english[index]));
            return entries.ToArray();
        }
    }
}
