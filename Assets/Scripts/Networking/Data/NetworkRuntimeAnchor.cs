using System;
using AlienFarms.Core;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Data
{
    /// <summary>
    /// Runtime anchor: the persistent NetworkRoot publishes its services here so scene objects (roster, bootstrap, dev
    /// panel) reach them through a serialized asset reference instead of Find* or singletons. Nothing is serialized;
    /// references from a previous play session are discarded through a generation counter (no domain reload).
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkRuntimeAnchor", menuName = "Alien Farms/Networking/Network Runtime Anchor (Runtime)")]
    public sealed class NetworkRuntimeAnchor : ScriptableObject
    {
        private static int s_generation;

        [NonSerialized] private int _generation = -1;
        [NonSerialized] private NetworkRootLifetime _root;
        [NonSerialized] private NetworkPlayer _localPlayer;
        [NonSerialized] private string _disconnectReason;

        // Spawned avatars by seat on this machine (index = (int)PlayerId - 1), on server and clients alike.
        [NonSerialized] private readonly NetworkPlayer[] _players = new NetworkPlayer[2];

        /// <summary>The published root, or null when none is alive in this play session.</summary>
        public NetworkRootLifetime Root => IsCurrent && _root != null ? _root : null;

        public bool HasRoot => Root != null;

        public NetworkManager Manager => HasRoot ? _root.Manager : null;

        public ConnectionApprovalHandler Approval => HasRoot ? _root.Approval : null;

        /// <summary>Seats of the current session (server side).</summary>
        public ServerSlotDirectory Slots => HasRoot && _root.Approval != null ? _root.Approval.Slots : null;

        public NetworkSceneLoader SceneLoader => HasRoot ? _root.SceneLoader : null;

        public IConnectionStrategy Connection => HasRoot ? _root.Connection : null;

        /// <summary>Avatar owned by this machine in the gameplay scene (null when none is spawned).</summary>
        public NetworkPlayer LocalPlayer => IsCurrent && _localPlayer != null ? _localPlayer : null;

        /// <summary>True on a running server (host included).</summary>
        public bool IsServer
        {
            get
            {
                NetworkManager manager = Manager;
                return manager != null && manager.IsListening && manager.IsServer;
            }
        }

        /// <summary>True on a running client that is not the server.</summary>
        public bool IsPureClient
        {
            get
            {
                NetworkManager manager = Manager;
                return manager != null && manager.IsListening && manager.IsClient && !manager.IsServer;
            }
        }

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            // Only scene objects reference this asset: keep its runtime state across scene loads.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        public void Publish(NetworkRootLifetime root)
        {
            if (!IsCurrent)
            {
                _localPlayer = null;
                _disconnectReason = null;
                ClearPlayers();
            }

            _generation = s_generation;
            _root = root;
        }

        /// <summary>Registers the avatar this machine owns (called by NetworkPlayer on the owner).</summary>
        public void SetLocalPlayer(NetworkPlayer player)
        {
            if (!IsCurrent)
            {
                _generation = s_generation;
                _root = null;
                ClearPlayers();
            }

            _localPlayer = player;
        }

        /// <summary>Registers a spawned avatar under its seat (called by NetworkPlayer once its PlayerId is known).</summary>
        public void RegisterPlayer(NetworkPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (!IsCurrent)
            {
                _generation = s_generation;
                _root = null;
                _localPlayer = null;
                _disconnectReason = null;
                ClearPlayers();
            }

            int index = (int)player.PlayerId - 1;
            if (index < 0 || index >= _players.Length)
            {
                return;
            }

            // A seat holds one avatar: drop any stale registration of this instance under another seat.
            UnregisterPlayer(player);
            _players[index] = player;
        }

        /// <summary>Removes <paramref name="player"/> from whatever seat it is registered under.</summary>
        public void UnregisterPlayer(NetworkPlayer player)
        {
            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == player)
                {
                    _players[i] = null;
                }
            }
        }

        /// <summary>Spawned avatar of <paramref name="id"/> on this machine.</summary>
        public bool TryGetPlayer(PlayerId id, out NetworkPlayer player)
        {
            int index = (int)id - 1;
            player = IsCurrent && index >= 0 && index < _players.Length && _players[index] != null ? _players[index] : null;
            return player != null;
        }

        /// <summary>
        /// Spawned avatar owned by NGO client <paramref name="clientId"/> on this machine. No allocation. Invariant: the
        /// host is always P1 (seat index 0), so the first seat wins for client 0 even while a leaving client's avatar is
        /// briefly owned by the server too (the roster unregisters that avatar right after the drop).
        /// </summary>
        public bool TryGetPlayerByClient(ulong clientId, out NetworkPlayer player)
        {
            if (IsCurrent)
            {
                for (int i = 0; i < _players.Length; i++)
                {
                    NetworkPlayer candidate = _players[i];
                    if (candidate != null && candidate.IsSpawned && candidate.OwnerClientId == clientId)
                    {
                        player = candidate;
                        return true;
                    }
                }
            }

            player = null;
            return false;
        }

        /// <summary>Unregisters <paramref name="player"/> when it is the registered local avatar.</summary>
        public void ClearLocalPlayer(NetworkPlayer player)
        {
            if (_localPlayer == player)
            {
                _localPlayer = null;
            }
        }

        /// <summary>Why this machine was dropped from the last session; shown by the menu once (see <see cref="ConsumeDisconnectReason"/>).</summary>
        public void SetDisconnectReason(string reason)
        {
            if (!IsCurrent)
            {
                _generation = s_generation;
                _root = null;
                _localPlayer = null;
                ClearPlayers();
            }

            _disconnectReason = reason;
        }

        /// <summary>Returns the pending disconnect reason (null when none) and clears it.</summary>
        public string ConsumeDisconnectReason()
        {
            if (!IsCurrent)
            {
                return null;
            }

            string reason = _disconnectReason;
            _disconnectReason = null;
            return reason;
        }

        /// <summary>Clears the anchor when <paramref name="root"/> is the published one.</summary>
        public void Clear(NetworkRootLifetime root)
        {
            if (_root == root)
            {
                _root = null;
            }
        }

        private void ClearPlayers()
        {
            for (int i = 0; i < _players.Length; i++)
            {
                _players[i] = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
