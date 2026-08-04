using System;
using System.Collections.Generic;
using System.Linq;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class AchievementUseCase
    {
        public const int EndingCount = 10;
        private readonly IGameSaveRepository _gameSaveRepository;
        private readonly ITmpMoraleJsonExporter _moraleJsonExporter;
        private readonly ProcessUseCase _processUseCase;

        public AchievementUseCase(IGameSaveRepository gameSaveRepository, ITmpMoraleJsonExporter moraleJsonExporter, ProcessUseCase processUseCase)
        {
            _gameSaveRepository = gameSaveRepository;
            _moraleJsonExporter = moraleJsonExporter;
            _processUseCase = processUseCase;
        }

        public HashSet<string> GetReachedEndingIds()
        {
            var saveData = _gameSaveRepository.Load();
            return new HashSet<string>(saveData?.reachedEndingIds ?? Array.Empty<string>());
        }

        public bool AreAllEndingsReached(HashSet<string> reachedEndingIds)
        {
            for (var index = 0; index < EndingCount; index++)
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

        public static string GetEndingId(int index)
        {
            if (index < 0 || index >= EndingCount) throw new ArgumentOutOfRangeException(nameof(index));
            return $"Ending_{(char)('A' + index)}";
        }
    }
}
