using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace AlienFarms.Items
{
    /// <summary>Plays a pooled <see cref="BombBlastVfx"/> on every <see cref="GameEvents.OnBombDetonated"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class BombBlastVfxPlayer : MonoBehaviour
    {
        private const int DefaultCapacity = 2;
        private const int MaxSize = 4;

        [Header("Config")]
        [SerializeField, Tooltip("Bomb definition (blast VFX prefab and duration).")]
        private BombData _data;

        [Header("References")]
        [SerializeField, Tooltip("Parent for pooled VFX instances.")]
        private Transform _container;

        private ObjectPool<BombBlastVfx> _pool;

        private void Awake()
        {
            if (_data == null || _data.BlastVfxPrefab == null)
            {
                Debug.LogError($"[{nameof(BombBlastVfxPlayer)}] Missing BombData or its blast VFX prefab on '{name}'; blasts will be invisible.", this);
                enabled = false;
                return;
            }

            _pool = new ObjectPool<BombBlastVfx>(
                CreateVfx,
                null,
                OnReleaseVfx,
                OnDestroyVfx,
                Application.isEditor,
                DefaultCapacity,
                MaxSize);
        }

        private void Start()
        {
            // Prewarm at load time (one-off temporary buffer).
            BombBlastVfx[] buffer = new BombBlastVfx[DefaultCapacity];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = _pool.Get();
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnBombDetonated += HandleBombDetonated;
        }

        private void OnDisable()
        {
            GameEvents.OnBombDetonated -= HandleBombDetonated;
        }

        private void HandleBombDetonated(PlayerId thrower, Vector3 position, float radius)
        {
            if (_pool == null)
            {
                return;
            }

            _pool.Get().Play(position, radius, _data.BlastVfxDuration, _pool);
        }

        private BombBlastVfx CreateVfx()
        {
            BombBlastVfx vfx = Instantiate(_data.BlastVfxPrefab, _container);
            vfx.gameObject.SetActive(false);
            return vfx;
        }

        private static void OnReleaseVfx(BombBlastVfx vfx) => vfx.gameObject.SetActive(false);

        private static void OnDestroyVfx(BombBlastVfx vfx)
        {
            if (vfx != null)
            {
                Destroy(vfx.gameObject);
            }
        }

        private void OnValidate()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(BombBlastVfxPlayer)}] Missing BombData on '{name}'.", this);
            }

            if (_container == null)
            {
                Debug.LogWarning($"[{nameof(BombBlastVfxPlayer)}] No container on '{name}'; VFX instances will sit at the scene root.", this);
            }
        }
    }
}
