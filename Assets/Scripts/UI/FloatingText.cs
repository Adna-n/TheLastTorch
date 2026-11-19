// FloatingText: a short "+10" / "-20" label that pops up above the player, rises and fades out.
// Spawned by the HUD whenever the score changes. Duration and rise distance are in GameSettings.
using UnityEngine;
using UnityEngine.UI;

namespace LastTorch
{
    public class FloatingText : MonoBehaviour
    {
        Text text;
        RectTransform rect;
        Vector2 start;
        Color baseColor;
        float age;

        public static FloatingText Spawn(RectTransform layer, Vector2 localPosition, string content, Color color, int fontSize = 42)
        {
            var t = UIFactory.CreateText(layer, "FloatingText", content, fontSize, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 60f);
            rt.anchoredPosition = localPosition;
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            var ft = t.gameObject.AddComponent<FloatingText>();
            ft.text = t;
            ft.rect = rt;
            ft.start = localPosition;
            ft.baseColor = color;
            return ft;
        }

        void Update()
        {
            var s = GameManager.Settings;
            age += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(age / Mathf.Max(0.01f, s.floatingTextDuration));
            float ease = 1f - (1f - k) * (1f - k);
            rect.anchoredPosition = start + Vector2.up * (s.floatingTextRise * ease);
            var c = baseColor;
            c.a = 1f - k * k;
            text.color = c;
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
