using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Heaviest animal: slow to react and to flee (straight line), worth the most points. Its carry and throw penalties
    /// live in its <see cref="AlienFarms.Data.AnimalData"/>.
    /// </summary>
    public sealed class Cow : Animal
    {
        protected override Vector3 ComputeFleeDirection(Vector3 awayFromThreat) => awayFromThreat;
    }
}
