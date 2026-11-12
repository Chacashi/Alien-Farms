namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Every text the ownership debug label can show ("Owner: P2 · IsOwner ✓ · HasAuthority ✗"), built once on first
    /// use and then returned by reference: changing the label never allocates.
    /// </summary>
    public sealed class OwnershipLabelCache
    {
        public const string OwnerPrefix = "Owner: ";
        public const string Separator = " · ";
        public const string IsOwnerCaption = "IsOwner ";
        public const string HasAuthorityCaption = "HasAuthority ";

        private const int VariantsPerTag = 4;

        private readonly string _yesMark;
        private readonly string _noMark;
        private readonly string[] _texts = new string[CarriableOwnerRules.TagCount * VariantsPerTag];

        public OwnershipLabelCache(string yesMark, string noMark)
        {
            _yesMark = yesMark ?? string.Empty;
            _noMark = noMark ?? string.Empty;
        }

        public string Get(CarriableOwner owner, bool isOwner, bool hasAuthority)
        {
            int index = CarriableOwnerRules.TagIndex(owner) * VariantsPerTag + (isOwner ? 2 : 0) + (hasAuthority ? 1 : 0);
            return _texts[index] ??= Format(CarriableOwnerRules.Tag(owner), isOwner, hasAuthority);
        }

        private string Format(string ownerTag, bool isOwner, bool hasAuthority) =>
            OwnerPrefix + ownerTag
            + Separator + IsOwnerCaption + (isOwner ? _yesMark : _noMark)
            + Separator + HasAuthorityCaption + (hasAuthority ? _yesMark : _noMark);
    }
}
