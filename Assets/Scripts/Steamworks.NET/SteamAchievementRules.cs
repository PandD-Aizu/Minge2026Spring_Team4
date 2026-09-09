using System;
using System.Collections.Generic;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.UseCase;

public static class SteamAchievementRules
{
    public static IEnumerable<string> GetUnlockedIds(GameSaveData save)
    {
        if (save == null) yield break;
        var endings = new HashSet<string>(save.reachedEndingIds ?? Array.Empty<string>());
        for (var index = 0; index < AchievementUseCase.EndingCount; index++)
            if (endings.Contains(AchievementUseCase.GetEndingId(index)))
                yield return $"ACH_ENDING_{(char)('A' + index)}";

        if (save.GetItem1) yield return "ACH_HIDDEN_ITEM_1";
        if (save.GetItem2) yield return "ACH_HIDDEN_ITEM_2";
    }
}
