using Minge2026Spring.Scripts.Application.Interface;
using UnityEngine;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class EndingUseCase
    {
        private const int EndingCount = 10;
        private const int DefaultEndingValue = 0;
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
            // 読込失敗時はEnding_Aへフォールバックする
            if (!_endingValueProvider.TryGetEndingValue(out var endingValue))
                endingValue = DefaultEndingValue;

            // 範囲外の値はログへ記録してEnding_Aへフォールバックする
            if (endingValue < 0 || endingValue >= EndingCount)
            {
                Debug.LogError($"[EndingUseCase] Ending value is out of range: {endingValue}");
                endingValue = DefaultEndingValue;
            }

            return $"Ending_{(char)('A' + endingValue)}";
        }
    }
}
