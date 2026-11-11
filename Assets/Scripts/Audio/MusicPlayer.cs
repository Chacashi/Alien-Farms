using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary>
    /// Loops a scene's music track on a 2D AudioSource (route its output to the Music mixer group). Optionally keeps
    /// playing while the game is paused (<see cref="AudioListener.pause"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField, Tooltip("Track looped while this scene is loaded.")]
        private AudioClip _track;

        [SerializeField, Tooltip("Keep playing while the game is paused (AudioListener.pause).")]
        private bool _playDuringPause = true;

        private AudioSource _source;

        private void Awake()
        {
            TryGetComponent(out _source);
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = _playDuringPause;
            _source.clip = _track;
        }

        private void Start()
        {
            if (_track != null)
            {
                _source.Play();
            }
            else
            {
                // Valid on purpose (no music for this scene yet): informational only, once per load.
                Debug.Log($"[{nameof(MusicPlayer)}] No music track assigned on '{name}'; playing nothing.", this);
            }
        }

        private void OnValidate()
        {
            // Prefab assets are not wired yet; only validate scene instances.
            if (!gameObject.scene.IsValid())
            {
                return;
            }

            if (TryGetComponent(out AudioSource source) && source.outputAudioMixerGroup == null)
            {
                Debug.LogWarning($"[{nameof(MusicPlayer)}] AudioSource on '{name}' has no output mixer group; the Music volume will not apply (use AlienFarmsMixer > Music).", this);
            }
        }
    }
}
