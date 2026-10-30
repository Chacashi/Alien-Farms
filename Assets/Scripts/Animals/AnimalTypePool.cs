using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Object pool for a single <see cref="AnimalData"/> type. Instances are created inactive under a container;
    /// activation is done by <see cref="Animal.Spawn"/> after positioning.
    /// </summary>
    public sealed class AnimalTypePool
    {
        private readonly AnimalData _data;
        private readonly Transform _container;
        private readonly Bounds _confinement;
        private readonly IZoneResolver _zones;
        private readonly float _killHeight;
        private readonly ObjectPool<Animal> _pool;

        public AnimalTypePool(
            AnimalData data,
            Transform container,
            Bounds confinement,
            IZoneResolver zones,
            float killHeight,
            int defaultCapacity,
            int maxSize)
        {
            _data = data;
            _container = container;
            _confinement = confinement;
            _zones = zones;
            _killHeight = killHeight;

            int max = Mathf.Max(1, maxSize);
            _pool = new ObjectPool<Animal>(
                CreateAnimal,
                null,
                OnReleaseAnimal,
                OnDestroyAnimal,
                Application.isEditor,
                Mathf.Clamp(defaultCapacity, 1, max),
                max);
        }

        public AnimalData Data => _data;
        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;

        public Animal Get() => _pool.Get();

        public void Release(Animal animal) => _pool.Release(animal);

        /// <summary>Creates instances up front so gameplay never instantiates. Only call at load time.</summary>
        public void Prewarm(int count)
        {
            int missing = count - _pool.CountAll;
            if (missing <= 0)
            {
                return;
            }

            // One-off temporary buffer at load time.
            Animal[] buffer = new Animal[missing];
            for (int i = 0; i < missing; i++)
            {
                buffer[i] = _pool.Get();
            }

            for (int i = 0; i < missing; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        private Animal CreateAnimal()
        {
            Animal animal = Object.Instantiate(_data.Prefab, _container);
            animal.gameObject.SetActive(false);
            animal.Initialize(_data, _confinement, this, _zones, _killHeight);
            return animal;
        }

        private static void OnReleaseAnimal(Animal animal) => animal.gameObject.SetActive(false);

        private static void OnDestroyAnimal(Animal animal)
        {
            if (animal != null)
            {
                Object.Destroy(animal.gameObject);
            }
        }
    }
}
