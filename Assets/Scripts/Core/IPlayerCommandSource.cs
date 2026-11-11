using System;
using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>
    /// Everything the player's gameplay components read from input. Implemented locally by the PlayerInputHandler and,
    /// online, by a network-fed source. Values are sampled every frame; events fire on button edges.
    /// </summary>
    public interface IPlayerCommandSource
    {
        /// <summary>Planar move input (x = right, y = forward), magnitude up to 1.</summary>
        Vector2 Move { get; }

        /// <summary>Look input: a per-frame pointer delta or a stick rate (see <see cref="IsLookPointerDelta"/>).</summary>
        Vector2 Look { get; }

        /// <summary>True when <see cref="Look"/> is a pointer delta (mouse) rather than a stick rate.</summary>
        bool IsLookPointerDelta { get; }

        bool IsSprintHeld { get; }

        bool IsGrabHeld { get; }

        bool IsAimHeld { get; }

        event Action GrabStarted;

        event Action GrabCanceled;
    }
}
