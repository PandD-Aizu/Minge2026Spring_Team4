using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Minge2026Spring.Scripts.Editor
{
    public sealed class VersionBuildProcessor : IPreprocessBuildWithReport
    {
        private static readonly Regex TrailingNumber = new Regex(@"^(.*?)(\d+)$");

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var currentVersion = PlayerSettings.bundleVersion.Trim();
            var match = TrailingNumber.Match(currentVersion);
            var nextVersion = match.Success
                ? $"{match.Groups[1].Value}{long.Parse(match.Groups[2].Value) + 1}"
                : $"{currentVersion}.1";

            PlayerSettings.bundleVersion = nextVersion;
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[VersionBuildProcessor] Version incremented: {currentVersion} -> {nextVersion}");
        }
    }
}
