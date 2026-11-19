// ProfileManager: saves player profiles and the top-5 leaderboard in PlayerPrefs as JSON
// (keys LT_Profiles, LT_Leaderboard, LT_LastName). StartRun() counts an attempt when a level loads;
// RecordRun() updates best score / best escape time / wins / gems and the leaderboard when a run ends.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTorch
{
    public static class ProfileManager
    {
        public const string ProfilesKey = "LT_Profiles";
        public const string LeaderboardKey = "LT_Leaderboard";
        public const string LastNameKey = "LT_LastName";

        [Serializable] class ProfileList { public List<PlayerProfile> items = new List<PlayerProfile>(); }
        [Serializable] class EntryList { public List<LeaderboardEntry> items = new List<LeaderboardEntry>(); }

        static ProfileList profiles;
        static EntryList leaderboard;

        /// <summary>True if the last recorded run beat that player's previous best.</summary>
        public static bool LastRunWasHighScore { get; private set; }

        public static string LastName
        {
            get => PlayerPrefs.GetString(LastNameKey, string.Empty);
            set { PlayerPrefs.SetString(LastNameKey, value); PlayerPrefs.Save(); }
        }

        public static IReadOnlyList<PlayerProfile> Profiles { get { EnsureLoaded(); return profiles.items; } }
        public static IReadOnlyList<LeaderboardEntry> Leaderboard { get { EnsureLoaded(); return leaderboard.items; } }

        static void EnsureLoaded()
        {
            if (profiles == null) profiles = Read<ProfileList>(ProfilesKey) ?? new ProfileList();
            if (leaderboard == null) leaderboard = Read<EntryList>(LeaderboardKey) ?? new EntryList();
        }

        static T Read<T>(string key) where T : class
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (Exception e) { Debug.LogWarning($"[ProfileManager] Could not read {key}: {e.Message}"); return null; }
        }

        static void Save()
        {
            PlayerPrefs.SetString(ProfilesKey, JsonUtility.ToJson(profiles));
            PlayerPrefs.SetString(LeaderboardKey, JsonUtility.ToJson(leaderboard));
            PlayerPrefs.Save();
        }

        /// <summary>Finds a profile by name (not case sensitive), or null.</summary>
        public static PlayerProfile Find(string name)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(name)) return null;
            string key = name.Trim();
            return profiles.items.Find(p => string.Equals(p.name, key, StringComparison.OrdinalIgnoreCase));
        }

        public static PlayerProfile GetOrCreate(string name)
        {
            var p = Find(name);
            if (p != null) return p;
            p = new PlayerProfile { name = name.Trim(), lastPlayed = Now() };
            profiles.items.Add(p);
            return p;
        }

        /// <summary>Counts an attempt. Called when the level starts.</summary>
        public static void StartRun(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var p = GetOrCreate(name);
            p.attempts++;
            p.lastPlayed = Now();
            Save();
        }

        /// <summary>Saves the result of a finished run. Returns true if it is a new personal best.</summary>
        public static bool RecordRun(string name, int score, float time, bool escaped, int gems)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var p = GetOrCreate(name);
            bool newBest = score > p.bestScore;
            if (newBest) p.bestScore = score;
            if (escaped)
            {
                p.wins++;
                if (p.bestEscapeTime <= 0f || time < p.bestEscapeTime) p.bestEscapeTime = time;
            }
            p.totalGems += gems;
            p.lastPlayed = Now();

            leaderboard.items.Add(new LeaderboardEntry { name = p.name, score = score, time = time, escaped = escaped, date = Now() });
            leaderboard.items.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.time.CompareTo(b.time));
            int size = Mathf.Max(1, GameManager.Settings.leaderboardSize);
            if (leaderboard.items.Count > size) leaderboard.items.RemoveRange(size, leaderboard.items.Count - size);

            Save();
            LastRunWasHighScore = newBest && score > 0;
            return LastRunWasHighScore;
        }

        /// <summary>Deletes every saved profile, the leaderboard, the last name and the options.</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(ProfilesKey);
            PlayerPrefs.DeleteKey(LeaderboardKey);
            PlayerPrefs.DeleteKey(LastNameKey);
            PlayerPrefs.DeleteKey(PlayerOptions.PrefsKey);
            PlayerPrefs.Save();
            profiles = null;
            leaderboard = null;
            PlayerOptions.Load();
        }

        static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }
}
