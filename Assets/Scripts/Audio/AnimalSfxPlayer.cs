using System;
using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary>
    /// Plays animal voices: the grab voice when any player picks an animal up (stealing included, since the thief
    /// grabs it again after the tackle knocks it loose) and the alarm voice when it is thrown. Plain releases,
    /// steals and forced drops are silent. Uses a small pool of scene-owned 2D AudioSources (split-screen with a
    /// single AudioListener: positional audio would favour one player's view).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimalSfxPlayer : MonoBehaviour
    {
        [Header("Voices")]
        [SerializeField, Tooltip("Pooled AudioSources used round-robin (forced to 2D, no loop, no play on awake). 4 is plenty.")]
        private AudioSource[] _voices;

        private bool[] _busy;
        private int _next;
        private IRandomSource _rng;

        private void Awake()
        {
            _rng = new SystemRandomSource(Environment.TickCount);
            int count = _voices != null ? _voices.Length : 0;
            _busy = new bool[count];

            for (int i = 0; i < count; i++)
            {
                AudioSource voice = _voices[i];
                if (voice == null)
                {
                    continue;
                }

                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnAnimalGrabbed += HandleAnimalGrabbed;
            GameEvents.OnAnimalDropped += HandleAnimalDropped;
        }

        private void OnDisable()
        {
            GameEvents.OnAnimalGrabbed -= HandleAnimalGrabbed;
            GameEvents.OnAnimalDropped -= HandleAnimalDropped;
        }

        private void HandleAnimalGrabbed(PlayerId playerId, Animal animal)
        {
            AnimalData data = animal != null ? animal.Data : null;
            if (data != null)
            {
                Play(data.GrabClips, data);
            }
        }

        private void HandleAnimalDropped(PlayerId playerId, Animal animal, DropReason reason)
        {
            if (reason != DropReason.Thrown)
            {
                return;
            }

            AnimalData data = animal != null ? animal.Data : null;
            if (data != null)
            {
                Play(data.ThrowClips, data);
            }
        }

        private void Play(AudioClip[] clips, AnimalData data)
        {
            // Paused game: stay silent (a clip started now would just be frozen and burst out on resume).
            if (AudioListener.pause)
            {
                return;
            }

            AudioClip clip = AudioVariation.PickRandom(clips, _rng);
            if (clip == null)
            {
                return;
            }

            AudioSource voice = AcquireVoice();
            if (voice == null)
            {
                return;
            }

            voice.Stop();
            voice.clip = clip;
            voice.volume = data.VoiceVolume;
            voice.pitch = AudioVariation.PickPitch(data.VoicePitchRange, _rng);
            voice.Play();
        }

        private AudioSource AcquireVoice()
        {
            for (int i = 0; i < _busy.Length; i++)
            {
                AudioSource voice = _voices[i];
                // Missing slots count as busy so they are only chosen when everything else is too.
                _busy[i] = voice == null || voice.isPlaying;
            }

            int index = AudioVariation.SelectVoice(_busy, _next);
            if (index < 0)
            {
                return null;
            }

            _next = (index + 1) % _busy.Length;
            return _voices[index];
        }

        private void OnValidate()
        {
            if (_voices == null || _voices.Length == 0)
            {
                Debug.LogWarning($"[{nameof(AnimalSfxPlayer)}] No voices assigned on '{name}'; animal voices will be silent.", this);
                return;
            }

            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voices[i] == null)
                {
                    Debug.LogWarning($"[{nameof(AnimalSfxPlayer)}] Voice slot {i} is empty on '{name}'.", this);
                }
                else if (_voices[i].outputAudioMixerGroup == null)
                {
                    Debug.LogWarning($"[{nameof(AnimalSfxPlayer)}] Voice slot {i} on '{name}' has no output mixer group; the SFX volume will not apply (use AlienFarmsMixer > SFX).", this);
                }
            }
        }
    }
}
