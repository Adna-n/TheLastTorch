// SceneBuilder: generates the game's scenes from code and sets the Build Settings.
// BuildMainMenu() creates Assets/Scenes/MainMenu.unity: a torch-lit stone wall (two flickering
// torches, glyphs, vines, pillars, drifting camera, post-processing, ambient drone) and the full
// menu UI (title, name input, difficulty, Play / How to Play / Leaderboard / Settings / Quit).
// BuildTemple() creates Assets/Scenes/Temple.unity: the maze from LevelBuilder, the player and
// camera, darkness + fog, post-processing volumes and the managers (game, score, collapse, audio).
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastTorch.EditorTools.EditorUtil;

namespace LastTorch.EditorTools
{
    public static class SceneBuilder
    {
        public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        public const string TemplePath = "Assets/Scenes/Temple.unity";
        /// <summary>Scene the Play button loads.</summary>
        public const string GameSceneName = "Temple";

        static readonly Color Orange = Hex("FF9A3C");
        static readonly Color Parchment = Hex("E8DCC0");
        static readonly Color PanelColor = new Color(0.05f, 0.045f, 0.04f, 0.85f);
        static readonly Color ButtonColor = Hex("3A322A");
        static readonly Color ErrorColor = Hex("FF6A55");

        // ------------------------------------------------------------------ shared scene setup

        public static void SetupDarkness(Color ambient, float fogDensity)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        public static Camera CreateCamera(Vector3 position, Quaternion rotation, float fov)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(position, rotation);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = fov;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            go.AddComponent<AudioListener>();
            return cam;
        }

        public static AudioManager CreateAudioManager(Transform parent = null)
        {
            var go = new GameObject("AudioManager");
            if (parent != null) go.transform.SetParent(parent, false);
            var audio = go.AddComponent<AudioManager>();
            audio.sounds = AudioGenerator.BuildSoundList();
            return audio;
        }

