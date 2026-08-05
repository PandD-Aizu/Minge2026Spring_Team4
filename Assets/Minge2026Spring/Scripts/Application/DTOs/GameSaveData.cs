using System;

namespace Minge2026Spring.Scripts.Application.DTOs
{
    [Serializable]
    public class GameSaveData
    {
        public string chapterId;
        public string currentBlockId;
        public string[] reachedBlockIds = Array.Empty<string>();
        public string[] reachedEndingIds = Array.Empty<string>();
        public int endingClearCount;
        public EndingMetricMap eachEndingClearTime = new();
        public EndingMetricMap eachEndingDeathCount = new();
    }

    [Serializable]
    public class EndingMetricMap
    {
        public int Ending_A;
        public int Ending_B;
        public int Ending_C;
        public int Ending_D;
        public int Ending_E;
        public int Ending_F;
        public int Ending_G;
        public int Ending_H;
        public int Ending_I;
        public int Ending_J;
        public int Ending_K;

        public int GetValue(string endingId) => endingId switch
        {
            "Ending_A" => Ending_A,
            "Ending_B" => Ending_B,
            "Ending_C" => Ending_C,
            "Ending_D" => Ending_D,
            "Ending_E" => Ending_E,
            "Ending_F" => Ending_F,
            "Ending_G" => Ending_G,
            "Ending_H" => Ending_H,
            "Ending_I" => Ending_I,
            "Ending_J" => Ending_J,
            "Ending_K" => Ending_K,
            _ => throw new ArgumentOutOfRangeException(nameof(endingId), endingId, null)
        };
    }
}
