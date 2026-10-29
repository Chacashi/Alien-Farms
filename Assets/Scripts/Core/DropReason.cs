namespace AlienFarms.Core
{
    /// <summary>Why a carried <see cref="IGrabbable"/> left its holder.</summary>
    public enum DropReason
    {
        /// <summary>The holder released the grab button.</summary>
        Released = 0,

        /// <summary>Another player tackled the holder.</summary>
        Stolen = 1,

        /// <summary>System-driven drop: holder left, object despawned or was captured.</summary>
        Forced = 2
    }
}
