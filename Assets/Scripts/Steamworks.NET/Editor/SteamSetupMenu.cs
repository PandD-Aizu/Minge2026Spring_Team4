using System.IO;
using UnityEditor;
using UnityEngine;

public static class SteamSetupMenu
{
    [MenuItem("Tools/Steam/Show Cloud Save Paths")]
    private static void ShowCloudSavePaths()
    {
        Debug.Log($"[Steam Auto-Cloud] Persistent root: {Application.persistentDataPath}\n" +
                  "Patterns (one row each): GameSave.json, InternalParameters.json, FMODSettings.json\n" +
                  $"External morale file: {Path.Combine(Application.streamingAssetsPath, "I_gonna_be_the_tresure_hunter/CharactersMoraleValue.json")}\n" +
                  "Use the standalone build's <ExecutableName>_Data/StreamingAssets path for the external morale file.\n" +
                  "See Documentation/SteamSetup.md for Steamworks configuration and testing.");
    }
}
