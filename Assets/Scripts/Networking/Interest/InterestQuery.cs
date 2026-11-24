namespace AlienFarms.Networking.Interest
{
    public readonly struct InterestQuery
    {
        public bool Enabled { get; }
        public bool KindFiltered { get; }
        public bool IsServerClient { get; }
        public bool IsOwner { get; }
        public bool IsExempt { get; }
        public bool HasObserverPosition { get; }
        public bool WasVisible { get; }
        public float SqrDistance { get; }

        public InterestQuery(bool enabled, bool kindFiltered, bool isServerClient, bool isOwner,
            bool isExempt, bool hasObserverPosition, bool wasVisible, float sqrDistance)
        {
            Enabled = enabled;
            KindFiltered = kindFiltered;
            IsServerClient = isServerClient;
            IsOwner = isOwner;
            IsExempt = isExempt;
            HasObserverPosition = hasObserverPosition;
            WasVisible = wasVisible;
            SqrDistance = sqrDistance;
        }
    }
}
