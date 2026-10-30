using UnityEngine;
using UnityEngine.Pool;

namespace AlienFarms.Items
{
    /// <summary>Pooled blast effect: a sphere that expands to the blast size and fades out, then returns to its pool.</summary>
    [DisallowMultipleComponent]
    public sealed class BombBlastVfx : MonoBehaviour
    {
        private const float StartScale = 0.2f;

        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        [Header("References")]
        [SerializeField, Tooltip("Optional: renderer faded out through a MaterialPropertyBlock (_BaseColor alpha).")]
        private Renderer _renderer;

        private MaterialPropertyBlock _block;
        private IObjectPool<BombBlastVfx> _pool;
        private Color _baseColor = Color.white;
        private float _duration;
        private float _elapsed;
        private float _endScale;
        private bool _playing;

        private void Awake()
        {
            EnsureInitialized();
        }

        // Also called from Play: an inactive prefab root would not have run Awake yet.
        private void EnsureInitialized()
        {
            if (_block != null)
            {
                return;
            }

            _block = new MaterialPropertyBlock();
            if (_renderer != null && _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(s_baseColorId))
            {
                _baseColor = _renderer.sharedMaterial.GetColor(s_baseColorId);
            }
            else
            {
                _renderer = null;
            }
        }

        /// <summary>Starts the effect at <paramref name="position"/>, growing to <paramref name="radius"/>.</summary>
        public void Play(Vector3 position, float radius, float duration, IObjectPool<BombBlastVfx> pool)
        {
            EnsureInitialized();
            _pool = pool;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _endScale = 2f * Mathf.Max(0f, radius);
            _playing = true;

            transform.position = position;
            Apply(0f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Apply(t);

            if (t >= 1f)
            {
                _playing = false;
                if (_pool != null)
                {
                    _pool.Release(this);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private void Apply(float t)
        {
            // Ease-out: fast growth that slows near the end.
            float eased = 1f - (1f - t) * (1f - t);
            float scale = Mathf.Lerp(StartScale, _endScale, eased);
            transform.localScale = new Vector3(scale, scale, scale);

            if (_renderer != null)
            {
                Color color = _baseColor;
                color.a = _baseColor.a * (1f - t);
                _renderer.GetPropertyBlock(_block);
                _block.SetColor(s_baseColorId, color);
                _renderer.SetPropertyBlock(_block);
            }
        }

        private void OnValidate()
        {
            if (_renderer == null)
            {
                TryGetComponent(out _renderer);
            }
        }
    }
}
