using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Server check of a remote throw: the view the owner reports must be close to the last view the server knows for
    /// that player (from its input commands), within a tolerance on yaw (wrapped) and pitch.
    /// </summary>
    public static class ThrowViewRules
    {
        public static bool IsWithinTolerance(float yaw, float pitch, float knownYaw, float knownPitch, float toleranceDegrees)
        {
            if (!IsFinite(yaw) || !IsFinite(pitch) || !IsFinite(knownYaw) || !IsFinite(knownPitch) || toleranceDegrees < 0f)
            {
                return false;
            }

            return Mathf.Abs(Mathf.DeltaAngle(knownYaw, yaw)) <= toleranceDegrees
                && Mathf.Abs(pitch - knownPitch) <= toleranceDegrees;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
