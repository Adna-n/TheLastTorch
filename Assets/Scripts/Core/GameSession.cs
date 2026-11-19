// GameSession: data carried from the Main Menu into the Temple scene (player name + difficulty).
// Static fields survive scene loads, so the menu sets them and the level reads them.
namespace LastTorch
{
    public enum Difficulty { Easy, Normal, Hard }

    public static class GameSession
    {
        public static string PlayerName = "Explorer";
        public static Difficulty Difficulty = Difficulty.Normal;
    }
}
