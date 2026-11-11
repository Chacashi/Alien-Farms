namespace AlienFarms.Menu
{
    /// <summary>Screens of the MainMenu scene, in flow order.</summary>
    public enum MenuPhase
    {
        Title = 0,
        CharacterSelect = 1,
        Loading = 2,

        /// <summary>A sub-menu opened from the title (Options, How to play); the title owner navigates it.</summary>
        SubMenu = 3
    }
}
