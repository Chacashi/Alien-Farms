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

        /// <summary>
        /// True when a player missing during Playing ends the round as a forfeit won by the one who stayed (online);
        /// false aborts the round back to WaitingForPlayers (local).
        /// </summary>
        bool ForfeitOnPlayerMissing { get; }

        /// <summary>Winner of the current round by forfeit; <see cref="PlayerId.None"/> unless it ended that way.</summary>
        PlayerId ForfeitWinner { get; }

        /// <summary>True when <paramref name="id"/> is joined and registered.</summary>
        bool IsPlayerPresent(PlayerId id);

        /// <summary>
        /// Ends the round in Results as a forfeit won by <paramref name="winner"/> (the score does not decide it).
        /// Only valid from Playing; ignored otherwise.
        /// </summary>
        void EndByForfeit(PlayerId winner);

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
