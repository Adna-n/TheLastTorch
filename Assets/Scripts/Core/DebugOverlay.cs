// DebugOverlay: a TEMPORARY on-screen readout for testing before the real HUD exists (Phase 5).
// Shows state, fuel, hearts, keys, score, door hints, the collapse timer and the intro story. Debug keys (only if GameSettings.enableDebugKeys):
// H = hurt yourself, R = restart the scene, M = main menu. Remove it from scenes once the HUD is in.
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTorch
{
    public class DebugOverlay : MonoBehaviour
    {
        TorchSystem torch;
        PlayerHealth health;
        Inventory inventory;
        GUIStyle style;

        void Start()
        {
            torch = FindAnyObjectByType<TorchSystem>();
            health = FindAnyObjectByType<PlayerHealth>();
            inventory = FindAnyObjectByType<Inventory>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !GameManager.Settings.enableDebugKeys) return;

            if (kb.hKey.wasPressedThisFrame && health != null)
                health.TakeDamage(1, health.transform.position + health.transform.forward);
            if (kb.rKey.wasPressedThisFrame && GameManager.Instance != null)
                GameManager.Instance.RestartLevel();
            if (kb.mKey.wasPressedThisFrame && GameManager.Instance != null)
                GameManager.Instance.LoadMainMenu();
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
                style.normal.textColor = Color.white;
            }

            var gm = GameManager.Instance;
            string text = "TEST OVERLAY (temporary)\n";
            if (gm != null) text += $"State: {gm.State}   Time: {gm.RunTime:0.0}s\n";
            if (torch != null) text += $"Fuel: {torch.Fuel:0.0} ({torch.FuelPercent * 100f:0}%){(torch.IsLowFuel ? "  LOW!" : "")}\n";
            if (health != null) text += $"Hearts: {health.Hearts}/{GameManager.Settings.maxHearts}{(health.IsInvincible ? "  (invincible)" : "")}\n";
            if (inventory != null) text += $"Keys: Sun {inventory.HasSun}  Moon {inventory.HasMoon}  Serpent {inventory.HasSerpent}\n";
            if (ScoreManager.Instance != null) text += $"Score: {ScoreManager.Instance.Total}\n";
            text += $"Player: {GameSession.PlayerName}  ({GameSession.Difficulty})\n";
            if (GameManager.Settings.enableDebugKeys) text += "[H] hurt  [R] restart  [M] main menu  [Esc] pause\n";
            if (gm != null && gm.IsGameOver) text += $"\n{(gm.State == GameState.Won ? "WON" : "LOST")}: {gm.GameOverReason}\nPress R to restart or M for the main menu";

            GUI.Label(new Rect(20, 20, 800, 400), text, style);

            if (bigStyle == null)
            {
                bigStyle = new GUIStyle(style) { fontSize = 34, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                bigStyle.normal.textColor = new Color(1f, 0.75f, 0.45f);
            }
            float w = Screen.width;
            if (gm != null && gm.State == GameState.Intro)
                GUI.Label(new Rect(0, Screen.height * 0.35f, w, 200),
                    "The map led you here. The door slammed shut behind you.\nYour torch won't last forever.\nFind the Heart of Eternity, and find a way out.\n\n(press any key)", bigStyle);
            if (gm != null && gm.ActiveHint != null)
                GUI.Label(new Rect(0, Screen.height - 160, w, 80), gm.ActiveHint, bigStyle);
            var collapse = CollapseSequence.Instance;
            if (collapse != null && collapse.IsActive && gm != null && !gm.IsGameOver)
                GUI.Label(new Rect(0, 20, w, 60), $"COLLAPSE: {Mathf.CeilToInt(collapse.TimeLeft)}s", bigStyle);
        }

        GUIStyle bigStyle;
    }
}
