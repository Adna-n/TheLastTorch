// GameSettingsAsset: makes sure Assets/Settings/GameSettings.asset exists (never overwrites your
// edited values) and adds the menu item "The Last Torch > Select Game Settings" to jump to it.
using UnityEditor;
using UnityEngine;

namespace LastTorch.EditorTools
{
    public static class GameSettingsAsset
    {
        public const string AssetPath = "Assets/Settings/GameSettings.asset";

        public static GameSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(AssetPath);
            if (settings != null) return settings;

            EditorUtil.EnsureFolder("Assets/Settings");
            settings = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameSettings] Created " + AssetPath);
            return settings;
        }

        [MenuItem("The Last Torch/Select Game Settings", priority = 100)]
        static void SelectSettings()
        {
            var settings = GetOrCreate();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
