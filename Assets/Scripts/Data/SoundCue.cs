using System;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>One sound effect: candidate clips (one picked at random), volume and random pitch range.</summary>
    [Serializable]
    public sealed class SoundCue
    {
        /// <summary>Lowest pitch accepted for a random pitch range.</summary>
        public const float MinPitch = 0.1f;

        /// <summary>Highest pitch accepted for a random pitch range.</summary>
        public const float MaxPitch = 3f;

        [SerializeField, Tooltip("Candidate clips; one is picked at random per play (empty = silent).")]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f), Tooltip("Playback volume.")]
        private float _volume = 0.8f;

        [SerializeField, Tooltip("Random pitch per play so repeats do not sound identical: x = min, y = max.")]
        private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

        public AudioClip[] Clips => _clips;
        public float Volume => _volume;
        public Vector2 PitchRange => _pitchRange;

        /// <summary>True when at least one clip slot is assigned.</summary>
        public bool HasClips
        {
            get
            {
                if (_clips == null)
                {
                    return false;
                }

                for (int i = 0; i < _clips.Length; i++)
                {
                    if (_clips[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Clamps the pitch range to the accepted values. Call from the owner's OnValidate.</summary>
        public void Sanitize()
        {
            _pitchRange.x = Mathf.Clamp(_pitchRange.x, MinPitch, MaxPitch);
            _pitchRange.y = Mathf.Clamp(_pitchRange.y, _pitchRange.x, MaxPitch);
        }
    }
}
