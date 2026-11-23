using System;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    public sealed class SfxVoicePool
    {
        private readonly AudioSource[] _voices;
        private readonly bool[] _busy;
        private int _next;

        public SfxVoicePool(AudioSource[] voices)
        {
            _voices = voices ?? Array.Empty<AudioSource>();
            _busy = new bool[_voices.Length];

            foreach (AudioSource voice in _voices)
            {
                if (voice == null)
                {
                    continue;
                }

                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
            }
        }

        public bool Play(SoundCue cue, IRandomSource rng, bool ignoreListenerPause = false)
        {
            if ((AudioListener.pause && !ignoreListenerPause) || cue == null)
            {
                return false;
            }

            AudioClip clip = AudioVariation.PickRandom(cue.Clips, rng);
            if (clip == null)
            {
                return false;
            }

            AudioSource voice = AcquireVoice();
            if (voice == null)
            {
                return false;
            }

            voice.Stop();
            voice.ignoreListenerPause = ignoreListenerPause;
            voice.clip = clip;
            voice.volume = cue.Volume;
            voice.pitch = AudioVariation.PickPitch(cue.PitchRange, rng);
            voice.Play();
            return true;
        }

        private AudioSource AcquireVoice()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource voice = _voices[i];
                _busy[i] = voice == null || voice.isPlaying;
            }

            int index = AudioVariation.SelectVoice(_busy, _next);
            if (index < 0)
            {
                return null;
            }

            // When all voices are busy, skip unassigned slots before stealing a voice.
            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource voice = _voices[index];
                index = (index + 1) % _voices.Length;
                if (voice != null)
                {
                    _next = index;
                    return voice;
                }
            }

            return null;
        }

        internal static void ValidateVoices(MonoBehaviour owner, AudioSource[] voices)
        {
            if (voices == null || voices.Length == 0)
            {
                Debug.LogWarning($"[{owner.GetType().Name}] No voices assigned on '{owner.name}'.", owner);
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] == null)
                {
                    Debug.LogWarning($"[{owner.GetType().Name}] Voice slot {i} is empty on '{owner.name}'.", owner);
                }
                else if (voices[i].outputAudioMixerGroup == null)
                {
                    Debug.LogWarning($"[{owner.GetType().Name}] Voice slot {i} on '{owner.name}' has no output mixer group (use AlienFarmsMixer > SFX).", owner);
                }
            }
        }
    }
}
