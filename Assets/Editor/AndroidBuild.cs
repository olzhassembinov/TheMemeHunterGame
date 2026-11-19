using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Builds an APK of the scenes in Build Settings.
    /// Meme Hunter / Build Android APK: a development build (Builds/MemeHunter.apk), for our own tests.
    /// Meme Hunter / Build Android APK (beta release): a normal build (Builds/MemeHunter-beta.apk), for the testers.
    /// Batch: Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod MemeHunter.Editor.AndroidBuild.Run
    /// Then: adb install -r Builds/MemeHunter.apk
    /// </summary>
    public static class AndroidBuild
    {
        const string DevelopmentPath = "Builds/MemeHunter.apk";
        const string ReleasePath = "Builds/MemeHunter-beta.apk";

        [MenuItem("Meme Hunter/Build Android APK")]
        public static void Run() => Build(development: true, DevelopmentPath);

        [MenuItem("Meme Hunter/Build Android APK (beta release)")]
        public static void RunRelease() => Build(development: false, ReleasePath);

        static void Build(bool development, string outputPath)
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[AndroidBuild] {summary.result}: {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime})");
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