        public static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var prefab = PrefabFactory.Load(prefabName);
            if (prefab == null) throw new System.Exception($"Prefab '{prefabName}' missing. Run 'The Last Torch > 1. Generate Assets' first.");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        /// <summary>
        /// Tidies the Hierarchy: creates one parent per section (in this order) and moves the named
        /// top-level objects under it, e.g. ("Managers", "GameManager", "AudioManager").
        /// </summary>
        public static void Organize(params (string section, string[] objects)[] sections)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < sections.Length; i++)
            {
                var parent = new GameObject(sections[i].section).transform;
                parent.SetSiblingIndex(i);
                foreach (string name in sections[i].objects)
                {
                    var go = roots.FirstOrDefault(r => r.name == name);
                    if (go != null) go.transform.SetParent(parent, true);
                    else Debug.LogWarning($"[SceneBuilder] Organize: '{name}' not found.");
                }
            }
        }

        public static void SetBuildSettings(params string[] scenePaths)
        {
            EditorBuildSettings.scenes = scenePaths
                .Where(p => System.IO.File.Exists(p))
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();
        }

        // ------------------------------------------------------------------ temple (the level)

        public static void BuildTemple()
        {
            var settings = GameSettingsAsset.GetOrCreate();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupDarkness(settings.ambientColor, settings.fogDensity);

            // Level from maze.txt
            var levelRoot = new GameObject("Level").transform;
            var info = LevelBuilder.Build(settings, levelRoot);

            // Player + third-person camera
            var player = Instantiate("Player", info.spawnPosition, info.spawnRotation);
            var camPos = info.spawnPosition + info.spawnRotation * new Vector3(0f, 2.5f, -settings.cameraDistance);
            var cam = CreateCamera(camPos, info.spawnRotation, 60f);
            cam.gameObject.AddComponent<CameraShake>();
            cam.gameObject.AddComponent<ThirdPersonCamera>().target = player.transform;
            player.GetComponent<PlayerController>().cameraTransform = cam.transform;

            // Post-processing
            PostFXBuilder.CreateVolume("Global Volume", PostFXBuilder.LoadGlobal(), 1f, 0f);
            var collapseVolume = PostFXBuilder.CreateVolume("Collapse Volume", PostFXBuilder.LoadCollapse(), 0f, 1f);

            // Managers
            var managers = new GameObject("GameManager");
            var gm = managers.AddComponent<GameManager>();
            gm.settings = settings;
            gm.playIntro = true;
            managers.AddComponent<ScoreManager>();
            var collapse = managers.AddComponent<CollapseSequence>();
            collapse.collapseVolume = collapseVolume;
            collapse.exitGate = info.exitGate;
            collapse.debrisPrefab = PrefabFactory.Load("Debris");
            var audio = CreateAudioManager();
            audio.startLoop = SoundId.AmbientDrone;

            BuildTempleUI();

            Organize(
                ("Managers", new[] { "GameManager", "AudioManager" }),
                ("Camera & Player", new[] { "Main Camera", "Player" }),
                ("World", new[] { "Level" }),
                ("Lighting & Effects", new[] { "Global Volume", "Collapse Volume" }),
                ("UI", new[] { "GameCanvas", "EventSystem", "UI Controllers" }));

            EditorSceneManager.SaveScene(scene, TemplePath);
        }

        /// <summary>In-game UI: HUD (+ intro overlay), end screen with fades, pause menu (top-most).</summary>
        static void BuildTempleUI()
        {
            var skin = new UISkin
            {
                bar = TextureGenerator.LoadSprite("ui_bar"),
                heart = TextureGenerator.LoadSprite("ui_heart"),
                vignette = TextureGenerator.LoadSprite("ui_vignette"),
                glyphSun = TextureGenerator.Load("glyph_sun"),
                glyphMoon = TextureGenerator.Load("glyph_moon"),
                glyphSerpent = TextureGenerator.Load("glyph_serpent"),
            };
            var canvas = UIFactory.CreateCanvas("GameCanvas").transform;
            UIFactory.CreateEventSystem();

            var ui = new GameObject("UI Controllers");
            ui.AddComponent<HUDController>().Build(skin, canvas);
            ui.AddComponent<EndScreen>().Build(skin, canvas);
            ui.AddComponent<PauseMenu>().Build(skin, canvas);
        }

        // ------------------------------------------------------------------ main menu

        public static void BuildMainMenu()
        {
            var settings = GameSettingsAsset.GetOrCreate();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupDarkness(new Color(0.03f, 0.025f, 0.03f), 0.03f);

            BuildMenuBackground();

            var target = new Vector3(0f, 2.9f, 4f);
            var camPos = new Vector3(0f, 2.2f, -5.5f);
            var cam = CreateCamera(camPos, Quaternion.LookRotation(target - camPos), 50f);
            cam.gameObject.AddComponent<MenuCameraDrift>();

            PostFXBuilder.CreateVolume("Global Volume", PostFXBuilder.LoadGlobal(), 1f, 0f);
            CreateAudioManager();

            BuildMenuUI(settings);

            Organize(
                ("Managers", new[] { "Main Menu Controller", "AudioManager" }),
                ("Camera", new[] { "Main Camera" }),
                ("World", new[] { "MenuBackground" }),
                ("Lighting & Effects", new[] { "Global Volume" }),
                ("UI", new[] { "MenuCanvas", "EventSystem" }));

            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        static void BuildMenuBackground()
        {
            var root = new GameObject("MenuBackground").transform;

            var wall = new GameObject("Wall");
            wall.transform.SetParent(root, false);
            wall.transform.localPosition = new Vector3(0f, 0f, 4.5f);
            wall.AddComponent<MeshFilter>().sharedMesh = MeshGenerator.Box("MenuWall", new Vector3(18f, 9f, 1f), new Vector3(0f, 4.5f, 0f));
            wall.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Load("Stone_Wall");
            wall.isStatic = true;

            var floor = new GameObject("Floor");
            floor.transform.SetParent(root, false);
            floor.AddComponent<MeshFilter>().sharedMesh = MeshGenerator.Box("MenuFloor", new Vector3(18f, 0.2f, 10f), new Vector3(0f, -0.1f, 0f));
            floor.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Load("Stone_Floor");
            floor.isStatic = true;

            WallTorch(root, new Vector3(-3.6f, 2.4f, 3.75f));
            WallTorch(root, new Vector3(3.6f, 2.4f, 3.75f));

            Instantiate("GlyphPanel", new Vector3(-6.4f, 3.3f, 3.99f), Quaternion.identity, root);
            Instantiate("GlyphPanel", new Vector3(6.4f, 3.3f, 3.99f), Quaternion.identity, root);
            var top = Instantiate("GlyphPanel", new Vector3(0f, 5.6f, 3.99f), Quaternion.identity, root);
            top.GetComponentInChildren<MeshRenderer>().sharedMaterial = MaterialFactory.Load("Glyph_Wall_3");
            Instantiate("Vine", new Vector3(-5f, 8.5f, 3.97f), Quaternion.identity, root);
            Instantiate("Vine", new Vector3(4.3f, 8f, 3.97f), Quaternion.identity, root);
            Instantiate("Vine", new Vector3(1.4f, 9f, 3.97f), Quaternion.identity, root);
            Instantiate("Pillar", new Vector3(-7.2f, 0f, 2.6f), Quaternion.identity, root);
            Instantiate("Pillar", new Vector3(7.2f, 0f, 2.6f), Quaternion.Euler(0f, 140f, 0f), root);
            Instantiate("Rubble", new Vector3(-1.8f, 0f, 3.2f), Quaternion.identity, root);
            Instantiate("Rubble", new Vector3(5.2f, 0f, 3.0f), Quaternion.Euler(0f, 90f, 0f), root);
            Instantiate("OilJar", new Vector3(2.2f, 0f, 3.1f), Quaternion.identity, root).GetComponent<Collectible>().enabled = false;

            // Dust motes drifting in the torchlight
            var dustGo = new GameObject("Dust");
            dustGo.transform.SetParent(root, false);
            dustGo.transform.localPosition = new Vector3(0f, 2.5f, 1f);
            var dust = dustGo.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 10f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.maxParticles = 200;
            var emission = dust.emission;
            emission.rateOverTime = 15f;
            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 5f, 6f);
            var noise = dust.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.3f;
            var dustRenderer = dustGo.GetComponent<ParticleSystemRenderer>();
            dustRenderer.sharedMaterial = MaterialFactory.Load("Particle_Ember");
            dustRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>A torch in a metal bracket on the wall, with fire particles and a flickering light.</summary>
        public static GameObject WallTorch(Transform parent, Vector3 position)
        {
            var root = new GameObject("WallTorch");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            Part(PrimitiveType.Cube, "Bracket", root.transform, new Vector3(0f, -0.15f, 0.18f), new Vector3(0.12f, 0.08f, 0.35f), MaterialFactory.Load("Metal"));
            var handle = Empty("Handle", root.transform, Vector3.zero).transform;
            handle.localRotation = Quaternion.Euler(-15f, 0f, 0f); // lean toward the room
            Part(PrimitiveType.Cylinder, "Wood", handle, new Vector3(0f, 0f, 0f), new Vector3(0.07f, 0.3f, 0.07f), MaterialFactory.Load("Wood"));
            Part(PrimitiveType.Cylinder, "Wrap", handle, new Vector3(0f, 0.28f, 0f), new Vector3(0.1f, 0.05f, 0.1f), MaterialFactory.Load("Explorer_Leather"));

            ExplorerBuilder.CreateFlame(handle, new Vector3(0f, 0.38f, 0f), MaterialFactory.Load("Particle_Fire"));
            ExplorerBuilder.CreateEmbers(handle, new Vector3(0f, 0.4f, 0f), MaterialFactory.Load("Particle_Ember"));

            var lightGo = Empty("Light", root.transform, new Vector3(0f, 0.6f, -0.35f));
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.28f);
            light.range = 9f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.Soft;
            lightGo.AddComponent<FlickerLight>();
            return root;
        }

        // ------------------------------------------------------------------ menu UI

        static Sprite Bar => TextureGenerator.LoadSprite("ui_bar");

        static RectTransform PlaceTop(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        static RectTransform PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        static Button MenuButton(Transform parent, string name, string label, float y, float height = 60f, bool primary = false)
        {
            var b = UIFactory.CreateButton(parent, name, label, Bar, primary ? Orange : ButtonColor,
                primary ? Color.black : Parchment, primary ? 38 : 28);
            PlaceTop((RectTransform)b.transform, 0f, y, 480f, height);
            return b;
        }

        static GameObject OverlayPanel(Transform canvas, string name, string title, out Button back)
        {
            var panel = UIFactory.CreateImage(canvas, name, Bar, PanelColor, true);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1000f, 720f));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchoredPosition = new Vector2(0f, -40f);

            var t = UIFactory.CreateText(panel.transform, "Title", title, 54, Orange, TextAnchor.MiddleCenter, FontStyle.Bold);
            PlaceTop(t.rectTransform, 0f, 30f, 900f, 70f);

            back = UIFactory.CreateButton(panel.transform, "Back", "BACK", Bar, ButtonColor, Parchment, 28);
            var rt = (RectTransform)back.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 36f);
            rt.sizeDelta = new Vector2(300f, 64f);
            return panel.gameObject;
        }

        static void BuildMenuUI(GameSettings settings)
        {
            var canvas = UIFactory.CreateCanvas("MenuCanvas").transform;
            UIFactory.CreateEventSystem();

            var vignette = UIFactory.CreateImage(canvas, "Vignette", TextureGenerator.LoadSprite("ui_vignette"), Color.white);
            UIFactory.Stretch(vignette.rectTransform);

            // Title
            var title = UIFactory.CreateText(canvas, "Title", "THE LAST TORCH", 124, Orange, TextAnchor.MiddleCenter, FontStyle.Bold);
            PlaceTop(title.rectTransform, 0f, 60f, 1700f, 160f);
            UIFactory.AddGlow(title, new Color(1f, 0.45f, 0.1f, 0.45f), 3f);
            var tagline = UIFactory.CreateText(canvas, "Tagline", "Your torch won't last forever.", 32, new Color(Parchment.r, Parchment.g, Parchment.b, 0.85f), TextAnchor.MiddleCenter, FontStyle.Italic);
            PlaceTop(tagline.rectTransform, 0f, 215f, 1400f, 50f);

            var footer = UIFactory.CreateText(canvas, "Footer", "All assets procedurally generated", 20, new Color(1f, 1f, 1f, 0.4f), TextAnchor.LowerRight);
            var frt = footer.rectTransform;
            frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(1f, 0f);
            frt.anchoredPosition = new Vector2(-30f, 20f);
            frt.sizeDelta = new Vector2(600f, 30f);

            // ---- Main panel (left)
            var mainImg = UIFactory.CreateImage(canvas, "MainPanel", Bar, PanelColor, true);
            var mrt = mainImg.rectTransform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
            mrt.pivot = new Vector2(0f, 0.5f);
            mrt.anchoredPosition = new Vector2(110f, -80f);
            mrt.sizeDelta = new Vector2(560f, 760f);
            var main = mainImg.transform;

            var nameLabel = UIFactory.CreateText(main, "NameLabel", "EXPLORER NAME", 24, Orange, TextAnchor.MiddleLeft, FontStyle.Bold);
            PlaceTop(nameLabel.rectTransform, 0f, 30f, 480f, 30f);
            var nameInput = UIFactory.CreateInputField(main, "NameInput", "Type your name...", Bar, new Color(0.12f, 0.1f, 0.09f, 1f), Parchment, 32, settings.maxNameLength);
            PlaceTop((RectTransform)nameInput.transform, 0f, 66f, 480f, 64f);
            var error = UIFactory.CreateText(main, "Error", "", 22, ErrorColor, TextAnchor.MiddleLeft);
            PlaceTop(error.rectTransform, 0f, 134f, 480f, 30f);
            var welcome = UIFactory.CreateText(main, "Welcome", "New explorer", 24, Parchment, TextAnchor.UpperLeft);
            PlaceTop(welcome.rectTransform, 0f, 166f, 480f, 64f);

            var diffLabel = UIFactory.CreateText(main, "DifficultyLabel", "DIFFICULTY", 24, Orange, TextAnchor.MiddleLeft, FontStyle.Bold);
            PlaceTop(diffLabel.rectTransform, 0f, 246f, 480f, 30f);
            string[] diffNames = { "EASY", "NORMAL", "HARD" };
            var diffButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                diffButtons[i] = UIFactory.CreateButton(main, "Difficulty_" + diffNames[i], diffNames[i], Bar, ButtonColor, Parchment, 24);
                PlaceTop((RectTransform)diffButtons[i].transform, (i - 1) * 165f, 282f, 150f, 56f);
            }

            var play = MenuButton(main, "PlayButton", "PLAY", 370f, 76f, true);
            var howTo = MenuButton(main, "HowToButton", "HOW TO PLAY", 466f);
            var leaderboard = MenuButton(main, "LeaderboardButton", "LEADERBOARD", 536f);
            var settingsBtn = MenuButton(main, "SettingsButton", "SETTINGS", 606f);
            var quit = MenuButton(main, "QuitButton", "QUIT", 676f);

            // ---- How to Play
            var howToPanel = OverlayPanel(canvas, "HowToPanel", "HOW TO PLAY", out var backHowTo);
            var howText = UIFactory.CreateText(howToPanel.transform, "Content",
                "<b><color=#FF9A3C>GOAL</color></b>   Find the 3 symbol keys, open the Inner Sanctum, take the Heart of Eternity, then escape before the temple collapses.\n\n" +
                "<b><color=#FF9A3C>CONTROLS</color></b>\n" +
                "WASD / Arrows   Move\nMouse   Look around\nShift   Sprint (burns more fuel)\nE   Interact\nEsc   Pause\n\n" +
                "<b><color=#FF9A3C>TIPS</color></b>\n" +
                "•  Your torch is your life. Oil jars refuel it.\n•  Sprinting burns fuel faster.\n•  Take the Heart, then RUN.",
                28, Parchment, TextAnchor.UpperLeft);
            UIFactory.Stretch(howText.rectTransform, 80f, 80f, 120f, 120f);
            howText.lineSpacing = 1.1f;

            // ---- Leaderboard
            var lbPanel = OverlayPanel(canvas, "LeaderboardPanel", "LEADERBOARD", out var backLb);
            float[] colX = { 90f, 170f, 500f, 660f, 790f };
            float[] colW = { 70f, 320f, 150f, 120f, 160f };
            string[] headers = { "#", "NAME", "SCORE", "TIME", "RESULT" };
            for (int c = 0; c < headers.Length; c++)
            {
                var h = UIFactory.CreateText(lbPanel.transform, "Header_" + headers[c], headers[c], 24, Orange, c == 1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, FontStyle.Bold);
                PlaceTopLeft(h.rectTransform, colX[c], 130f, colW[c], 36f);
            }
            int rows = Mathf.Max(1, settings.leaderboardSize);
            var cols = new Text[5][];
            for (int c = 0; c < 5; c++) cols[c] = new Text[rows];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    var t = UIFactory.CreateText(lbPanel.transform, $"Row{r}_{headers[c]}", "", 32, Parchment, c == 1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                    PlaceTopLeft(t.rectTransform, colX[c], 185f + r * 66f, colW[c], 50f);
                    cols[c][r] = t;
                }
            }
            var empty = UIFactory.CreateText(lbPanel.transform, "Empty", "No runs yet. Be the first explorer.", 30, new Color(Parchment.r, Parchment.g, Parchment.b, 0.7f), TextAnchor.MiddleCenter, FontStyle.Italic);
            PlaceTop(empty.rectTransform, 0f, 320f, 900f, 60f);

            // ---- Settings
            var setPanel = OverlayPanel(canvas, "SettingsPanel", "SETTINGS", out var backSet);
            string[] labels = { "Mouse Sensitivity", "Master Volume", "SFX Volume" };
            var sliders = new Slider[3];
            var values = new Text[3];
            var opts = PlayerOptions.Current;
            float[] mins = { 0.25f, 0f, 0f };
            float[] maxs = { 3f, 1f, 1f };
            float[] starts = { opts.mouseSensitivity, opts.masterVolume, opts.sfxVolume };
            for (int i = 0; i < 3; i++)
            {
                float y = 170f + i * 110f;
                var label = UIFactory.CreateText(setPanel.transform, "Label_" + i, labels[i], 30, Parchment, TextAnchor.MiddleLeft);
                PlaceTopLeft(label.rectTransform, 90f, y, 300f, 50f);
                sliders[i] = UIFactory.CreateSlider(setPanel.transform, "Slider_" + i, mins[i], maxs[i], starts[i], Bar,
                    new Color(0.15f, 0.13f, 0.12f, 1f), Orange, Parchment);
                PlaceTopLeft((RectTransform)sliders[i].transform, 400f, y, 400f, 50f);
                values[i] = UIFactory.CreateText(setPanel.transform, "Value_" + i, "", 30, Orange, TextAnchor.MiddleRight);
                PlaceTopLeft(values[i].rectTransform, 810f, y, 110f, 50f);
            }

            // ---- Controller
            var controllerGo = new GameObject("Main Menu Controller");
            var ctrl = controllerGo.AddComponent<MainMenuController>();
            ctrl.settings = settings;
            ctrl.gameScene = GameSceneName;
            ctrl.mainPanel = mainImg.gameObject;
            ctrl.nameInput = nameInput;
            ctrl.welcomeText = welcome;
            ctrl.errorText = error;
            ctrl.difficultyButtons = diffButtons;
            ctrl.difficultySelected = Orange;
            ctrl.difficultyNormal = ButtonColor;
            ctrl.playButton = play;
            ctrl.howToButton = howTo;
            ctrl.leaderboardButton = leaderboard;
            ctrl.settingsButton = settingsBtn;
            ctrl.quitButton = quit;
            ctrl.howToPanel = howToPanel;
            ctrl.leaderboardPanel = lbPanel;
            ctrl.settingsPanel = setPanel;
            ctrl.backButtons = new[] { backHowTo, backLb, backSet };
            ctrl.lbRank = cols[0];
            ctrl.lbName = cols[1];
            ctrl.lbScore = cols[2];
            ctrl.lbTime = cols[3];
            ctrl.lbResult = cols[4];
            ctrl.lbEmptyText = empty;
            ctrl.sensitivitySlider = sliders[0];
            ctrl.masterSlider = sliders[1];
            ctrl.sfxSlider = sliders[2];
            ctrl.sensitivityValue = values[0];
            ctrl.masterValue = values[1];
            ctrl.sfxValue = values[2];
        }
    }
}
