using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>Something that runs away from a sudden threat (e.g. a blast). Only free, grounded animals react.</summary>
    public interface IScareable
    {
        /// <summary>Flees from <paramref name="threatPosition"/> for at least <paramref name="minDuration"/> seconds.</summary>
        void Scare(Vector3 threatPosition, float minDuration);
    }
}
