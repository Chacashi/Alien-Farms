using AlienFarms.Core;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Replicated "inventory" of each pen: one <see cref="NetworkList{T}"/> of <see cref="PenEntry"/> per player, on the
    /// in-scene MatchNetwork object (inert in local mode). Server: kept up to date from
    /// <see cref="GameEvents.OnPenInventoryChanged"/>. Pure clients: raise that same event from <c>OnListChanged</c> (and
    /// once per entry on spawn, so a late joiner sees every pen).
    /// The lists are created with the field (NGO requirement), never in OnNetworkSpawn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkPenLedger : NetworkBehaviour
    {
        private readonly NetworkList<PenEntry> _player1Pen = new NetworkList<PenEntry>();
        private readonly NetworkList<PenEntry> _player2Pen = new NetworkList<PenEntry>();

        private bool _clientListening;

        /// <summary>Animals of type <paramref name="animalTypeIndex"/> in <paramref name="playerId"/>'s pen, as replicated.</summary>
        public int GetCount(PlayerId playerId, int animalTypeIndex)
        {
            NetworkList<PenEntry> pen = GetPen(playerId);
            if (pen == null)
            {
                return 0;
            }

            int index = IndexOf(pen, animalTypeIndex);
            return index >= 0 ? pen[index].Count : 0;
        }

        private void OnEnable()
        {
            GameEvents.OnPenInventoryChanged += HandlePenInventoryChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPenInventoryChanged -= HandlePenInventoryChanged;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                return;
            }

            _player1Pen.OnListChanged += HandlePlayer1PenChanged;
            _player2Pen.OnListChanged += HandlePlayer2PenChanged;
            _clientListening = true;
            RaiseAll(PlayerId.Player1, _player1Pen);
            RaiseAll(PlayerId.Player2, _player2Pen);
        }

        public override void OnNetworkDespawn()
        {
            if (!_clientListening)
            {
                return;
            }

            _clientListening = false;
            _player1Pen.OnListChanged -= HandlePlayer1PenChanged;
            _player2Pen.OnListChanged -= HandlePlayer2PenChanged;
        }

        /// <summary>Server only (clients raise this event themselves from the lists).</summary>
        private void HandlePenInventoryChanged(PlayerId playerId, int animalTypeIndex, int count)
        {
            if (!IsSpawned || !IsServer || animalTypeIndex < 0 || animalTypeIndex > byte.MaxValue)
            {
                return;
            }

            NetworkList<PenEntry> pen = GetPen(playerId);
            if (pen == null)
            {
                return;
            }

            var entry = new PenEntry(animalTypeIndex, count);
            int index = IndexOf(pen, animalTypeIndex);
            if (index < 0)
            {
                pen.Add(entry);
            }
            else if (!pen[index].Equals(entry))
            {
                pen[index] = entry;
            }
        }

        private void HandlePlayer1PenChanged(NetworkListEvent<PenEntry> change) => RaiseChange(PlayerId.Player1, _player1Pen, change);

        private void HandlePlayer2PenChanged(NetworkListEvent<PenEntry> change) => RaiseChange(PlayerId.Player2, _player2Pen, change);

        private void RaiseChange(PlayerId playerId, NetworkList<PenEntry> pen, NetworkListEvent<PenEntry> change)
        {
            if (IsServer)
            {
                return;
            }

            switch (change.Type)
            {
                case NetworkListEvent<PenEntry>.EventType.Add:
                case NetworkListEvent<PenEntry>.EventType.Insert:
                case NetworkListEvent<PenEntry>.EventType.Value:
                    GameEvents.RaisePenInventoryChanged(playerId, change.Value.AnimalTypeIndex, change.Value.Count);
                    break;
                case NetworkListEvent<PenEntry>.EventType.Remove:
                case NetworkListEvent<PenEntry>.EventType.RemoveAt:
                    GameEvents.RaisePenInventoryChanged(playerId, change.Value.AnimalTypeIndex, 0);
                    break;
                default:
                    RaiseAll(playerId, pen);
                    break;
            }
        }

        private static void RaiseAll(PlayerId playerId, NetworkList<PenEntry> pen)
        {
            for (int i = 0; i < pen.Count; i++)
            {
                PenEntry entry = pen[i];
                GameEvents.RaisePenInventoryChanged(playerId, entry.AnimalTypeIndex, entry.Count);
            }
        }

        private static int IndexOf(NetworkList<PenEntry> pen, int animalTypeIndex)
        {
            for (int i = 0; i < pen.Count; i++)
            {
                if (pen[i].AnimalTypeIndex == animalTypeIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private NetworkList<PenEntry> GetPen(PlayerId playerId)
        {
            switch (playerId)
            {
                case PlayerId.Player1:
                    return _player1Pen;
                case PlayerId.Player2:
                    return _player2Pen;
                default:
                    return null;
            }
        }
    }
}
