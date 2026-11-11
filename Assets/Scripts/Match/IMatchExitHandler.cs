namespace AlienFarms.Match
{
    /// <summary>What the match exit buttons do. Local: <see cref="LocalMatchExitHandler"/>; online, a network handler.</summary>
    public interface IMatchExitHandler
    {
        /// <summary>Asks for a new round with the same players.</summary>
        void RequestRematch();

        /// <summary>Leaves the match for the title screen. True once leaving has started (further requests are ignored).</summary>
        bool ReturnToTitle();
    }
}
