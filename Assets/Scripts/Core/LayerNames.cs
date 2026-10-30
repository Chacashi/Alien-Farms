namespace AlienFarms.Core
{
    /// <summary>Physics layer names as configured in Tags and Layers. Resolve to indices once, never per frame.</summary>
    public static class LayerNames
    {
        public const string Ground = "Ground";
        public const string Environment = "Environment";
        public const string Player = "Player";
        public const string Animal = "Animal";
        public const string Zone = "Zone";
        public const string CapturedAnimal = "CapturedAnimal";
        public const string ZoneBarrier = "ZoneBarrier";
        public const string Bomb = "Bomb";
    }
}
