using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    [DisallowMultipleComponent]
    public sealed class CharacterSelectSfxPlayer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Sound cues for local character selection.")]
        private UiSfxLibrary _library;

        [Header("Voices")]
        [SerializeField, Tooltip("Two pooled AudioSources routed to AlienFarmsMixer > SFX.")]
        private AudioSource[] _voices;

        private SfxVoicePool _pool;
        private IRandomSource _rng;

        private void Awake()
        {
            _pool = new SfxVoicePool(_voices);
            _rng = new SystemRandomSource(Environment.TickCount);
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterPickConfirmed += HandlePickConfirmed;
            GameEvents.OnCharacterPickReleased += HandlePickReleased;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterPickConfirmed -= HandlePickConfirmed;
            GameEvents.OnCharacterPickReleased -= HandlePickReleased;
        }

        private void HandlePickConfirmed(PlayerId playerId) => Play(UiSfxCue.CharacterConfirm);

        private void HandlePickReleased(PlayerId playerId) => Play(UiSfxCue.CharacterRelease);

        private void Play(UiSfxCue cue)
        {
            if (_library != null)
            {
                _pool.Play(_library.Get(cue), _rng);
            }
        }

        private void OnValidate()
        {
            if (_library == null)
            {
                Debug.LogWarning($"[{nameof(CharacterSelectSfxPlayer)}] Assign the UI SFX library on '{name}'.", this);
            }

            SfxVoicePool.ValidateVoices(this, _voices);
        }
    }
}
