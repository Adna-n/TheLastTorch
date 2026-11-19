// MainMenuController: runs the main menu page. The explorer types a name (1-12 characters,
// remembered between sessions) and sees "Welcome back... Best · Wins" or "New explorer", picks a
// difficulty, and can open How to Play, Leaderboard (top 5) and Settings (mouse sensitivity,
// master and SFX volume). Play stores the name/difficulty in GameSession and loads the level.
// All widgets are built by SceneBuilder; this script only wires up their behaviour.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTorch
{
    public class MainMenuController : MonoBehaviour
    {
        public GameSettings settings;
        [Tooltip("Scene loaded by the Play button.")]
        public string gameScene = "Temple";

        [Header("Main panel")]
        public GameObject mainPanel;
        public InputField nameInput;
        public Text welcomeText;
        public Text errorText;
        public Button[] difficultyButtons = new Button[3];
        public Button playButton;
        public Button howToButton;
        public Button leaderboardButton;
        public Button settingsButton;
        public Button quitButton;

        [Header("Overlay panels")]
        public GameObject howToPanel;
        public GameObject leaderboardPanel;
        public GameObject settingsPanel;
        public Button[] backButtons;

        [Header("Leaderboard rows")]
        public Text[] lbRank;
        public Text[] lbName;
        public Text[] lbScore;
        public Text[] lbTime;
        public Text[] lbResult;
        public Text lbEmptyText;

        [Header("Settings")]
        public Slider sensitivitySlider;
        public Slider masterSlider;
        public Slider sfxSlider;
        public Text sensitivityValue;
        public Text masterValue;
        public Text sfxValue;

        [Header("Colours")]
        public Color difficultySelected = new Color(1f, 0.6f, 0.24f);
        public Color difficultyNormal = new Color(0.22f, 0.2f, 0.18f);

        Difficulty difficulty;

        void Awake()
        {
            GameManager.RegisterSettings(settings);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Start()
        {
            var s = GameManager.Settings;
            var options = PlayerOptions.Current;

            nameInput.characterLimit = s.maxNameLength;
            nameInput.text = ProfileManager.LastName;
            nameInput.onValueChanged.AddListener(_ => { errorText.text = string.Empty; RefreshWelcome(); });

            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var d = (Difficulty)i;
                difficultyButtons[i].onClick.AddListener(() => SetDifficulty(d));
            }
            SetDifficulty(options.lastDifficulty);

            playButton.onClick.AddListener(Play);
            howToButton.onClick.AddListener(() => ShowPanel(howToPanel));
            leaderboardButton.onClick.AddListener(() => { FillLeaderboard(); ShowPanel(leaderboardPanel); });
            settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel));
            quitButton.onClick.AddListener(Quit);
            foreach (var b in backButtons) b.onClick.AddListener(Back);

            sensitivitySlider.value = options.mouseSensitivity;
            masterSlider.value = options.masterVolume;
            sfxSlider.value = options.sfxVolume;
            sensitivitySlider.onValueChanged.AddListener(v => { PlayerOptions.Current.mouseSensitivity = v; RefreshSettingsLabels(); });
            masterSlider.onValueChanged.AddListener(v => { PlayerOptions.Current.masterVolume = v; PlayerOptions.Apply(); RefreshSettingsLabels(); });
            sfxSlider.onValueChanged.AddListener(v => { PlayerOptions.Current.sfxVolume = v; RefreshSettingsLabels(); });
            RefreshSettingsLabels();

            errorText.text = string.Empty;
            RefreshWelcome();
            ShowPanel(null);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayLoop(SoundId.AmbientDrone);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame && !mainPanel.activeSelf) Back();
            if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) && mainPanel.activeSelf) Play();
        }

        void SetDifficulty(Difficulty d)
        {
            difficulty = d;
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var img = difficultyButtons[i].targetGraphic;
                img.color = i == (int)d ? difficultySelected : difficultyNormal;
                var label = difficultyButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.color = i == (int)d ? Color.black : new Color(0.9f, 0.85f, 0.75f);
            }
        }

        string CleanName() => nameInput.text == null ? string.Empty : nameInput.text.Trim();

        void RefreshWelcome()
        {
            string name = CleanName();
            if (name.Length == 0)
            {
                welcomeText.text = "Enter your name, explorer.";
                return;
            }
            var p = ProfileManager.Find(name);
            if (p != null && p.attempts > 0)
            {
                string best = p.bestEscapeTime > 0f ? $"  ·  Fastest: {LeaderboardEntry.FormatTime(p.bestEscapeTime)}" : string.Empty;
                welcomeText.text = $"Welcome back, {p.name}.\nBest: {p.bestScore:N0}  ·  Wins: {p.wins}{best}";
            }
            else welcomeText.text = "New explorer";
        }

        void Play()
        {
            string name = CleanName();
            int max = GameManager.Settings.maxNameLength;
            if (name.Length < 1 || name.Length > max)
            {
                errorText.text = $"Enter a name (1-{max} characters).";
                nameInput.ActivateInputField();
                return;
            }

            GameSession.PlayerName = name;
            GameSession.Difficulty = difficulty;
            ProfileManager.LastName = name;
            PlayerOptions.Current.lastDifficulty = difficulty;
            PlayerOptions.Save();

            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                errorText.text = $"Scene '{gameScene}' is not built yet.";
                Debug.LogError($"[MainMenu] Scene '{gameScene}' is not in Build Settings. Run The Last Torch > 2. Build Scenes.");
                return;
            }
            SceneManager.LoadScene(gameScene);
        }

        void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == null);
            howToPanel.SetActive(panel == howToPanel);
            leaderboardPanel.SetActive(panel == leaderboardPanel);
            settingsPanel.SetActive(panel == settingsPanel);
        }

        void Back()
        {
            if (settingsPanel.activeSelf) PlayerOptions.Save();
            ShowPanel(null);
            RefreshWelcome();
        }

        void FillLeaderboard()
        {
            IReadOnlyList<LeaderboardEntry> entries = ProfileManager.Leaderboard;
            for (int i = 0; i < lbRank.Length; i++)
            {
                bool has = i < entries.Count;
                var e = has ? entries[i] : null;
                lbRank[i].text = has ? (i + 1) + "." : string.Empty;
                lbName[i].text = has ? e.name : string.Empty;
                lbScore[i].text = has ? e.score.ToString("N0") : string.Empty;
                lbTime[i].text = has ? LeaderboardEntry.FormatTime(e.time) : string.Empty;
                lbResult[i].text = has ? (e.escaped ? "ESCAPED" : "LOST") : string.Empty;
                lbResult[i].color = has && e.escaped ? new Color(1f, 0.82f, 0.35f) : new Color(0.85f, 0.3f, 0.25f);
            }
            lbEmptyText.gameObject.SetActive(entries.Count == 0);
        }

        void RefreshSettingsLabels()
        {
            sensitivityValue.text = sensitivitySlider.value.ToString("0.00") + "x";
            masterValue.text = Mathf.RoundToInt(masterSlider.value * 100f) + "%";
            sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        }

        void Quit()
        {
            PlayerOptions.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
