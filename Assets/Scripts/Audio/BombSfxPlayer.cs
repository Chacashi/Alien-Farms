using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Items;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary>
    /// Plays the stun bomb sounds: spawn pop, grab, a looped fuse per armed bomb (its pitch rises while it flies and it
    /// is cut when that bomb detonates or leaves play), the explosion and one dizzy chime per stunned player.
    /// Scene-owned 2D AudioSources (split-screen with a single AudioListener: positional audio would favour one view).
    /// One-shots stay silent while the game is paused; the looped fuses obey AudioListener.pause.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BombSfxPlayer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Bomb definition holding the sound cues.")]
        private BombData _data;

        [Header("Voices")]
        [SerializeField, Tooltip("Pooled AudioSources for one-shots (spawn, grab, explosion, stun), used round-robin. Forced to 2D, no loop, no play on awake. 4 is plenty.")]
        private AudioSource[] _oneShotVoices;

        [SerializeField, Tooltip("AudioSources for armed-bomb fuses, one per bomb in flight (forced to 2D, looping, no play on awake). Match the max bombs in play (2).")]
        private AudioSource[] _fuseVoices;

        private bool[] _oneShotBusy;
        private int _nextOneShot;

        // Per fuse voice: the armed bomb it belongs to (null = free), its rolled base pitch and when it was lit.
        private Bomb[] _fuseOwners;
        private bool[] _fuseBusy;
        private float[] _fuseBasePitch;
        private float[] _fuseStartTime;
        private int _nextFuse;
        private int _activeFuses;

        private IRandomSource _rng;

        private void Awake()
        {
            _rng = new SystemRandomSource(Environment.TickCount);

            int oneShotCount = _oneShotVoices != null ? _oneShotVoices.Length : 0;
            _oneShotBusy = new bool[oneShotCount];
            for (int i = 0; i < oneShotCount; i++)
            {
                Configure(_oneShotVoices[i], false);
            }

            int fuseCount = _fuseVoices != null ? _fuseVoices.Length : 0;
            _fuseOwners = new Bomb[fuseCount];
            _fuseBusy = new bool[fuseCount];
            _fuseBasePitch = new float[fuseCount];
            _fuseStartTime = new float[fuseCount];
            for (int i = 0; i < fuseCount; i++)
            {
                Configure(_fuseVoices[i], true);
            }

            if (_data == null)
            {
                Debug.LogError($"[{nameof(BombSfxPlayer)}] Missing BombData on '{name}'; bombs will be silent.", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnBombSpawned += HandleBombSpawned;
            GameEvents.OnBombGrabbed += HandleBombGrabbed;
            GameEvents.OnBombArmed += HandleBombArmed;
            GameEvents.OnBombDespawned += HandleBombDespawned;
            GameEvents.OnBombDetonated += HandleBombDetonated;
            GameEvents.OnPlayerStunned += HandlePlayerStunned;
        }

        private void OnDisable()
        {
            GameEvents.OnBombSpawned -= HandleBombSpawned;
            GameEvents.OnBombGrabbed -= HandleBombGrabbed;
            GameEvents.OnBombArmed -= HandleBombArmed;
            GameEvents.OnBombDespawned -= HandleBombDespawned;
            GameEvents.OnBombDetonated -= HandleBombDetonated;
            GameEvents.OnPlayerStunned -= HandlePlayerStunned;
            StopAllFuses();
        }

        private void Update()
        {
            if (_activeFuses == 0 || _data == null)
            {
                return;
            }

            // Time.time freezes with timeScale 0, so the ramp also holds while paused.
            float now = Time.time;
            for (int i = 0; i < _fuseOwners.Length; i++)
            {
                if (!HasOwner(i))
                {
                    continue;
                }

                // Owner destroyed without despawning (scene unload): never leave its fuse looping.
                AudioSource voice = _fuseVoices[i];
                if (_fuseOwners[i] == null || voice == null)
                {
                    StopFuse(i);
                    continue;
                }

                voice.pitch = AudioVariation.RampPitch(
                    _fuseBasePitch[i], now - _fuseStartTime[i], _data.FusePitchRampTime, _data.FusePitchRampMultiplier);
            }
        }

        private void HandleBombSpawned()
        {
            if (_data != null)
            {
                PlayOneShot(_data.SpawnSound);
            }
        }

        private void HandleBombGrabbed(PlayerId playerId, Bomb bomb)
        {
            if (_data != null)
            {
                PlayOneShot(_data.GrabSound);
            }
        }

        private void HandleBombDetonated(PlayerId thrower, Vector3 position, float radius)
        {
            if (_data != null)
            {
                PlayOneShot(_data.ExplosionSound);
            }
        }

        private void HandlePlayerStunned(PlayerId victim, PlayerId source, float duration)
        {
            if (_data != null)
            {
                PlayOneShot(_data.StunSound);
            }
        }

        private void HandleBombArmed(PlayerId thrower, Bomb bomb)
        {
            if (_data == null || bomb == null || AudioListener.pause)
            {
                return;
            }

            SoundCue cue = _data.FuseSound;
            AudioClip clip = cue != null ? AudioVariation.PickRandom(cue.Clips, _rng) : null;
            if (clip == null)
            {
                return;
            }

            int index = AcquireFuse();
            if (index < 0)
            {
                return;
            }

            AudioSource voice = _fuseVoices[index];
            float basePitch = AudioVariation.PickPitch(cue.PitchRange, _rng);

            if (!HasOwner(index))
            {
                _activeFuses++;
            }

            _fuseOwners[index] = bomb;
            _fuseBasePitch[index] = basePitch;
            _fuseStartTime[index] = Time.time;

            voice.Stop();
            voice.clip = clip;
            voice.volume = cue.Volume;
            voice.pitch = basePitch;
            voice.Play();
        }

        private void HandleBombDespawned(Bomb bomb)
        {
            if (_fuseOwners == null)
            {
                return;
            }

            for (int i = 0; i < _fuseOwners.Length; i++)
            {
                if (_fuseOwners[i] == bomb)
                {
                    StopFuse(i);
                }
            }
        }

        private void PlayOneShot(SoundCue cue)
        {
            // Paused game: stay silent (a clip started now would just be frozen and burst out on resume).
            if (cue == null || AudioListener.pause)
            {
                return;
            }

            AudioClip clip = AudioVariation.PickRandom(cue.Clips, _rng);
            if (clip == null)
            {
                return;
            }

            AudioSource voice = AcquireOneShot();
            if (voice == null)
            {
                return;
            }

            voice.Stop();
            voice.clip = clip;
            voice.volume = cue.Volume;
            voice.pitch = AudioVariation.PickPitch(cue.PitchRange, _rng);
            voice.Play();
        }

        private AudioSource AcquireOneShot()
        {
            for (int i = 0; i < _oneShotBusy.Length; i++)
            {
                AudioSource voice = _oneShotVoices[i];
                // Missing slots count as busy so they are only chosen when everything else is too.
                _oneShotBusy[i] = voice == null || voice.isPlaying;
            }

            int index = AudioVariation.SelectVoice(_oneShotBusy, _nextOneShot);
            if (index < 0)
            {
                return null;
            }

            _nextOneShot = (index + 1) % _oneShotBusy.Length;
            return _oneShotVoices[index];
        }

        /// <summary>Index of a free fuse voice (or the oldest one, stolen); -1 when there is none usable.</summary>
        private int AcquireFuse()
        {
            for (int i = 0; i < _fuseBusy.Length; i++)
            {
                _fuseBusy[i] = _fuseVoices[i] == null || HasOwner(i);
            }

            int index = AudioVariation.SelectVoice(_fuseBusy, _nextFuse);
            if (index < 0 || _fuseVoices[index] == null)
            {
                return -1;
            }

            _nextFuse = (index + 1) % _fuseBusy.Length;
            return index;
        }

        private void StopFuse(int index)
        {
            if (HasOwner(index))
            {
                _activeFuses--;
            }

            _fuseOwners[index] = null;
            AudioSource voice = _fuseVoices[index];
            if (voice != null)
            {
                voice.Stop();
            }
        }

        private void StopAllFuses()
        {
            if (_fuseOwners == null)
            {
                return;
            }

            for (int i = 0; i < _fuseOwners.Length; i++)
            {
                StopFuse(i);
            }

            _activeFuses = 0;
        }

        // Reference check on purpose: a destroyed owner still occupies its slot until StopFuse clears it.
        private bool HasOwner(int index) => !ReferenceEquals(_fuseOwners[index], null);

        private static void Configure(AudioSource voice, bool loop)
        {
            if (voice == null)
            {
                return;
            }

            voice.playOnAwake = false;
            voice.loop = loop;
            voice.spatialBlend = 0f;
            voice.ignoreListenerPause = false;
        }

        private void OnValidate()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(BombSfxPlayer)}] Missing BombData on '{name}'; bombs will be silent.", this);
            }

            ValidateVoices(_oneShotVoices, "One-shot");
            ValidateVoices(_fuseVoices, "Fuse");
        }

        private void ValidateVoices(AudioSource[] voices, string label)
        {
            if (voices == null || voices.Length == 0)
            {
                Debug.LogWarning($"[{nameof(BombSfxPlayer)}] No {label} voices assigned on '{name}'; those bomb sounds will be silent.", this);
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] == null)
                {
                    Debug.LogWarning($"[{nameof(BombSfxPlayer)}] {label} voice slot {i} is empty on '{name}'.", this);
                }
                else if (voices[i].outputAudioMixerGroup == null)
                {
                    Debug.LogWarning($"[{nameof(BombSfxPlayer)}] {label} voice slot {i} on '{name}' has no output mixer group; the SFX volume will not apply (use AlienFarmsMixer > SFX).", this);
                }
            }
        }
    }
}
