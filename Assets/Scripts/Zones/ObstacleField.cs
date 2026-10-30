using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// Scene composition root of the random obstacles of the Animal Zone. Holds the scene-placed
    /// <see cref="ObstacleSlot"/>s and, on <see cref="Regenerate"/>, asks <see cref="ObstacleLayoutPlanner"/> which slots get
    /// which variant and yaw, then toggles the pre-instantiated children (no Instantiate/Destroy, no positions moved).
    /// The first layout is rolled in Awake, before the animal spawner's Start populates the zone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObstacleField : MonoBehaviour
    {
        // Gizmo circle resolution.
        private const int GizmoSegments = 24;

        [Header("Config")]
        [SerializeField, Tooltip("Count, variant weights, spacing, mirroring and seed.")]
        private ObstacleLayoutSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Every obstacle slot of the Animal Zone (mirror pairs wired on the slots).")]
        private ObstacleSlot[] _slots = new ObstacleSlot[0];

        [SerializeField, Tooltip("Layers that block a slot for the round when inside its clearance radius (Player).")]
        private LayerMask _clearanceMask;

        [SerializeField, Tooltip("Animal Zone (editor validation and gizmos only).")]
        private AnimalZone _animalZone;

        private ObstacleLayoutPlanner _planner;
        private IRandomSource _rng;
        private ObstacleVariantRule[] _rules;
        private ObstacleSlotInfo[] _slotInfos;
        private ObstaclePlacement[] _placements;
        private Vector3[] _probeCenters;
        // [slot * ruleCount + tableIndex] -> local child index of that slot, or -1.
        private int[] _localVariantIndex;
        private bool _initialized;
        private int _activeCount;

        /// <summary>Obstacles enabled by the last <see cref="Regenerate"/>.</summary>
        public int ActiveCount => _activeCount;

        private void Reset()
        {
            _clearanceMask = LayerMask.GetMask(LayerNames.Player);
        }

        private void Awake()
        {
            if (_settings == null || _slots == null || _slots.Length == 0)
            {
                Debug.LogError($"[{nameof(ObstacleField)}] Missing settings or slots on '{name}'; obstacles disabled.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    Debug.LogError($"[{nameof(ObstacleField)}] Slot {i} on '{name}' is empty; obstacles disabled.", this);
                    enabled = false;
                    return;
                }
            }

            BuildRules();
            BuildSlots();

            _placements = new ObstaclePlacement[_slots.Length];
            _planner = new ObstacleLayoutPlanner(_slots.Length);
            _rng = new SystemRandomSource(_settings.UseFixedSeed ? _settings.Seed : System.Environment.TickCount);
            _initialized = true;

            Regenerate();
        }

        /// <summary>
        /// Rolls a new layout: slots with a player inside their clearance stay empty. Called by the match on each new
        /// round, after the players are respawned and before the animals are repopulated.
        /// </summary>
        public void Regenerate()
        {
            if (!_initialized)
            {
                return;
            }

            // Respawned players were teleported this frame: make the clearance queries see them.
            Physics.SyncTransforms();

            float probeHeight = _settings.ClearanceProbeHeight;
            for (int i = 0; i < _slots.Length; i++)
            {
                Vector3 center = _probeCenters[i] + Vector3.up * probeHeight;
                _slotInfos[i].Blocked = Physics.CheckSphere(center, _slots[i].ClearanceRadius, _clearanceMask, QueryTriggerInteraction.Ignore);
            }

            _activeCount = _planner.Plan(
                _rng, _slotInfos, _rules, _settings.MinActive, _settings.MaxActive, _settings.MinSpacing, _placements);

            int ruleCount = _rules.Length;
            for (int i = 0; i < _slots.Length; i++)
            {
                ObstaclePlacement placement = _placements[i];
                int local = placement.IsActive ? _localVariantIndex[i * ruleCount + placement.Variant] : -1;
                _slots[i].Apply(local, placement.Yaw);
            }

            // The animal spawner checks free spots right after this: colliders must be at their new state.
            Physics.SyncTransforms();

            if (_activeCount < _settings.MinActive)
            {
                Debug.LogWarning($"[{nameof(ObstacleField)}] Placed {_activeCount} obstacles, below the minimum of {_settings.MinActive} (blocked slots, spacing or variants).", this);
            }
        }

        private void BuildRules()
        {
            IReadOnlyList<ObstacleWeightEntry> entries = _settings.Variants;
            int count = Mathf.Min(entries != null ? entries.Count : 0, ObstacleLayoutSettings.MaxVariants);
            _rules = new ObstacleVariantRule[count];
            for (int i = 0; i < count; i++)
            {
                ObstacleWeightEntry entry = entries[i];
                ObstacleVariantData data = entry.Variant;
                // Duplicated entries resolve to the first one, so the later copies are never picked.
                bool usable = data != null && _settings.IndexOf(data) == i;
                float[] yaws = usable ? CopyYaws(data.AllowedYaws) : new float[0];
                _rules[i] = new ObstacleVariantRule(usable ? Mathf.Max(0, entry.Weight) : 0, yaws, !usable || data.IsMirrorSymmetric);
            }
        }

        private static float[] CopyYaws(IReadOnlyList<float> source)
        {
            int count = source != null ? source.Count : 0;
            float[] yaws = new float[count];
            for (int i = 0; i < count; i++)
            {
                yaws[i] = Mathf.Repeat(source[i], 360f);
            }

            return yaws;
        }

        private void BuildSlots()
        {
            int slotCount = _slots.Length;
            int ruleCount = _rules.Length;
            _slotInfos = new ObstacleSlotInfo[slotCount];
            _probeCenters = new Vector3[slotCount];
            _localVariantIndex = new int[slotCount * ruleCount];
            for (int i = 0; i < _localVariantIndex.Length; i++)
            {
                _localVariantIndex[i] = -1;
            }

            for (int i = 0; i < slotCount; i++)
            {
                ObstacleSlot slot = _slots[i];
                int mirror = _settings.MirrorAcrossCenter ? IndexOfSlot(slot.Mirror) : -1;
                uint mask = 0u;

                for (int local = 0; local < slot.VariantCount; local++)
                {
                    ObstacleVariant variant = slot.GetVariant(local);
                    int table = variant != null ? _settings.IndexOf(variant.Data) : -1;
                    if (table < 0)
                    {
                        Debug.LogWarning($"[{nameof(ObstacleField)}] Variant {local} of slot '{slot.name}' is not in the layout settings' table; it is never placed.", slot);
                        continue;
                    }

                    int key = i * ruleCount + table;
                    if (_localVariantIndex[key] < 0)
                    {
                        _localVariantIndex[key] = local;
                        mask |= 1u << table;
                    }
                }

                _probeCenters[i] = slot.transform.position;
                _slotInfos[i] = new ObstacleSlotInfo(slot.PlanarPosition, mirror, mask);
            }
        }

        private int IndexOfSlot(ObstacleSlot slot)
        {
            if (slot == null)
            {
                return -1;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == slot)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(ObstacleField)}] Missing ObstacleLayoutSettings on '{name}'.", this);
            }

            if (_animalZone == null)
            {
                Debug.LogWarning($"[{nameof(ObstacleField)}] Missing AnimalZone on '{name}' (validation only).", this);
            }

            if (_clearanceMask.value == 0)
            {
                Debug.LogWarning($"[{nameof(ObstacleField)}] Clearance mask on '{name}' is empty; players never block a slot.", this);
            }

            if (_slots == null || _slots.Length == 0)
            {
                Debug.LogWarning($"[{nameof(ObstacleField)}] '{name}' has no obstacle slots.", this);
                return;
            }

            float minSpacingSqr = _settings != null ? _settings.MinSpacing * _settings.MinSpacing : 0f;
            for (int i = 0; i < _slots.Length; i++)
            {
                ObstacleSlot slot = _slots[i];
                if (slot == null)
                {
                    Debug.LogWarning($"[{nameof(ObstacleField)}] Slot {i} on '{name}' is empty.", this);
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    ObstacleSlot other = _slots[j];
                    if (other == null)
                    {
                        continue;
                    }

                    if (other == slot)
                    {
                        Debug.LogWarning($"[{nameof(ObstacleField)}] Slot '{slot.name}' is listed twice on '{name}'.", this);
                    }
                    else if ((other.PlanarPosition - slot.PlanarPosition).sqrMagnitude < minSpacingSqr)
                    {
                        Debug.LogWarning($"[{nameof(ObstacleField)}] Slots '{other.name}' and '{slot.name}' are closer than the min spacing; they are never both active.", this);
                    }
                }

                if (slot.Mirror != null && IndexOfSlot(slot.Mirror) < 0)
                {
                    Debug.LogWarning($"[{nameof(ObstacleField)}] Mirror of slot '{slot.name}' is not listed on '{name}'; it is treated as a single.", this);
                }

                if (_animalZone != null && _settings != null)
                {
                    Bounds bounds = _animalZone.Bounds;
                    Vector2 p = slot.PlanarPosition;
                    float edge = Mathf.Min(Mathf.Min(p.x - bounds.min.x, bounds.max.x - p.x), Mathf.Min(p.y - bounds.min.z, bounds.max.z - p.y));
                    if (edge < _settings.MinEdgeDistance)
                    {
                        Debug.LogWarning($"[{nameof(ObstacleField)}] Slot '{slot.name}' is {edge:0.##} m from the Animal Zone edge (min {_settings.MinEdgeDistance}).", this);
                    }
                }

                if (_settings != null)
                {
                    for (int v = 0; v < slot.VariantCount; v++)
                    {
                        ObstacleVariant variant = slot.GetVariant(v);
                        if (variant != null && _settings.IndexOf(variant.Data) < 0)
                        {
                            Debug.LogWarning($"[{nameof(ObstacleField)}] Variant '{variant.name}' of slot '{slot.name}' is not in the layout settings' table.", this);
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_slots == null)
            {
                return;
            }

            float spacing = _settings != null ? _settings.MinSpacing * 0.5f : 0f;
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.5f);
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    continue;
                }

                // Half the min spacing: two circles touching = two slots exactly at the min spacing.
                Vector3 center = _slots[i].transform.position;
                Vector3 previous = center + new Vector3(spacing, 0f, 0f);
                for (int s = 1; s <= GizmoSegments; s++)
                {
                    float angle = s * (2f * Mathf.PI / GizmoSegments);
                    Vector3 next = center + new Vector3(Mathf.Cos(angle) * spacing, 0f, Mathf.Sin(angle) * spacing);
                    Gizmos.DrawLine(previous, next);
                    previous = next;
                }
            }

            if (_animalZone != null)
            {
                Bounds bounds = _animalZone.Bounds;
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(bounds.center, bounds.size);

                if (_settings != null)
                {
                    float inset = _settings.MinEdgeDistance * 2f;
                    Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
                    Gizmos.DrawWireCube(bounds.center, new Vector3(Mathf.Max(0f, bounds.size.x - inset), bounds.size.y, Mathf.Max(0f, bounds.size.z - inset)));
                }
            }
        }
    }
}
