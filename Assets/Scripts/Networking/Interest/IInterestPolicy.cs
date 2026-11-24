using AlienFarms.Networking.Player;

namespace AlienFarms.Networking.Interest
{
    public interface IInterestPolicy
    {
        bool Evaluate(NetworkCarriable target, ulong clientId);
        void EnsureVisibleTo(NetworkCarriable target, ulong clientId);
    }
}
