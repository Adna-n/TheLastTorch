// LeaderboardEntry: one finished run on the leaderboard (name, score, time, escaped or not, date).
// Also has the shared m:ss time formatter used by the menus.
using System;

namespace LastTorch
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
        public float time;
        public bool escaped;
        public string date;

        public static string FormatTime(float seconds)
        {
            int total = Math.Max(0, (int)Math.Round(seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
