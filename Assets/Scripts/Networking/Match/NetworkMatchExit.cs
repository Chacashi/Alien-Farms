using AlienFarms.Core;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Online rematch (server side). Each player's "Rematch" button sets its own <c>ReadyForRematch</c> (an
    /// owner-written NetworkVariable on its NetworkPlayer, see <see cref="OnlineMatchExitHandler"/>). On the server,
    /// once both players are ready and the results input delay has passed, the match is restarted (no scene change);
    /// owners clear their flag when the phase leaves Results. Returning to the title stays local (exit handler).
    /// Lives under OnlineModeRoot; does nothing off the server.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkMatchExit : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (server check, players by seat).")]
        private NetworkRuntimeAnchor _anchor;

        [Header("References")]
        [SerializeField, Tooltip("Match restarted by the rematch (server only).")]
        private MatchManager _match;

        /// <summary>True on the server while both players are ready for a rematch.</summary>
        public bool BothReady => IsReady(PlayerId.Player1) && IsReady(PlayerId.Player2);

        private void Update()
        {
            if (_anchor == null || _match == null || !_anchor.IsServer || _match.CurrentPhase != MatchPhase.Results
                || !_match.IsRematchReady || !BothReady)
            {
                return;
            }

            Debug.Log($"[{nameof(NetworkMatchExit)}] Both players ready: rematch.", this);
            _match.RequestRematch();
        }

        /// <summary>A player counts as ready only while its avatar is spawned and its owner is still connected.</summary>
        private bool IsReady(PlayerId id)
        {
            if (_anchor == null || !_anchor.TryGetPlayer(id, out NetworkPlayer player) || !player.IsSpawned || !player.IsReadyForRematch)
            {
                return false;
            }

            NetworkManager manager = _anchor.Manager;
            return manager != null && manager.IsServer && manager.ConnectedClients.ContainsKey(player.OwnerClientId);
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkMatchExit)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_match == null)
            {
                Debug.LogWarning($"[{nameof(NetworkMatchExit)}] Missing MatchManager on '{name}'.", this);
            }
        }
    }
}
