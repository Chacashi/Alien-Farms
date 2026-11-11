using AlienFarms.Core;

namespace AlienFarms.Player
{
    /// <summary>
    /// Registry of the players in the match, as the match drives them. Local: <see cref="PlayerJoinCoordinator"/>;
    /// online, a network roster on the server.
    /// </summary>
    public interface IPlayerRoster
    {
        /// <summary>Players currently registered (joined and configured).</summary>
        int JoinedCount { get; }

        bool TryGetRig(PlayerId id, out PlayerRig rig);

        /// <summary>Drops carried objects and moves every registered player back to its spawn point.</summary>
        void RespawnAll();

        /// <summary>Freezes (false) or unfreezes (true) every player; also applied to players registered later.</summary>
        void SetControlsEnabled(bool enabled);
    }
}
