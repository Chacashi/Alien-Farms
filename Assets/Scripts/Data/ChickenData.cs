using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Animal data for chickens: adds a zigzag while fleeing.</summary>
    [CreateAssetMenu(fileName = "ChickenData", menuName = "Alien Farms/Animals/Chicken Data")]
    public sealed class ChickenData : AnimalData
    {
        [Header("Chicken")]
        [SerializeField, Range(0f, 90f), Tooltip("Max deviation from the straight flee direction (deg).")]
        private float _zigzagAngle = 35f;

        [SerializeField, Min(0f), Tooltip("Zigzag oscillations per second (Hz).")]
        private float _zigzagFrequency = 2f;

        public float ZigzagAngle => _zigzagAngle;
        public float ZigzagFrequency => _zigzagFrequency;
    }
}
