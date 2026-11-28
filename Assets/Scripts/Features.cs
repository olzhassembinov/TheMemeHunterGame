namespace MemeHunter
{
    /// <summary>
    /// Parts of the designed UI that are built but switched off because the server has nothing behind them yet.
    /// Flip a value to bring the part back; the code is kept.
    /// </summary>
    public static class Features
    {
        /// <summary>Notifications page: shows sample messages only, the server sends no notifications.</summary>
        public static readonly bool Notifications = false;

        /// <summary>Weekly league with simulated rivals (Game/Progress/LocalLeague). The server has a global XP leaderboard.</summary>
        public static readonly bool WeeklyLeague = false;

        /// <summary>"Price: N$" on the meme card: nothing can be bought with money.</summary>
        public static readonly bool MemePrice = false;

        /// <summary>Donate buttons: donations are not open yet (planned for a later build). Hidden in the beta build.</summary>
        public static readonly bool Donate = !Net.ApiConfig.BetaRelease;
    }
}
