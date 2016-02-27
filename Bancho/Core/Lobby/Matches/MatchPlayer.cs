using osuBancho.Core.Players;

namespace osuBancho.Core.Lobby.Matches
{
    internal class /*struct*/ MatchPlayer
    {
        internal readonly Player Player;

        internal bool SkipRequested;
        internal bool LoadFinished;
        internal bool PlayFinished;

        internal MatchPlayer(Player player)
        {
            this.Player = player;
            this.SkipRequested = this.LoadFinished = this.PlayFinished = false;
        }
    }
}
