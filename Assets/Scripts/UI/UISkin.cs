// UISkin: the generated sprites/textures and the shared colours used by the in-game UI
// (HUD, pause menu, end screen). The scene builder fills it in and hands it to each screen.
using System;
using UnityEngine;

namespace LastTorch
{
    [Serializable]
    public class UISkin
    {
        public Sprite bar;
        public Sprite heart;
        public Sprite vignette;
        public Texture2D glyphSun;
        public Texture2D glyphMoon;
        public Texture2D glyphSerpent;

        public static readonly Color Orange = new Color(1f, 0.604f, 0.235f);
        public static readonly Color Parchment = new Color(0.91f, 0.863f, 0.753f);
        public static readonly Color Panel = new Color(0.05f, 0.045f, 0.04f, 0.88f);
        public static readonly Color ButtonColor = new Color(0.227f, 0.196f, 0.165f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.35f);
        public static readonly Color Danger = new Color(0.95f, 0.25f, 0.2f);
        public static readonly Color SunColor = new Color(1f, 0.6f, 0.24f);
        public static readonly Color MoonColor = new Color(0.75f, 0.85f, 1f);
        public static readonly Color SerpentColor = new Color(0.36f, 1f, 0.48f);

        public Texture2D Glyph(KeyType key) => key == KeyType.Sun ? glyphSun : key == KeyType.Moon ? glyphMoon : glyphSerpent;
        public static Color KeyColor(KeyType key) => key == KeyType.Sun ? SunColor : key == KeyType.Moon ? MoonColor : SerpentColor;
    }
}
