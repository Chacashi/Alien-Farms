using AlienFarms.Data;

namespace AlienFarms.Core
{
    /// <summary>
    /// A grabbable that can be launched on a ballistic arc while carried. The launch itself goes through
    /// <see cref="IGrabbable.Release"/> with <see cref="DropReason.Thrown"/>.
    /// </summary>
    public interface IThrowable : IGrabbable
    {
        /// <summary>True when the object can be thrown right now (normally: while carried).</summary>
        bool CanBeThrown { get; }

        /// <summary>Per-object throw tuning, or null to use the thrower's default.</summary>
        ThrowSettings ThrowSettings { get; }

        /// <summary>Planar radius of the body (m). The preview sweeps a sphere this wide (capped by the half height).</summary>
        float CollisionRadius { get; }

        /// <summary>Half the body height (m). <see cref="IGrabbable.Position"/> is the base of the body.</summary>
        float HalfHeight { get; }
    }
}
