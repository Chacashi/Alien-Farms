namespace AlienFarms.Core
{
    /// <summary>Screens of the online menu panel (resolved by AlienFarms.Menu.OnlineMenuRules.Resolve).</summary>
    public enum OnlineScreen
    {
        /// <summary>Signed in (or signed out) and in no session: create, join or go back.</summary>
        Hub = 0,

        /// <summary>Typing a join code.</summary>
        Join = 1,

        /// <summary>In a session: room code, character cards, seats and start.</summary>
        Room = 2,

        /// <summary>Waiting for the service (signing in, creating, joining, leaving, loading the match).</summary>
        Busy = 3
    }
}
