using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Builds a development APK of the scenes in Build Settings to Builds/MemeHunter.apk.
    /// Batch: Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod MemeHunter.Editor.AndroidBuild.Run
    /// Then: adb install -r Builds/MemeHunter.apk
    /// </summary>
    public static class AndroidBuild
    {
        const string OutputPath = "Builds/MemeHunter.apk";

        [MenuItem("Meme Hunter/Build Android APK")]
        public static void Run()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[AndroidBuild] {s.result}: {s.outputPath} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime})");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
