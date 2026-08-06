using Minge2026Spring.Scripts.Application.DTOs;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class MoraleEndingSelector
    {
        public int GetEndingIndex(MoraleDto moraleDto)
        {
            var value1 = GetMorale(moraleDto, CharacterMoraleKeys.CharacterA);
            var value2 = GetMorale(moraleDto, CharacterMoraleKeys.CharacterB);
            var value3 = GetMorale(moraleDto, CharacterMoraleKeys.CharacterC);
            var value4 = GetMorale(moraleDto, CharacterMoraleKeys.CharacterD);

            // The order is significant: more specific ending conditions take priority.
            if (value1 < 30 && value2 < 30 && value3 < 30 && value4 < 30) return 9; // J
            if (value2 >= 90 && value3 >= 90 && value4 >= 90) return 6; // G
            if (value1 >= 90) return 3; // D
            if (value1 is >= 30 and < 90 && value2 is >= 30 and < 90 && value3 >= 30 && value4 >= 30) return 7; // H
            if (value1 < 30 && value2 < 30) return 1; // B
            if (value3 < 30 && value4 < 30) return 2; // C
            return 0; // A
        }

        private static int GetMorale(MoraleDto moraleDto, string characterId)
        {
            return moraleDto?.MoraleMap != null && moraleDto.MoraleMap.TryGetValue(characterId, out var morale)
                ? morale
                : 0;
        }
    }
}
