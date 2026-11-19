// Inventory: what the explorer is carrying: the three symbol keys (Sun, Moon, Serpent) and the
// Heart of Eternity. Raises OnChanged so the HUD key icons can update.
using System;
using UnityEngine;

namespace LastTorch
{
    public enum KeyType { Sun, Moon, Serpent }

    public class Inventory : MonoBehaviour
    {
        [SerializeField] bool hasSun;
        [SerializeField] bool hasMoon;
        [SerializeField] bool hasSerpent;
        [SerializeField] bool hasHeart;

        public bool HasSun => hasSun;
        public bool HasMoon => hasMoon;
        public bool HasSerpent => hasSerpent;
        public bool HasHeart => hasHeart;

        public event Action OnChanged;

        public bool HasKey(KeyType key)
        {
            switch (key)
            {
                case KeyType.Sun: return hasSun;
                case KeyType.Moon: return hasMoon;
                default: return hasSerpent;
            }
        }

        public void AddKey(KeyType key)
        {
            switch (key)
            {
                case KeyType.Sun: hasSun = true; break;
                case KeyType.Moon: hasMoon = true; break;
                default: hasSerpent = true; break;
            }
            OnChanged?.Invoke();
        }

        public void TakeHeart()
        {
            hasHeart = true;
            OnChanged?.Invoke();
        }

        public static string KeyName(KeyType key) => key + " Key";
    }
}
