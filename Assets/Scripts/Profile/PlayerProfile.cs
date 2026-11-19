// PlayerProfile: the saved stats of one explorer name (best score, best escape time, attempts,
// wins, total gems, last played date). Stored as JSON by ProfileManager.
using System;

namespace LastTorch
{
    [Serializable]
    public class PlayerProfile
    {
        public string name;
        public int bestScore;
        /// <summary>Fastest winning run in seconds (0 = never escaped).</summary>
        public float bestEscapeTime;
        public int attempts;
        public int wins;
        public int totalGems;
        public string lastPlayed;
    }
}
