using AlienFarms.Networking.Interest;
using AlienFarms.Networking.Player;
using Unity.Netcode;

namespace AlienFarms.Networking.Diagnostics
{
    public static class KnownObjectsCounter
    {
        public static KnownObjectsCounts Count(NetworkManager manager)
        {
            NetworkSpawnManager spawnManager = manager != null ? manager.SpawnManager : null;
            if (spawnManager == null)
            {
                return default;
            }

            int animals = 0;
            int bombs = 0;
            int other = 0;
            foreach (NetworkObject networkObject in spawnManager.SpawnedObjectsList)
            {
                if (!networkObject.TryGetComponent(out NetworkCarriable carriable))
                {
                    other++;
                    continue;
                }

                switch (carriable.InterestKind)
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
