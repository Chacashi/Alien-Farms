using System;
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
            }

            _localPlayer = player;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
