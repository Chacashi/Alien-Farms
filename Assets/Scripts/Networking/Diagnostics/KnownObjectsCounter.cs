using System.Collections.Generic;
using AlienFarms.Networking.Interest;
using AlienFarms.Networking.Player;
using Unity.Netcode;

namespace AlienFarms.Networking.Diagnostics
{
    public sealed class KnownObjectsCounter
    {
        private readonly Dictionary<NetworkObject, InterestKind?> _kinds = new(128);
        private readonly List<NetworkObject> _removed = new(128);

        public void Clear() => _kinds.Clear();

        public KnownObjectsCounts Count(NetworkManager manager)
        {
            NetworkSpawnManager spawnManager = manager != null ? manager.SpawnManager : null;
            if (spawnManager == null)
            {
                Clear();
                return default;
            }

            HashSet<NetworkObject> spawned = spawnManager.SpawnedObjectsList;
            foreach (NetworkObject networkObject in _kinds.Keys)
            {
                if (!spawned.Contains(networkObject))
                {
                    _removed.Add(networkObject);
                }
            }

            foreach (NetworkObject networkObject in _removed)
            {
                _kinds.Remove(networkObject);
            }

            _removed.Clear();

            int animals = 0;
            int bombs = 0;
            int other = 0;
            foreach (NetworkObject networkObject in spawned)
            {
                if (!_kinds.TryGetValue(networkObject, out InterestKind? kind))
                {
                    kind = networkObject.TryGetComponent(out NetworkCarriable carriable) ? carriable.InterestKind : null;
                    _kinds.Add(networkObject, kind);
                }

                switch (kind)
                {
                    case InterestKind.Animal:
                        animals++;
                        break;
                    case InterestKind.Bomb:
                        bombs++;
                        break;
                    default:
                        other++;
                        break;
                }
            }

            return new KnownObjectsCounts(animals, bombs, other);
        }
    }
}
