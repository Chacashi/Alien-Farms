namespace AlienFarms.Core
{
    /// <summary>Why a carried <see cref="IGrabbable"/> left its holder.</summary>
    public enum DropReason
    {
        /// <summary>The holder released the grab button.</summary>
        Released = 0,

        /// <summary>Another player tackled the holder.</summary>
        Stolen = 1,

        /// <summary>System-driven drop: holder left, was frozen or the object despawned.</summary>
        Forced = 2,

        /// <summary>The holder released Grab while aiming: launched on a ballistic arc.</summary>
        Thrown = 3
    }
}
