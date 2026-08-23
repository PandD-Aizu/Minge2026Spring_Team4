using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Minge2026Spring.Scripts.View
{
    /// <summary>
    /// UI向けLocalization参照の共通窓口。
    /// 現在言語はUnity LocalizationのSelectedLocaleを唯一の情報源とする。
    /// </summary>
    public static class UILocalization
    {
        public const string LocalePreferenceKey = "minge.selected-locale";
        public const string JapaneseLocaleCode = "ja";
        public const string EnglishLocaleCode = "en";

        public static string CurrentLocaleCode =>
            LocalizationSettings.SelectedLocale?.Identifier.Code ?? JapaneseLocaleCode;

        public static string Get(string table, string entry, params object[] arguments)
        {
            var value = LocalizationSettings.StringDatabase.GetLocalizedString(table, entry, arguments: arguments);
            if (!string.IsNullOrEmpty(value))
                return value;

            Debug.LogError($"[UILocalization] Missing localized string: {table}/{entry}");
            return $"[{table}/{entry}]";
        }

        public static void SelectLocale(string localeCode)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
                throw new InvalidOperationException($"Locale is not registered: {localeCode}");

            PlayerPrefs.SetString(LocalePreferenceKey, locale.Identifier.Code);
            PlayerPrefs.Save();
            LocalizationSettings.SelectedLocale = locale;
        }

        public static RuntimeLocalizedText Bind(TMP_Text text, string table, string entry, params object[] arguments)
        {
            var localizer = text.GetComponent<RuntimeLocalizedText>();
            if (localizer == null)
                localizer = text.gameObject.AddComponent<RuntimeLocalizedText>();
            localizer.SetReference(text, table, entry, arguments);
            return localizer;
        }
    }

    /// <summary>
    /// 実行時に生成されるTMPへLocalizedStringを結び付ける。
    /// LocalizedString自身がSelectedLocaleChangedを監視するため、表示済みUIも即時更新される。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RuntimeLocalizedText : MonoBehaviour
    {
        private TMP_Text _target;
        private LocalizedString _localizedString;
        private bool _subscribed;

        public void SetReference(TMP_Text target, string table, string entry, params object[] arguments)
        {
            Unsubscribe();
            _target = target;
            _localizedString = new LocalizedString(table, entry)
            {
                Arguments = arguments
            };
            Subscribe();
        }

        public void SetArguments(params object[] arguments)
        {
            if (_localizedString == null)
                return;

            _localizedString.Arguments = arguments;
            _localizedString.RefreshString();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _target == null || _localizedString == null)
                return;

            _localizedString.StringChanged += Apply;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _localizedString == null)
                return;

            _localizedString.StringChanged -= Apply;
            _subscribed = false;
        }

        private void Apply(string value)
        {
            if (_target != null)
                _target.text = value;
        }
    }
}
