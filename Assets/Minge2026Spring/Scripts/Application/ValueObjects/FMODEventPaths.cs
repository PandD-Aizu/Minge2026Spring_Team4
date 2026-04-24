// THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
// Generated at: 2026-04-25 06:00:00

using FMODUnity;

namespace Minge2026Spring.Scripts.Application.ValueObjects
{
    public readonly struct FMODEventPath
    {
        public EventReference Reference { get; }
        private FMODEventPath(string path) => Reference = RuntimeManager.PathToEventReference(path);

        public static readonly FMODEventPath BGM_HALFWAY = new ("event:/BGM/Halfway");
        public static readonly FMODEventPath SE_NOTIFICATION_SE = new ("event:/SE/NotificationSE");
        public static readonly FMODEventPath BGM_ENDING_BGM = new ("event:/BGM/EndingBGM");
        public static readonly FMODEventPath BGM_ACHIEVEMENT_BGM = new ("event:/BGM/AchievementBGM");
        public static readonly FMODEventPath BGM_TITLE_BGM = new ("event:/BGM/TitleBGM");
        public static readonly FMODEventPath SE_CLICK_SE = new ("event:/SE/ClickSE");
        public static readonly FMODEventPath BGM_OPTION_BGM = new ("event:/BGM/OptionBGM");
        public static readonly FMODEventPath BGM_NOVEL_SCENE_BGM = new ("event:/BGM/NovelSceneBGM");
    }
}
