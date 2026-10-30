using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Match
{
    /// <summary>What match states may read and do. Implemented by MatchManager (and by test fakes).</summary>
    public interface IMatchContext
    {
        MatchSettings Settings { get; }

        /// <summary>Players currently joined and registered.</summary>
        int JoinedPlayerCount { get; }

        MatchTimer Timer { get; }

        MatchScore Score { get; }

        void ChangeState(MatchPhase next);

        /// <summary>
        /// Clears score and timer and repopulates animals; optionally moves players back to their spawns and, when
        /// respawning players, rolls a new obstacle layout.
        /// </summary>
        void ResetRound(bool respawnPlayers);

        /// <summary>Freezes (false) or unfreezes (true) movement and grabbing of every player. Camera stays free.</summary>
        void SetPlayerControlsEnabled(bool enabled);
    }
}
