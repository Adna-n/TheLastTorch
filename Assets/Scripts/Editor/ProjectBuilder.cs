// ProjectBuilder: the "The Last Torch" menu in the Unity top bar.
// 1. Generate Assets = textures -> meshes -> audio -> materials -> post-FX -> prefabs.
// 2. Build Scenes = MainMenu + Temple (from maze.txt) and the Build Settings list.
// Build All = both. Reset Saved Profiles = delete the LT_ PlayerPrefs keys.
// BuildAllBatch() is the same as Build All for the command line (-executeMethod).
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastTorch.EditorTools
{
    public static class ProjectBuilder
    {
        [MenuItem("The Last Torch/1. Generate Assets", priority = 1)]
        public static void GenerateAssets()
        {
            try
            {
                GameSettingsAsset.GetOrCreate();
                Step("Textures", 0.05f); TextureGenerator.GenerateAll();
                Step("Meshes", 0.35f); MeshGenerator.GenerateAll();
                Step("Audio", 0.45f); AudioGenerator.GenerateAll();
                Step("Materials", 0.7f); MaterialFactory.GenerateAll();
                Step("Post-processing", 0.8f); PostFXBuilder.GenerateAll();
                Step("Prefabs", 0.85f); PrefabFactory.GenerateAll();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[ProjectBuilder] Assets generated in Assets/Generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("The Last Torch/2. Build Scenes", priority = 2)]
        public static void BuildScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Step("Main Menu", 0.2f); SceneBuilder.BuildMainMenu();
                Step("Temple", 0.5f); SceneBuilder.BuildTemple();
                SceneBuilder.SetBuildSettings(SceneBuilder.MainMenuPath, SceneBuilder.TemplePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[ProjectBuilder] Scenes built. Build Settings: MainMenu (0), Temple (1).");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(SceneBuilder.MainMenuPath);
        }

        [MenuItem("The Last Torch/Build All (1 + 2)", priority = 3)]
        public static void BuildAll()
        {
            GenerateAssets();
            BuildScenes();
        }

        [MenuItem("The Last Torch/Reset Saved Profiles", priority = 50)]
        public static void ResetProfiles()
        {
            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Reset Saved Profiles", "Delete all profiles, the leaderboard and saved options?", "Delete", "Cancel"))
                return;
            ProfileManager.ResetAll();
            Debug.Log("[ProjectBuilder] Saved profiles, leaderboard and options deleted.");
        }

        /// <summary>Command line: Unity.exe -batchmode -quit -executeMethod LastTorch.EditorTools.ProjectBuilder.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            try
            {
                BuildAll();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void Step(string label, float progress)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("The Last Torch", label + "...", progress);
            Debug.Log("[ProjectBuilder] " + label);
        }
    }
}
