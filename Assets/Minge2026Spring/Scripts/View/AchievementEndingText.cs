using System;
using System.Text;
using UnityEngine;

namespace Minge2026Spring.Scripts.View
{
    public static class AchievementEndingText
    {
        public const int EndingCount = 11;
        public const int RainbowEndingIndex = 10;

        public static string GetTitle(int index) =>
            UILocalization.Get("Achievements", $"ending.{ValidateIndex(index)}.title");

        public static string GetDescription(int index) =>
            UILocalization.Get("Achievements", $"ending.{ValidateIndex(index)}.description");

        public static string FormatTitle(int index)
        {
            var title = GetTitle(index);
            if (index == RainbowEndingIndex)
                return title;

            var color = Color.HSVToRGB(index / 10f, 0.8f, 1f);
            var colorHex = ColorUtility.ToHtmlStringRGB(color);
            var builder = new StringBuilder(title.Length + 64);
            foreach (var character in title)
            {
                if (character is 'P' or 'D')
                    builder.Append("<size=115%><b><color=#").Append(colorHex).Append('>')
                        .Append(character).Append("</color></b></size>");
                else
                    builder.Append(character);
            }

            return builder.ToString();
        }

        private static int ValidateIndex(int index)
        {
            if (index < 0 || index >= EndingCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return index;
        }
    }
}
