using System;
using System.Collections.Generic;
using System.Linq;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using R3;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class AchievementUseCase
    {
        public const int EndingCount = 11;
        public const int ExtraStageEndingIndex = 10;
        private const int BaseEndingCount = 10;
        private readonly IGameSaveRepository _gameSaveRepository;
        private readonly ITmpMoraleJsonExporter _moraleJsonExporter;
        private readonly IHiddenItemStatusRepository _hiddenItemStatusRepository;
        private readonly ProcessUseCase _processUseCase;
        private readonly EndingUseCase _endingUseCase;

        public ReadOnlyReactiveProperty<bool> IsExtraStageRunning => _processUseCase.IsProcessRunning;

        public AchievementUseCase(
            IGameSaveRepository gameSaveRepository,
            ITmpMoraleJsonExporter moraleJsonExporter,
            IHiddenItemStatusRepository hiddenItemStatusRepository,
            ProcessUseCase processUseCase,
            EndingUseCase endingUseCase)
        {
            _gameSaveRepository = gameSaveRepository;
            _moraleJsonExporter = moraleJsonExporter;
            _hiddenItemStatusRepository = hiddenItemStatusRepository;
            _processUseCase = processUseCase;
            _endingUseCase = endingUseCase;
        }

        public HashSet<string> GetReachedEndingIds()
        {
            var saveData = _gameSaveRepository.Load();
            return new HashSet<string>(saveData?.reachedEndingIds ?? Array.Empty<string>());
        }

        public int GetEndingClearCount() => Math.Max(0, _gameSaveRepository.Load()?.endingClearCount ?? 0);

        public HiddenItemStatus GetHiddenItemStatus() => _hiddenItemStatusRepository.LoadHiddenItemStatus();

        public EndingPlayStatistics[] GetEndingPlayStatistics()
        {
            var saveData = _gameSaveRepository.Load();
            var clearTimes = saveData?.eachEndingClearTime;
            var deathCounts = saveData?.eachEndingDeathCount;
            var statistics = new EndingPlayStatistics[EndingCount];

            for (var index = 0; index < EndingCount; index++)
            {
                var endingId = GetEndingId(index);
                statistics[index] = new EndingPlayStatistics(
                    clearTimes?.GetValue(endingId) ?? 0,
                    deathCounts?.GetValue(endingId) ?? 0);
            }

            return statistics;
        }

        public bool IsExtraStageUnlocked(HashSet<string> reachedEndingIds)
        {
            for (var index = 0; index < BaseEndingCount; index++)
                if (!reachedEndingIds.Contains(GetEndingId(index))) return false;
            return true;
        }

        public void StartExtraStage()
        {
            const int morale = 101;
            _moraleJsonExporter.Export(new MoraleDto
            {
                MoraleMap = new Dictionary<string, int>
                {
                    [CharacterMoraleKeys.CharacterA] = morale,
                    [CharacterMoraleKeys.CharacterB] = morale,
                    [CharacterMoraleKeys.CharacterC] = morale,
                    [CharacterMoraleKeys.CharacterD] = morale,
                }
            });
            _processUseCase.StartProcess("I_gonna_be_the_tresure_hunter/I_wanna_Siv3D.exe");
        }

        public void CheckExtraStageProcessIsRunning() => _processUseCase.CheckProcessIsRunning();

        public bool TryRecordExtraStageClear()
        {
            var extraStageEndingId = GetEndingId(ExtraStageEndingIndex);
            if (_endingUseCase.GetEndingBlockId() != extraStageEndingId)
                return false;

            var saveData = _gameSaveRepository.Load() ?? new GameSaveData();
            var endingIds = (saveData.reachedEndingIds ?? Array.Empty<string>()).ToList();
            if (!endingIds.Contains(extraStageEndingId))
            {
                endingIds.Add(extraStageEndingId);
                saveData.reachedEndingIds = endingIds.ToArray();
                _gameSaveRepository.Save(saveData);
            }

            return true;
        }

        public static string GetEndingId(int index)
        {
            if (index < 0 || index >= EndingCount) throw new ArgumentOutOfRangeException(nameof(index));
            return $"Ending_{(char)('A' + index)}";
        }
    }

    public readonly struct EndingPlayStatistics
    {
        public int ClearTimeSeconds { get; }
        public int DeathCount { get; }

        public EndingPlayStatistics(int clearTimeSeconds, int deathCount)
        {
            ClearTimeSeconds = Math.Max(0, clearTimeSeconds);
            DeathCount = Math.Max(0, deathCount);
        }
    }
}
