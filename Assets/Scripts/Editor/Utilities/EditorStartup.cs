// EditorStartup: when the Unity editor opens on an empty "Untitled" scene, it automatically opens
// the Main Menu scene so the game shows up in the Hierarchy straight away. Also adds
// "The Last Torch > Open Scene > Main Menu / Temple" to jump between the two scenes.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastTorch.EditorTools
{
    [InitializeOnLoad]
    public static class EditorStartup
    {
        const string SessionKey = "LastTorch.StartupSceneChecked";

        static EditorStartup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += OpenMainMenuIfEmpty;
        }

        static void OpenMainMenuIfEmpty()
        {
            // Only once per editor session (not after every script recompile)
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var active = EditorSceneManager.GetActiveScene();
            bool emptyUntitled = string.IsNullOrEmpty(active.path) && !active.isDirty;
            if (emptyUntitled && File.Exists(SceneBuilder.MainMenuPath))
                EditorSceneManager.OpenScene(SceneBuilder.MainMenuPath);
        }

        [MenuItem("The Last Torch/Open Scene/Main Menu", priority = 20)]
        static void OpenMainMenu() => Open(SceneBuilder.MainMenuPath);

        [MenuItem("The Last Torch/Open Scene/Temple (the level)", priority = 21)]
        static void OpenTemple() => Open(SceneBuilder.TemplePath);

        static void Open(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("The Last Torch", $"{path} has not been built yet.\nRun 'The Last Torch > Build All (1 + 2)'.", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }
    }
}
