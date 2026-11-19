// PauseMenu: shown while the game is paused (Esc, handled by GameManager, which also sets
// Time.timeScale = 0). Buttons: Resume, Restart, Main Menu, plus a controls reminder.
// Build() creates the panel (called by SceneBuilder).
using UnityEngine;
using UnityEngine.UI;

namespace LastTorch
{
    public class PauseMenu : MonoBehaviour
    {
        public GameObject panel;
        public Button resumeButton;
        public Button restartButton;
        public Button menuButton;

        public void Build(UISkin skin, Transform canvas)
        {
            var dim = UIFactory.CreateImage(canvas, "PauseMenu", null, new Color(0f, 0f, 0f, 0.6f), true);
            UIFactory.Stretch(dim.rectTransform);
            panel = dim.gameObject;

            var box = UIFactory.CreateImage(dim.transform, "Box", skin.bar, UISkin.Panel, true);
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 600f));

            var title = UIFactory.CreateText(box.transform, "Title", "PAUSED", 64, UISkin.Orange, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(560f, 80f));

            resumeButton = Button(box.transform, skin, "RESUME", -140f, true);
            restartButton = Button(box.transform, skin, "RESTART", -230f, false);
            menuButton = Button(box.transform, skin, "MAIN MENU", -320f, false);

            var help = UIFactory.CreateText(box.transform, "Controls",
                "WASD / Arrows  move   ·   Mouse  look\nShift  sprint   ·   E  interact   ·   Esc  resume",
                22, new Color(UISkin.Parchment.r, UISkin.Parchment.g, UISkin.Parchment.b, 0.7f));
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(580f, 80f));
        }

        static Button Button(Transform parent, UISkin skin, string label, float y, bool primary)
        {
            var b = UIFactory.CreateButton(parent, label, label, skin.bar, primary ? UISkin.Orange : UISkin.ButtonColor,
                primary ? Color.black : UISkin.Parchment, 30);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(420f, 70f));
            return b;
        }

        void Start()
        {
            panel.SetActive(false);
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnStateChanged += state => panel.SetActive(state == GameState.Paused);
            resumeButton.onClick.AddListener(gm.Resume);
            restartButton.onClick.AddListener(gm.RestartLevel);
            menuButton.onClick.AddListener(gm.LoadMainMenu);
        }
    }
}
