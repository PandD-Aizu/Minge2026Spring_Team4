using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class EndingUseCase
    {
        private const int EndingCount = 10;
        private const int DefaultEndingValue = 0;
        private static readonly string[] EndingTitles =
        {
            "Pass and Delivery", "Passive and Drift", "Plain and Dry", "Power and Disaster", "Pause and Delete",
            "Panic and Deadlock", "Passion and Discord", "Perfect and Delight", "Pride and Determination", "Planning and Development"
        };
        private readonly IEndingValueProvider _endingValueProvider;

        public EndingUseCase(IEndingValueProvider endingValueProvider)
        {
            _endingValueProvider = endingValueProvider;
        }

        /// <summary>
        /// 出力値に対応するエンディングブロックIDを取得する
        /// </summary>
        /// <returns>Ending_AからEnding_JまでのブロックID</returns>
        public string GetEndingBlockId()
        {
            var endingValue = GetEndingValue();
            return $"Ending_{(char)('A' + endingValue)}";
        }

        public string GetEndingTitle() => EndingTitles[GetEndingValue()];

        private int GetEndingValue()
        {
            if (!_endingValueProvider.TryGetEndingValue(out var endingValue))
                return DefaultEndingValue;
            if (endingValue >= 0 && endingValue < EndingCount)
                return endingValue;

            Debug.LogError($"[EndingUseCase] Ending value is out of range: {endingValue}");
            return DefaultEndingValue;
        }
    }
}
