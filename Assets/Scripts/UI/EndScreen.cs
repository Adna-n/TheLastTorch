// EndScreen: shown when the run ends. A white fade (win) or dark fade (loss) plays first, then
// "ESCAPED!" (gold) or "LOST" (red), the reason line, a score breakdown table (gems, oil, keys,
// heart, damage, escape bonus, torch bonus, TOTAL), the run time and "NEW HIGH SCORE!" when beaten.
// Buttons: Retry and Main Menu. Build() creates the widgets (called by SceneBuilder).
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastTorch
{
    public class EndScreen : MonoBehaviour
    {
        public Image fadeImage;
        public GameObject panel;
        public Text titleText;
        public Text reasonText;
        public Text[] rowLabels;
        public Text[] rowValues;
        public Text timeText;
        public Text highScoreText;
        public Button retryButton;
        public Button menuButton;

        const int Rows = 8;

        public void Build(UISkin skin, Transform canvas)
        {
            fadeImage = UIFactory.CreateImage(canvas, "ScreenFade", null, new Color(1f, 1f, 1f, 0f));
            UIFactory.Stretch(fadeImage.rectTransform);

            var backdrop = UIFactory.CreateImage(canvas, "EndScreen", null, new Color(0f, 0f, 0f, 0.7f), true);
            UIFactory.Stretch(backdrop.rectTransform);
            panel = backdrop.gameObject;

            var box = UIFactory.CreateImage(backdrop.transform, "Box", skin.bar, UISkin.Panel, true);
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f));

            titleText = UIFactory.CreateText(box.transform, "Title", "ESCAPED!", 96, UISkin.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(860f, 120f));
            UIFactory.AddGlow(titleText, new Color(0f, 0f, 0f, 0.6f), 3f);
            reasonText = UIFactory.CreateText(box.transform, "Reason", "", 30, UISkin.Parchment, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIFactory.Place(reasonText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(840f, 50f));

            rowLabels = new Text[Rows];
            rowValues = new Text[Rows];
            for (int i = 0; i < Rows; i++)
            {
                bool total = i == Rows - 1;
                float y = -230f - i * 52f - (total ? 16f : 0f);
                rowLabels[i] = UIFactory.CreateText(box.transform, "Label" + i, "", total ? 36 : 30, total ? UISkin.Orange : UISkin.Parchment,
                    TextAnchor.MiddleLeft, total ? FontStyle.Bold : FontStyle.Normal);
                UIFactory.Place(rowLabels[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(-160f, y), new Vector2(460f, 48f));
                rowValues[i] = UIFactory.CreateText(box.transform, "Value" + i, "", total ? 36 : 30, total ? UISkin.Orange : UISkin.Parchment,
                    TextAnchor.MiddleRight, total ? FontStyle.Bold : FontStyle.Normal);
                UIFactory.Place(rowValues[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, y), new Vector2(240f, 48f));
            }
            var line = UIFactory.CreateImage(box.transform, "TotalLine", null, new Color(1f, 0.6f, 0.24f, 0.5f));
            UIFactory.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -230f - (Rows - 1) * 52f + 4f), new Vector2(700f, 2f));

            timeText = UIFactory.CreateText(box.transform, "Time", "", 28, UISkin.Parchment);
            UIFactory.Place(timeText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(800f, 40f));
            highScoreText = UIFactory.CreateText(box.transform, "HighScore", "NEW HIGH SCORE!", 40, UISkin.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(highScoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(800f, 50f));

            retryButton = UIFactory.CreateButton(box.transform, "Retry", "RETRY", skin.bar, UISkin.Orange, Color.black, 32);
            UIFactory.Place((RectTransform)retryButton.transform, new Vector2(0.5f, 0f), new Vector2(-170f, 36f), new Vector2(300f, 70f));
            menuButton = UIFactory.CreateButton(box.transform, "Menu", "MAIN MENU", skin.bar, UISkin.ButtonColor, UISkin.Parchment, 32);
            UIFactory.Place((RectTransform)menuButton.transform, new Vector2(0.5f, 0f), new Vector2(170f, 36f), new Vector2(300f, 70f));
        }

        void Start()
        {
            panel.SetActive(false);
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnGameOver += OnGameOver;
            retryButton.onClick.AddListener(gm.RestartLevel);
            menuButton.onClick.AddListener(gm.LoadMainMenu);
        }

        void OnGameOver(bool won, string reason) => StartCoroutine(Show(won, reason));

        IEnumerator Show(bool won, string reason)
        {
            var s = GameManager.Settings;
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.StopLoop();
                audio.Play(won ? SoundId.WinJingle : SoundId.LoseSting);
            }

            Color fadeColor = won ? Color.white : Color.black;
            float duration = won ? s.winFadeTime : s.loseFadeTime;
            yield return Fade(fadeColor, 0f, 1f, duration);

            Fill(won, reason);
            panel.SetActive(true);
            yield return Fade(fadeColor, 1f, 0f, duration * 0.6f);
        }

        IEnumerator Fade(Color color, float from, float to, float duration)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration))
            {
                color.a = Mathf.Lerp(from, to, t);
                fadeImage.color = color;
                yield return null;
            }
            color.a = to;
            fadeImage.color = color;
        }

        void Fill(bool won, string reason)
        {
            titleText.text = won ? "ESCAPED!" : "LOST";
            titleText.color = won ? UISkin.Gold : UISkin.Danger;
            reasonText.text = reason;

            var score = ScoreManager.Instance;
            int P(ScoreCategory c) => score != null ? score.GetPoints(c) : 0;
            int N(ScoreCategory c) => score != null ? score.GetCount(c) : 0;
            string Pts(int v) => (v > 0 ? "+" : "") + v.ToString("N0");

            Set(0, $"Gems  x{N(ScoreCategory.Gems)}", Pts(P(ScoreCategory.Gems)));
            Set(1, $"Oil jars  x{N(ScoreCategory.Oil)}", Pts(P(ScoreCategory.Oil)));
            Set(2, $"Keys  x{N(ScoreCategory.Keys)}", Pts(P(ScoreCategory.Keys)));
            Set(3, "Heart of Eternity", Pts(P(ScoreCategory.Heart)));
            Set(4, $"Damage  x{N(ScoreCategory.Damage)}", Pts(P(ScoreCategory.Damage)));
            Set(5, "Escape bonus", won ? Pts(P(ScoreCategory.EscapeBonus)) : "-");
            Set(6, "Torch bonus", won ? Pts(P(ScoreCategory.TorchBonus)) : "-");
            Set(7, "TOTAL", (score != null ? score.Total : 0).ToString("N0"));

            float time = GameManager.Instance != null ? GameManager.Instance.RunTime : 0f;
            timeText.text = $"Time  {LeaderboardEntry.FormatTime(time)}   ·   {GameSession.PlayerName}   ·   {GameSession.Difficulty}";
            highScoreText.gameObject.SetActive(ProfileManager.LastRunWasHighScore);
        }

        void Set(int row, string label, string value)
        {
            rowLabels[row].text = label;
            rowValues[row].text = value;
        }
    }
}
