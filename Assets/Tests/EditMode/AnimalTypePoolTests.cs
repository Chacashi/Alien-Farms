using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AlienFarms.Tests
{
    /// <summary>Pool release guard and replica despawn paths, on clones of the real sheep prefab (no physics step).</summary>
    public sealed class AnimalTypePoolTests
    {
        private const string SheepDataPath = "Assets/ScriptableObjects/Animals/AnimalData_Sheep.asset";

        private GameObject _container;
        private AnimalData _data;
        private AnimalTypePool _pool;

        [SetUp]
        public void SetUp()
        {
            // Read-only use of the asset: the pool only reads its prefab and tuning.
            _data = AssetDatabase.LoadAssetAtPath<AnimalData>(SheepDataPath);
            Assert.That(_data, Is.Not.Null, $"Missing {SheepDataPath}.");
            Assert.That(_data.Prefab, Is.Not.Null, "Sheep AnimalData has no prefab.");

            _container = new GameObject("AnimalContainer");
            _pool = new AnimalTypePool(_data, _container.transform, new Bounds(Vector3.zero, Vector3.one * 20f), null, -10f, 1, 4);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_container);
        }

        [Test]
        public void Release_Twice_IsIgnored()
        {
            Animal animal = _pool.Get();
            animal.Spawn(Vector3.zero, Quaternion.identity);

            _pool.Release(animal);
            Assert.DoesNotThrow(() => _pool.Release(animal));

            Assert.That(_pool.CountInactive, Is.EqualTo(1));
            Assert.That(_pool.CountActive, Is.EqualTo(0));
        }

        [Test]
        public void Despawn_AfterRelease_IsIgnored()
        {
            Animal animal = _pool.Get();
            animal.Spawn(Vector3.zero, Quaternion.identity);
            animal.Despawn();

            Assert.DoesNotThrow(() => _pool.Release(animal));
            Assert.That(_pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void ReplicatedInactive_ReturnsReplicaToPool()
        {
            Animal animal = _pool.GetForReplica(Vector3.zero, Quaternion.identity);
            Assert.That(animal.Role, Is.EqualTo(SimulationRole.Replica));

            animal.ApplyReplicatedState(AnimalState.Inactive, PlayerId.None, PlayerId.None);

            Assert.That(animal.State, Is.EqualTo(AnimalState.Inactive));
            Assert.That(_pool.CountInactive, Is.EqualTo(1));
            Assert.DoesNotThrow(animal.Despawn);
            Assert.That(_pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void ApplyReplicatedState_OnAuthority_IsIgnored()
        {
            Animal animal = _pool.Get();
            animal.Spawn(Vector3.zero, Quaternion.identity);

            animal.ApplyReplicatedState(AnimalState.Captured, PlayerId.Player1, PlayerId.None);

            Assert.That(animal.State, Is.EqualTo(AnimalState.Wandering));
            Assert.That(animal.Owner, Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void ApplyReplicatedState_OnReplica_RaisesStateChanged()
        {
            Animal animal = _pool.GetForReplica(Vector3.zero, Quaternion.identity);
            int calls = 0;
            animal.StateChanged += _ => calls++;

            animal.ApplyReplicatedState(AnimalState.Captured, PlayerId.Player2, PlayerId.None);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(animal.State, Is.EqualTo(AnimalState.Captured));
            Assert.That(animal.Owner, Is.EqualTo(PlayerId.Player2));
        }
    }
}
