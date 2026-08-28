using System;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Minge2026Spring.Scripts.Infrastructure.Localization
{
    [Serializable]
    [UnityEngine.Localization.DisplayName("Game Save Locale Selector")]
    public sealed class GameSaveLocaleSelector : IStartupLocaleSelector
    {
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            var languageType = new GameSaveRepository().Load()?.languageType;
            if (string.IsNullOrWhiteSpace(languageType))
                return null;

            return availableLocales.GetLocale(new LocaleIdentifier(languageType));
        }
    }
}
