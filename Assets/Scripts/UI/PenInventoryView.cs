using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Shows one player's pen inventory (count per animal type). Driven only by
    /// <see cref="GameEvents.OnPenInventoryChanged"/>, raised locally/on the server by the match and re-raised on pure
    /// clients by the network ledger, so it never reads the animals physically inside the pen.
    /// On enable it catches up from <see cref="PenInventorySnapshot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PenInventoryView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Player whose pen this view shows.")]
        private PlayerId _player = PlayerId.Player1;

        [SerializeField, Tooltip("Optional: must be the same AnimalSpawnSettings asset used by the AnimalSpawner, since the animal type index is the position in its Entries. Fills empty slot icons with each AnimalData icon.")]
        private AnimalSpawnSettings _spawnSettings;

        [Header("References")]
        [SerializeField, Tooltip("One slot per animal type, indexed like the spawn settings' entries (chicken, sheep, cow...).")]
        private PenInventorySlotView[] _slots = System.Array.Empty<PenInventorySlotView>();

        public PlayerId Player => _player;

        private void Awake()
        {
            AssignIcons();
            ResetCounts();
        }

        private void OnEnable()
        {
            // Catch up with counts raised while this view was disabled (or before it existed).
            RefreshFromSnapshot();
            GameEvents.OnPenInventoryChanged += HandlePenInventoryChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPenInventoryChanged -= HandlePenInventoryChanged;
        }

        /// <summary>Shows 0 in every slot.</summary>
        public void ResetCounts()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].SetCount(0);
                }
            }
        }

        /// <summary>Shows the last cached count of every slot for this view's player.</summary>
        private void RefreshFromSnapshot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].SetCount(PenInventorySnapshot.Get(_player, i));
                }
            }
        }

        private void HandlePenInventoryChanged(PlayerId playerId, int animalTypeIndex, int count)
        {
            if (playerId != _player || animalTypeIndex < 0 || animalTypeIndex >= _slots.Length)
            {
                return;
            }

            PenInventorySlotView slot = _slots[animalTypeIndex];
            if (slot != null)
            {
                slot.SetCount(count);
            }
        }

        private void AssignIcons()
        {
            if (_spawnSettings == null || _spawnSettings.Entries == null)
            {
                return;
            }

            AnimalSpawnEntry[] entries = _spawnSettings.Entries;
            int length = Mathf.Min(entries.Length, _slots.Length);
            for (int i = 0; i < length; i++)
            {
                if (_slots[i] != null && entries[i].Data != null)
                {
                    _slots[i].SetIconIfEmpty(entries[i].Data.Icon);
                }
            }
        }

        private void OnValidate()
        {
            if (_player == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(PenInventoryView)}] Player is None on '{name}'.", this);
            }

            if (_slots == null || _slots.Length == 0)
            {
                Debug.LogWarning($"[{nameof(PenInventoryView)}] No slots assigned on '{name}'.", this);
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    Debug.LogWarning($"[{nameof(PenInventoryView)}] Slot {i} is empty on '{name}'.", this);
                }
            }

            if (_spawnSettings != null && _spawnSettings.Entries != null && _spawnSettings.Entries.Length != _slots.Length)
            {
                Debug.LogWarning(
                    $"[{nameof(PenInventoryView)}] Slot count ({_slots.Length}) differs from spawn entries ({_spawnSettings.Entries.Length}) on '{name}'.",
                    this);
            }
        }
    }
}
