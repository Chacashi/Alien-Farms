namespace AlienFarms.Player
{
    /// <summary>Who decides when a <see cref="PlayerGrabber"/> lets go of what it carries.</summary>
    public enum ReleaseMode
    {
        /// <summary>Releasing Grab drops (or throws, while aiming) the carried object. Local default.</summary>
        Auto = 0,

        /// <summary>Only <see cref="PlayerGrabber.ReleaseHeld"/> / <see cref="PlayerGrabber.ThrowWithView"/> release it.</summary>
        External = 1
    }
}
