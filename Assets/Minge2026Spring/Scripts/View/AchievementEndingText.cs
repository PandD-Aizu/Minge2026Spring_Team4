using System;
using System.Text;
using UnityEngine;

namespace Minge2026Spring.Scripts.View
{
    public static class AchievementEndingText
    {
        public const int EndingCount = 11;
        public const int RainbowEndingIndex = 10;

        private static readonly string[] Titles =
        {
            "Pass and Delivery",
            "Passive and Drift",
            "Plain and Dry",
            "Power and Disaster",
            "Pause and Delete",
            "Panic and Deadlock",
            "Passion and Discord",
            "Perfect and Delight",
            "Pride and Determination",
            "Poor and Defect",
            "Planning and Development"
        };

        private static readonly string[] Descriptions =
        {
            "４人のうち３人のやる気度が高い",
            "プログラマ２人のやる気度が低い",
            "サウンド、グラフィッカのやる気度が低い",
            "Gotのやる気度が高い",
            "途中でゲームをやめる",
            "ゲーム中に詰みセーブが発生した",
            "ごっと以外の３人のやる気度が高い",
            "4人のやる気度が高い",
            "一度も死なずにゲームクリア",
            "全員のやる気度が低い",
            "完全クリア\nおめでとう！！！！"
        };

        public static string GetTitle(int index) => Titles[ValidateIndex(index)];

        public static string GetDescription(int index) => Descriptions[ValidateIndex(index)];

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
