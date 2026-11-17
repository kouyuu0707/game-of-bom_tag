namespace BombTag.Online
{
    public enum GameMode : byte { SinglePlayer, Cooperative }

    public static class GameModeRules
    {
        public static int RequiredPlayers(GameMode mode) => mode == GameMode.SinglePlayer ? 1 : 2;
        public static bool CanStartRound(GameMode mode, int players) => players == RequiredPlayers(mode);
        public static bool CanJoin(GameMode mode, bool isLocalPlayer) =>
            mode != GameMode.SinglePlayer || isLocalPlayer;
    }
}
