namespace AlienFarms.Networking.Diagnostics
{
    public readonly struct KnownObjectsCounts
    {
        public int Total => Animals + Bombs + Other;
        public int Animals { get; }
        public int Bombs { get; }
        public int Other { get; }

        public KnownObjectsCounts(int animals, int bombs, int other)
        {
            Animals = animals;
            Bombs = bombs;
            Other = other;
        }
    }
}
