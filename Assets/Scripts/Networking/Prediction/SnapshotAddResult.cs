namespace AlienFarms.Networking.Prediction
{
    /// <summary>What <see cref="SnapshotBuffer.Add"/> did with a snapshot.</summary>
    public enum SnapshotAddResult
    {
        /// <summary>Not newer than the newest buffered one: ignored.</summary>
        Rejected = 0,

        /// <summary>Appended after the history.</summary>
        Added = 1,

        /// <summary>
        /// First snapshot, or a new teleport epoch: the history was cleared and the pose must snap. Also detected when
        /// the snapshot that started the epoch was lost (every later one carries the epoch).
        /// </summary>
        Reset = 2
    }
}
