// HUDController: everything on screen during play. Top-left: fuel bar ("FUEL 76%", turns red and
// pulses when low) and hearts. Top-right: score and player name. Top-centre: the three key icons
// (grey until found) and, during the collapse, a big red countdown. Bottom-centre: hints.
// Also: floating "+points" text, red damage flash, red low-fuel vignette, and the intro story
// overlay (three lines fade in, any key skips). Build() creates the widgets (called by SceneBuilder).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastTorch
{
    public class HUDController : MonoBehaviour
    {
        public UISkin skin;

        [Header("Widgets (created by Build)")]
        public CanvasGroup hudGroup;
        public RectTransform fuelFill;
        public Image fuelFillImage;
        public Text fuelLabel;
        public RectTransform heartsRow;
        public Text scoreText;
        public Text nameText;
        public RawImage[] keyIcons = new RawImage[3];
        public Text collapseText;
        public Text hintText;
        public Image damageFlash;
        public Image lowFuelVignette;
        public RectTransform floatingLayer;
        public CanvasGroup introGroup;
        public Text[] introLines = new Text[3];

        static readonly string[] Story =
        {
            "The map led you here. The door slammed shut behind you.",
            "Your torch won't last forever.",
            "Find the Heart of Eternity, and find a way out."
        };

        TorchSystem torch;
        PlayerHealth health;
        Inventory inventory;
        Camera cam;
        readonly List<Image> hearts = new List<Image>();
        float hintAlpha;
        float flashAlpha;
        float introTimer;
        bool shownControls;

        // ------------------------------------------------------------------ building (editor)

        public void Build(UISkin uiSkin, Transform canvas)
        {
            skin = uiSkin;
            var root = UIFactory.CreateRect("HUD", canvas);
            UIFactory.Stretch(root);
            hudGroup = root.gameObject.AddComponent<CanvasGroup>();
            hudGroup.blocksRaycasts = false;

            // Full-screen effects (behind the widgets)
            lowFuelVignette = UIFactory.CreateImage(root, "LowFuelVignette", skin.vignette, new Color(0.8f, 0f, 0f, 0f));
            UIFactory.Stretch(lowFuelVignette.rectTransform);
            damageFlash = UIFactory.CreateImage(root, "DamageFlash", null, new Color(0.8f, 0f, 0f, 0f));
            UIFactory.Stretch(damageFlash.rectTransform);

            // Fuel bar
            var fuelBack = UIFactory.CreateImage(root, "FuelBar", skin.bar, new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Place(fuelBack.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(420f, 34f));
            fuelFillImage = UIFactory.CreateImage(fuelBack.transform, "Fill", skin.bar, UISkin.Orange);
            fuelFill = fuelFillImage.rectTransform;
            fuelFill.anchorMin = Vector2.zero;
            fuelFill.anchorMax = Vector2.one;
            fuelFill.pivot = new Vector2(0f, 0.5f);
            fuelFill.offsetMin = new Vector2(4f, 4f);
            fuelFill.offsetMax = new Vector2(-4f, -4f);
            fuelLabel = UIFactory.CreateText(root, "FuelLabel", "FUEL 100%", 28, UISkin.Parchment, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(fuelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(475f, -40f), new Vector2(260f, 34f));
            AddShadow(fuelLabel);

            // Hearts (images are created at runtime so maxHearts can be changed live)
            heartsRow = UIFactory.CreateRect("Hearts", root);
            UIFactory.Place(heartsRow, new Vector2(0f, 1f), new Vector2(40f, -90f), new Vector2(400f, 46f));

            // Score + name
            scoreText = UIFactory.CreateText(root, "Score", "0", 48, UISkin.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -26f), new Vector2(500f, 60f));
            AddShadow(scoreText);
            nameText = UIFactory.CreateText(root, "Name", "", 26, UISkin.Parchment, TextAnchor.MiddleRight);
            UIFactory.Place(nameText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -88f), new Vector2(500f, 34f));
            AddShadow(nameText);

            // Key icons
            for (int i = 0; i < 3; i++)
            {
                var back = UIFactory.CreateImage(root, "KeySlot_" + (KeyType)i, skin.bar, new Color(0f, 0f, 0f, 0.55f));
                UIFactory.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 92f, -24f), new Vector2(76f, 76f));
                back.rectTransform.pivot = new Vector2(0.5f, 1f);
                var icon = UIFactory.CreateRect("Icon", back.transform).gameObject.AddComponent<RawImage>();
                icon.texture = skin.Glyph((KeyType)i);
                icon.raycastTarget = false;
                UIFactory.Stretch(icon.rectTransform, 8f, 8f, 8f, 8f);
                keyIcons[i] = icon;
            }

            // Collapse countdown
            collapseText = UIFactory.CreateText(root, "CollapseTimer", "", 76, UISkin.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(collapseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(700f, 100f));
            collapseText.rectTransform.pivot = new Vector2(0.5f, 1f);
            AddShadow(collapseText);

            // Hint line
            hintText = UIFactory.CreateText(root, "Hint", "", 34, UISkin.Parchment, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1500f, 60f));
            hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
            AddShadow(hintText);

            floatingLayer = UIFactory.CreateRect("FloatingText", root);
            UIFactory.Stretch(floatingLayer);

            // Intro overlay (separate from the HUD group so it shows while the HUD is hidden)
            var intro = UIFactory.CreateImage(canvas, "Intro", null, Color.black);
            UIFactory.Stretch(intro.rectTransform);
            introGroup = intro.gameObject.AddComponent<CanvasGroup>();
            introGroup.blocksRaycasts = false;
            for (int i = 0; i < 3; i++)
            {
                var line = UIFactory.CreateText(intro.transform, "Line" + (i + 1), Story[i], 44, UISkin.Parchment, TextAnchor.MiddleCenter, FontStyle.Italic);
                UIFactory.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f - i * 90f), new Vector2(1600f, 70f));
                line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                introLines[i] = line;
            }
            var skip = UIFactory.CreateText(intro.transform, "Skip", "Press any key", 24, new Color(1f, 1f, 1f, 0.45f));
            UIFactory.Place(skip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(600f, 40f));
            skip.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        static void AddShadow(Graphic g)
        {
            var shadow = g.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        // ------------------------------------------------------------------ runtime

        void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                torch = player.GetComponent<TorchSystem>();
                health = player.GetComponent<PlayerHealth>();
                inventory = player.GetComponent<Inventory>();
            }
            cam = Camera.main;

            nameText.text = GameSession.PlayerName + "  ·  " + GameSession.Difficulty;
            scoreText.text = "0";
            hintText.text = string.Empty;
            collapseText.text = string.Empty;

            if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
            if (health != null) { health.OnHealthChanged += (c, m) => RefreshHearts(); health.OnDamaged += () => flashAlpha = GameManager.Settings.damageFlashAlpha; }
            if (inventory != null) inventory.OnChanged += RefreshKeys;
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += OnStateChanged;

            RefreshHearts();
            RefreshKeys();
            var gm = GameManager.Instance;
            introGroup.alpha = gm != null && gm.State == GameState.Intro ? 1f : 0f;
            if (gm != null && gm.State != GameState.Intro) OnStateChanged(gm.State);
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing && !shownControls && GameManager.Instance != null)
            {
                shownControls = true;
                GameManager.Instance.ShowHint("WASD move  ·  Mouse look  ·  Shift sprint  ·  E interact  ·  Esc pause",
                    GameManager.Settings.controlsHintDuration);
            }
        }

        void OnScoreChanged(int total, int delta, ScoreCategory category)
        {
            scoreText.text = total.ToString("N0");
            if (delta == 0 || category == ScoreCategory.EscapeBonus || category == ScoreCategory.TorchBonus) return;
            if (cam == null || torch == null) return;

            Vector3 screen = cam.WorldToScreenPoint(torch.transform.position + Vector3.up * 2.2f);
            if (screen.z < 0f) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(floatingLayer, screen, null, out Vector2 local))
                FloatingText.Spawn(floatingLayer, local + Random.insideUnitCircle * 20f, (delta > 0 ? "+" : "") + delta,
                    delta > 0 ? UISkin.Gold : UISkin.Danger);
        }

        void RefreshHearts()
        {
            int max = Mathf.Max(0, GameManager.Settings.maxHearts);
            while (hearts.Count < max)
            {
                var img = UIFactory.CreateImage(heartsRow, "Heart" + hearts.Count, skin.heart, Color.white);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(0f, 0.5f);
                img.rectTransform.sizeDelta = new Vector2(44f, 44f);
                img.rectTransform.anchoredPosition = new Vector2(hearts.Count * 52f, 0f);
                img.preserveAspect = true;
                hearts.Add(img);
            }
            int current = health != null ? health.Hearts : max;
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].color = i < current ? new Color(0.92f, 0.15f, 0.15f) : new Color(0.25f, 0.22f, 0.22f, 0.7f);
            }
        }

        void RefreshKeys()
        {
            for (int i = 0; i < 3; i++)
            {
                bool has = inventory != null && inventory.HasKey((KeyType)i);
                keyIcons[i].color = has ? UISkin.KeyColor((KeyType)i) : new Color(0.5f, 0.5f, 0.5f, 0.25f);
            }
        }

        void Update()
        {
            var s = GameManager.Settings;
            var gm = GameManager.Instance;
            float dt = Time.unscaledDeltaTime;
            GameState state = gm != null ? gm.State : GameState.Playing;

            // HUD visible only while playing / paused
            bool showHud = state == GameState.Playing || state == GameState.Collapse || state == GameState.Paused;
            hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, showHud ? 1f : 0f, dt * 3f);

            UpdateIntro(s, state, dt);
            UpdateFuel(s);

            if (hearts.Count != Mathf.Max(0, s.maxHearts)) RefreshHearts();

            // Collapse countdown
            var collapse = CollapseSequence.Instance;
            bool showTimer = collapse != null && collapse.IsActive && (state == GameState.Collapse || state == GameState.Paused);
            if (showTimer)
            {
                int secs = Mathf.CeilToInt(collapse.TimeLeft);
                collapseText.text = $"{secs / 60}:{secs % 60:00}";
                float pulse = collapse.TimeLeft < 10f ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 6f)) : 1f;
                collapseText.rectTransform.localScale = Vector3.one * pulse;
            }
            else collapseText.text = string.Empty;

            // Hint fade in/out
            string active = gm != null ? gm.ActiveHint : null;
            if (active != null) hintText.text = active;
            hintAlpha = Mathf.MoveTowards(hintAlpha, active != null ? 1f : 0f, dt / Mathf.Max(0.01f, s.hintFadeTime));
            var hc = hintText.color; hc.a = hintAlpha; hintText.color = hc;

            // Damage flash
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * s.damageFlashAlpha / Mathf.Max(0.01f, s.damageFlashFade));
            var fc = damageFlash.color; fc.a = flashAlpha; damageFlash.color = fc;
        }

        void UpdateIntro(GameSettings s, GameState state, float dt)
        {
            if (state == GameState.Intro)
            {
                introTimer += dt;
                float lineDelay = s.introDuration / 4f;
                for (int i = 0; i < introLines.Length; i++)
                {
                    var c = introLines[i].color;
                    c.a = Mathf.Clamp01((introTimer - i * lineDelay) / Mathf.Max(0.01f, s.introLineFade));
                    introLines[i].color = c;
                }
                introGroup.alpha = 1f;
            }
            else if (introGroup.alpha > 0f)
            {
                introGroup.alpha = Mathf.MoveTowards(introGroup.alpha, 0f, dt / Mathf.Max(0.01f, s.introLineFade));
            }
        }

        void UpdateFuel(GameSettings s)
        {
            if (torch == null) return;
            float pct = torch.FuelPercent;
            fuelFill.anchorMax = new Vector2(Mathf.Max(0.001f, pct), 1f);
            fuelLabel.text = $"FUEL {Mathf.CeilToInt(pct * 100f)}%";

            float warn = Mathf.Max(0.01f, s.lowFuelWarningPercent / 100f);
            Color barColor = Color.Lerp(UISkin.Danger, UISkin.Orange, Mathf.Clamp01((pct - warn * 0.5f) / warn));
            float vignetteTarget = 0f;
            if (torch.IsLowFuel)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * s.lowFuelPulseSpeed);
                barColor = Color.Lerp(barColor, Color.white, pulse * 0.35f);
                vignetteTarget = 0.25f + 0.25f * pulse;
            }
            fuelFillImage.color = barColor;
            fuelLabel.color = torch.IsLowFuel ? UISkin.Danger : UISkin.Parchment;
            var vc = lowFuelVignette.color;
            vc.a = Mathf.MoveTowards(vc.a, vignetteTarget, Time.unscaledDeltaTime * 2f);
            lowFuelVignette.color = vc;
        }
    }
}
