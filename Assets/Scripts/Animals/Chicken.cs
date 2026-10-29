using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>Fast, light animal that zigzags while fleeing.</summary>
    public sealed class Chicken : Animal
    {
        private ChickenData _chickenData;
        private float _zigzagPhase;

        protected override void OnInitialized()
        {
            _chickenData = Data as ChickenData;
            if (_chickenData == null)
            {
                Debug.LogError($"[{nameof(Chicken)}] '{name}' needs a {nameof(ChickenData)} asset; it will flee in a straight line.", this);
            }

            // Desynchronize chickens so they don't zigzag in unison.
            _zigzagPhase = Random.Range(0f, 2f * Mathf.PI);
        }

        protected override Vector3 ComputeFleeDirection(Vector3 awayFromThreat)
        {
            if (_chickenData == null)
            {
                return awayFromThreat;
            }

            float wave = Mathf.Sin(Time.time * 2f * Mathf.PI * _chickenData.ZigzagFrequency + _zigzagPhase);
            return Quaternion.AngleAxis(wave * _chickenData.ZigzagAngle, Vector3.up) * awayFromThreat;
        }
    }
}
