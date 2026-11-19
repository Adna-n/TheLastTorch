// BuildScript: builds the playable Windows game to Builds/Windows/TheLastTorch.exe using the scenes in
// Build Settings (MainMenu first). Menu "The Last Torch > 3. Build Windows EXE", or from the command
// line with -executeMethod LastTorch.EditorTools.BuildScript.BuildWindowsBatch.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastTorch.EditorTools
{
    public static class BuildScript
    {
        public const string ExePath = "Builds/Windows/TheLastTorch.exe";

        [MenuItem("The Last Torch/3. Build Windows EXE", priority = 4)]
        public static void BuildWindowsMenu()
        {
            var result = BuildWindows();
            if (!Application.isBatchMode && result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(ExePath);
        }

        public static BuildResult BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                ProjectBuilder.BuildScenes();
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            }

            PlayerSettings.productName = "The Last Torch";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[BuildScript] {summary.result}: {ExePath}  ({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime.TotalSeconds:0}s)");
            return summary.result;
        }

        public static void BuildWindowsBatch()
        {
            try
            {
                if (BuildWindows() != BuildResult.Succeeded) EditorApplication.Exit(1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
