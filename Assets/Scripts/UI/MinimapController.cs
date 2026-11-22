// MinimapController: a map of the maze in the top-right corner (press M for a big map in the centre).
// It draws the maze grid from LevelLayout into a small texture (walls, floor, coloured doors, red
// trap tiles), marks the keys, the Heart, the exit, oil jars and gems, and shows the player as an
// arrow that turns with them. Optional fog of war: only cells near the player get drawn.
// All options are in GameSettings > Minimap. Build() creates the widgets (called by SceneBuilder).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastTorch
{
    public class MinimapController : MonoBehaviour
    {
        public UISkin skin;
        public RectTransform frame;
        public RawImage mapImage;
        public RectTransform markerLayer;
        public Text label;

        const int PixelsPerCell = 8;
        static readonly Color32 Hidden = new Color32(0, 0, 0, 0);

        class Marker
        {
            public Transform target;
            public RectTransform rect;
            public float size;
            public bool objective;
            public ExitGate gate;
        }

        LevelLayout layout;
        Transform player;
        Texture2D texture;
        Color32[] pixels;
        bool[,] explored;
        readonly List<Marker> markers = new List<Marker>();
        RectTransform playerArrow;
        bool big;

        // ------------------------------------------------------------------ building (editor)

        public void Build(UISkin uiSkin, Transform parent)
        {
            skin = uiSkin;
            var bg = UIFactory.CreateImage(parent, "Minimap", skin.bar, new Color(0f, 0f, 0f, 0.75f));
            frame = bg.rectTransform;
            UIFactory.Place(frame, new Vector2(1f, 1f), new Vector2(-40f, -140f), new Vector2(316f, 270f));

            mapImage = UIFactory.CreateRect("Map", frame).gameObject.AddComponent<RawImage>();
            mapImage.raycastTarget = false;
            UIFactory.Stretch(mapImage.rectTransform, 8f, 8f, 8f, 8f);

            markerLayer = UIFactory.CreateRect("Markers", mapImage.transform);
            UIFactory.Stretch(markerLayer);

            label = UIFactory.CreateText(frame, "Label", "MAP  [M]", 20, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperRight);
            var lr = label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(1f, 0f);
            lr.pivot = new Vector2(1f, 1f);
            lr.anchoredPosition = new Vector2(-6f, -4f);
            lr.sizeDelta = new Vector2(300f, 26f);
        }

        // ------------------------------------------------------------------ runtime

        void Start()
        {
            var s = GameManager.Settings;
            layout = FindAnyObjectByType<LevelLayout>();
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
            if (layout == null || player == null)
            {
                frame.gameObject.SetActive(false);
                return;
            }

            explored = new bool[layout.rows, layout.cols];
            texture = new Texture2D(layout.cols * PixelsPerCell, layout.rows * PixelsPerCell, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "MinimapTexture"
            };
            pixels = new Color32[texture.width * texture.height];
            for (int r = 0; r < layout.rows; r++)
                for (int c = 0; c < layout.cols; c++)
                {
                    explored[r, c] = s.minimapRevealAll;
                    PaintCell(r, c);
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            mapImage.texture = texture;

            CreateMarkers();
            ApplySize();
        }

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        Color32 CellColor(char ch)
        {
            switch (ch)
            {
                case '#': return new Color32(20, 20, 18, 255);
                case '^': return new Color32(150, 55, 45, 255);    // spikes
                case 'T': return new Color32(150, 95, 45, 255);    // dart plate
                case 'b': return new Color32(130, 70, 50, 255);    // boulder trigger
                case 'A': return new Color32(255, 154, 60, 255);   // Sun door
                case 'H': return new Color32(255, 210, 120, 255);  // Sanctum door
                case 'X': return new Color32(255, 250, 230, 255);  // exit gate
                case 'D': return new Color32(110, 40, 35, 255);    // entrance (sealed)
                default: return new Color32(118, 110, 94, 255);    // floor
            }
        }

        void PaintCell(int r, int c)
        {
            Color32 col = explored[r, c] ? CellColor(layout.At(r, c)) : Hidden;
            int y0 = (layout.rows - 1 - r) * PixelsPerCell, x0 = c * PixelsPerCell;
            for (int y = 0; y < PixelsPerCell; y++)
                for (int x = 0; x < PixelsPerCell; x++)
                    pixels[(y0 + y) * texture.width + x0 + x] = col;
        }

        void CreateMarkers()
        {
            foreach (var item in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            {
                switch (item.type)
                {
                    case CollectibleType.Key:
                        var bgDot = AddMarker(item.transform, skin.dot, new Color(0f, 0f, 0f, 0.7f), 26f, true);
                        var glyph = UIFactory.CreateRect("Glyph", bgDot.rect).gameObject.AddComponent<RawImage>();
                        glyph.texture = skin.Glyph(item.keyType);
                        glyph.color = UISkin.KeyColor(item.keyType);
                        glyph.raycastTarget = false;
                        UIFactory.Stretch(glyph.rectTransform, 1f, 1f, 1f, 1f);
                        break;
                    case CollectibleType.Heart: AddMarker(item.transform, skin.dot, new Color(0.18f, 0.9f, 0.84f), 20f, true); break;
                    case CollectibleType.Oil: AddMarker(item.transform, skin.dot, UISkin.Orange, 11f, false); break;
                    case CollectibleType.Gem: AddMarker(item.transform, skin.dot, new Color(0.5f, 1f, 0.95f), 8f, false); break;
                }
            }
            var gate = FindAnyObjectByType<ExitGate>();
            if (gate != null) AddMarker(gate.transform, skin.dot, Color.white, 18f, true).gate = gate;

            playerArrow = UIFactory.CreateImage(markerLayer, "Player", skin.arrow, new Color(1f, 0.85f, 0.4f)).rectTransform;
            playerArrow.pivot = new Vector2(0.5f, 0.5f);
            var outline = playerArrow.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
        }

        Marker AddMarker(Transform target, Sprite sprite, Color color, float size, bool objective)
        {
            var img = UIFactory.CreateImage(markerLayer, target.name, sprite, color);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var m = new Marker { target = target, rect = img.rectTransform, size = size, objective = objective };
            markers.Add(m);
            return m;
        }

        void ApplySize()
        {
            var s = GameManager.Settings;
            float width = big ? s.bigMapSize : s.minimapSize;
            float height = width * layout.rows / Mathf.Max(1, layout.cols);
            if (big) UIFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width + 16f, height + 16f));
            else UIFactory.Place(frame, new Vector2(1f, 1f), new Vector2(-40f, -140f), new Vector2(width + 16f, height + 16f));

            float scale = width / Mathf.Max(1f, s.minimapSize);
            foreach (var m in markers) m.rect.sizeDelta = Vector2.one * m.size * Mathf.Sqrt(scale);
            playerArrow.sizeDelta = Vector2.one * 22f * Mathf.Sqrt(scale);
            label.text = big ? "MAP  [M] close" : "MAP  [M]";
        }

        void Update()
        {
            if (layout == null || player == null) return;
            var s = GameManager.Settings;
            var gm = GameManager.Instance;

            frame.gameObject.SetActive(s.minimapEnabled);
            if (!s.minimapEnabled) return;

            bool canToggle = gm == null || gm.IsGameplayActive || gm.State == GameState.Paused;
            if (canToggle && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                big = !big;
                ApplySize();
            }
            if (gm != null && gm.IsGameOver && big) { big = false; ApplySize(); }

            var c = new Color(1f, 1f, 1f, s.minimapOpacity);
            mapImage.color = c;

            Reveal(s);
            UpdateMarkers(s);

            Vector2 uv = layout.WorldToMapUV(player.position);
            playerArrow.anchorMin = playerArrow.anchorMax = uv;
            playerArrow.anchoredPosition = Vector2.zero;
            playerArrow.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
        }

        void Reveal(GameSettings s)
        {
            if (s.minimapRevealAll)
            {
                if (!explored[0, 0] || !explored[layout.rows - 1, layout.cols - 1]) RevealArea(0, 0, Mathf.Max(layout.rows, layout.cols) * 2);
                return;
            }
            var cell = layout.WorldToCell(player.position);
            RevealArea(cell.x, cell.y, s.minimapRevealRadius);
        }

        void RevealArea(int row, int col, int radius)
        {
            bool dirty = false;
            for (int r = row - radius; r <= row + radius; r++)
            {
                for (int c = col - radius; c <= col + radius; c++)
                {
                    if (!layout.InBounds(r, c) || explored[r, c]) continue;
                    int dr = r - row, dc = c - col;
                    if (dr * dr + dc * dc > radius * radius) continue;
                    explored[r, c] = true;
                    PaintCell(r, c);
                    dirty = true;
                }
            }
            if (dirty)
            {
                texture.SetPixels32(pixels);
                texture.Apply();
            }
        }

        void UpdateMarkers(GameSettings s)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var m in markers)
            {
                if (m.target == null) // collected
                {
                    if (m.rect.gameObject.activeSelf) m.rect.gameObject.SetActive(false);
                    continue;
                }
                var cell = layout.WorldToCell(m.target.position);
                bool seen = layout.InBounds(cell.x, cell.y) && explored[cell.x, cell.y];
                bool visible = seen || (m.objective && s.minimapShowObjectivesAlways);
                m.rect.gameObject.SetActive(visible);
                if (!visible) continue;

                m.rect.anchorMin = m.rect.anchorMax = layout.WorldToMapUV(m.target.position);
                m.rect.anchoredPosition = Vector2.zero;
                if (m.objective) m.rect.localScale = Vector3.one * pulse;
                if (m.gate != null)
                    m.rect.GetComponent<Image>().color = m.gate.IsOpen ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.9f);
            }
        }
    }
}
