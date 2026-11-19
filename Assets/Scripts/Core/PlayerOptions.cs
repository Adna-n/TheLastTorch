// PlayerOptions: the player's personal options from the Settings menu (volumes, mouse sensitivity).
// Saved as JSON in PlayerPrefs under "LT_Settings" so they persist between runs.
// Mouse sensitivity here is a multiplier on GameSettings.mouseSensitivity.
using System;
using UnityEngine;

namespace LastTorch
{
    [Serializable]
    public class PlayerOptions
    {
        public const string PrefsKey = "LT_Settings";

        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Tooltip("Multiplier on GameSettings.mouseSensitivity.")]
        public float mouseSensitivity = 1f;
        [Tooltip("Difficulty chosen last time in the main menu.")]
        public Difficulty lastDifficulty = Difficulty.Normal;

        static PlayerOptions current;

        public static PlayerOptions Current
        {
            get
            {
                if (current == null) Load();
                return current;
            }
        }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            current = string.IsNullOrEmpty(json) ? new PlayerOptions() : JsonUtility.FromJson<PlayerOptions>(json);
            if (current == null) current = new PlayerOptions();
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>Pushes the master volume to Unity's global listener.</summary>
        public static void Apply()
        {
            AudioListener.volume = Mathf.Clamp01(Current.masterVolume);
        }
    }
}
