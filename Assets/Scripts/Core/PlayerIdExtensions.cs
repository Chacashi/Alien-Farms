namespace AlienFarms.Core
{
    /// <summary>Helpers for the two-player <see cref="PlayerId"/>.</summary>
    public static class PlayerIdExtensions
    {
        /// <summary>The opponent of <paramref name="id"/> (None for None).</summary>
        public static PlayerId Other(this PlayerId id)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return PlayerId.Player2;
                case PlayerId.Player2:
                    return PlayerId.Player1;
                default:
                    return PlayerId.None;
            }
        }
    }
}
