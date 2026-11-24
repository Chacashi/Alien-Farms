namespace AlienFarms.Networking.Interest
{
    public struct InterestCounters
    {
        public long Shows { get; internal set; }
        public long Hides { get; internal set; }
        public int VisibleAnimals { get; internal set; }
        public int VisibleBombs { get; internal set; }

        public InterestCounters(long shows, long hides, int visibleAnimals, int visibleBombs)
        {
            Shows = shows;
            Hides = hides;
            VisibleAnimals = visibleAnimals;
            VisibleBombs = visibleBombs;
        }
    }
}
