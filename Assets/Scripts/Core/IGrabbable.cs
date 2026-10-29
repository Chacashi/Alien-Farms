using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>Something a player can pick up, carry at a hold point and drop.</summary>
    public interface IGrabbable
    {
        /// <summary>True when the object can currently be picked up.</summary>
        bool CanBeGrabbed { get; }

        /// <summary>Player currently carrying the object, or <see cref="PlayerId.None"/>.</summary>
        PlayerId Holder { get; }

        Vector3 Position { get; }

        /// <summary>Multiplier applied to the holder's walk speed while carrying.</summary>
        float CarrySpeedMultiplier { get; }

        /// <summary>Attaches the object to <paramref name="holdPoint"/>. Returns false if it cannot be grabbed.</summary>
        bool TryGrab(PlayerId holder, Transform holdPoint);

        /// <summary>Detaches the object and places it at <paramref name="position"/> with <paramref name="velocity"/>.</summary>
        void Release(Vector3 position, Vector3 velocity, DropReason reason);
    }
}
