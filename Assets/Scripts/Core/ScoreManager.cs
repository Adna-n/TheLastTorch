// ScoreManager: keeps the score, split into categories (gems, oil, keys, heart, damage, bonuses)
// plus how many of each were collected, so the end screen can show a breakdown. Point values come
// from GameSettings. Final score on a win = collected points - damage + escape bonus + torch bonus;
// on a loss there are no bonuses.
using System;
using UnityEngine;

namespace LastTorch
{
    public enum ScoreCategory { Gems, Oil, Keys, Heart, Damage, EscapeBonus, TorchBonus }

    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        static readonly int CategoryCount = Enum.GetValues(typeof(ScoreCategory)).Length;
        readonly int[] points = new int[CategoryCount];
        readonly int[] counts = new int[CategoryCount];

        public int GemsCollected => counts[(int)ScoreCategory.Gems];

        /// <summary>Current total (never below zero).</summary>
        public int Total
        {
            get
            {
                int sum = 0;
                foreach (int p in points) sum += p;
                return Mathf.Max(0, sum);
            }
        }

        /// <summary>(newTotal, delta, category)</summary>
        public event Action<int, int, ScoreCategory> OnScoreChanged;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public int GetPoints(ScoreCategory category) => points[(int)category];
        /// <summary>How many times points were added in this category (gems picked up, hits taken...).</summary>
        public int GetCount(ScoreCategory category) => counts[(int)category];

        public void AddPoints(int amount, ScoreCategory category)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            points[(int)category] += amount;
            counts[(int)category]++;
            OnScoreChanged?.Invoke(Total, amount, category);
        }

        public void AddPenalty(int amount)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            points[(int)ScoreCategory.Damage] -= amount;
            counts[(int)ScoreCategory.Damage]++;
            OnScoreChanged?.Invoke(Total, -amount, ScoreCategory.Damage);
        }

        /// <summary>Call right BEFORE GameManager.Win(): adds the escape and torch bonuses.</summary>
        public void ApplyEscapeBonuses(float collapseSecondsLeft, float fuelLeft)
        {
            var s = GameManager.Settings;
            points[(int)ScoreCategory.EscapeBonus] = Mathf.RoundToInt(Mathf.Max(0f, collapseSecondsLeft) * s.escapeBonusPerSecond);
            points[(int)ScoreCategory.TorchBonus] = Mathf.RoundToInt(Mathf.Max(0f, fuelLeft) * s.torchBonusPerFuel);
            OnScoreChanged?.Invoke(Total, 0, ScoreCategory.EscapeBonus);
        }
    }
}
