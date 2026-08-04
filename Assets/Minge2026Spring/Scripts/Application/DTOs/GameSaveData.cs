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
    }
}
