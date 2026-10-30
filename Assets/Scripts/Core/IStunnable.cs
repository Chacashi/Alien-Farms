using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>Something a blast can stun (a player): drops what it carries, gets knocked back and loses control.</summary>
    public interface IStunnable
    {
        /// <summary>Player that owns this target.</summary>
        PlayerId Id { get; }

        /// <summary>
        /// Drops anything carried (<see cref="DropReason.Stunned"/>), knocks the target away from <paramref name="origin"/>
        /// at <paramref name="knockbackSpeed"/> (m/s) and stuns it for <paramref name="duration"/> seconds (never stacks).
        /// </summary>
        void ReceiveStun(PlayerId source, Vector3 origin, float duration, float knockbackSpeed);
    }
}
