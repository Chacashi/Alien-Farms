using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>Heavy, slow animal that flees in a straight line.</summary>
    public sealed class Sheep : Animal
    {
        protected override Vector3 ComputeFleeDirection(Vector3 awayFromThreat) => awayFromThreat;
    }
}
