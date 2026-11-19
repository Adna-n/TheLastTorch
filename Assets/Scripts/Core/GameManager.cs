// GameManager: the "referee" of a scene. Holds the GameSettings, the game state
// (Intro -> Playing -> Paused -> Collapse -> Won/Lost), the run timer and the difficulty multiplier.
// Other scripts ask it "is the game running?" and call Win()/Lose(). Esc toggles pause.
// Raises OnStateChanged and OnGameOver(won, reason) so UI and audio can react, and saves each
// run to the player profile/leaderboard (ProfileManager).
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LastTorch
{
    public enum GameState { Intro, Playing, Paused, Collapse, Won, Lost }

    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Assets/Settings/GameSettings.asset")]
        public GameSettings settings;
        [Tooltip("If off, the game starts in Playing immediately (used by the test scene).")]
        public bool playIntro = true;
        public string mainMenuScene = "MainMenu";

        [Tooltip("Count attempts and save results to the player profile / leaderboard.")]
        public bool recordProfile = true;

        static GameSettings registeredSettings;
        static GameSettings fallbackSettings;

        /// <summary>Lets scenes without a GameManager (the main menu) supply the settings asset.</summary>
        public static void RegisterSettings(GameSettings value)
        {
            if (value != null) registeredSettings = value;
        }

        /// <summary>The active tunables. Falls back to default values if no asset is assigned.</summary>
        public static GameSettings Settings
        {
            get
            {
                if (Instance != null && Instance.settings != null) return Instance.settings;
                if (registeredSettings != null) return registeredSettings;
                if (fallbackSettings == null)
                {
                    fallbackSettings = ScriptableObject.CreateInstance<GameSettings>();
                    Debug.LogWarning("[GameManager] No GameSettings asset found, using default values.");
                }
                return fallbackSettings;
            }
        }

        public GameState State { get; private set; } = GameState.Intro;
        public float RunTime { get; private set; }
        public string GameOverReason { get; private set; }
        public float DifficultyMultiplier => Settings.GetDifficultyMultiplier(GameSession.Difficulty);

        /// <summary>True while the player can move and the torch burns.</summary>
        public bool IsGameplayActive => State == GameState.Playing || State == GameState.Collapse;
        public bool IsGameOver => State == GameState.Won || State == GameState.Lost;

        public event Action<GameState> OnStateChanged;
        public event Action<bool, string> OnGameOver;

        GameState stateBeforePause;
        float introTimer;

        void Awake()
        {
            Instance = this;
            RegisterSettings(settings);
            Time.timeScale = 1f;
            State = GameState.Intro;
        }

        void Start()
        {
            ApplyLighting();
            PlayerOptions.Apply();
            if (recordProfile) ProfileManager.StartRun(GameSession.PlayerName);
            if (!playIntro) SetState(GameState.Playing);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            if (State == GameState.Intro)
            {
                introTimer += Time.deltaTime;
                bool skip = (keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
                            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                if (introTimer >= Settings.introDuration || skip) EndIntro();
                return;
            }

            if (IsGameplayActive) RunTime += Time.deltaTime;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsGameplayActive) Pause();
                else if (State == GameState.Paused) Resume();
            }
        }

        /// <summary>Applies the dark ambient light and fog values from GameSettings.</summary>
        public void ApplyLighting()
        {
            RenderSettings.ambientLight = Settings.ambientColor;
            RenderSettings.fogDensity = Settings.fogDensity;
        }

        public void EndIntro()
        {
            if (State == GameState.Intro) SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (!IsGameplayActive) return;
            stateBeforePause = State;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(stateBeforePause);
        }

        /// <summary>Called when the Heart is taken: switches to the escape phase.</summary>
        public void BeginCollapse()
        {
            if (State == GameState.Playing) SetState(GameState.Collapse);
        }

        public void Win(string reason) => EndGame(true, reason);
        public void Lose(string reason) => EndGame(false, reason);

        void EndGame(bool won, string reason)
        {
            if (IsGameOver) return;
            Time.timeScale = 1f;
            GameOverReason = reason;
            SetState(won ? GameState.Won : GameState.Lost);
            Debug.Log($"[GameManager] {(won ? "WON" : "LOST")}: {reason}  (time {RunTime:0.0}s)");

            if (recordProfile)
            {
                var score = ScoreManager.Instance;
                ProfileManager.RecordRun(GameSession.PlayerName, score != null ? score.Total : 0, RunTime, won,
                    score != null ? score.GemsCollected : 0);
            }
            OnGameOver?.Invoke(won, reason);
        }

        void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
