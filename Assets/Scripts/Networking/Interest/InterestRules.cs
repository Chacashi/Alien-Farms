using UnityEngine;

namespace AlienFarms.Networking.Interest
{
    public static class InterestRules
    {
        public static bool ShouldBeVisible(in InterestQuery q, in InterestRadii r)
        {
            if (!q.Enabled || !q.KindFiltered || q.IsServerClient || q.IsOwner || q.IsExempt || !q.HasObserverPosition)
            {
                return true;
            }

            return q.WasVisible ? q.SqrDistance <= r.ExitSqr : q.SqrDistance < r.EnterSqr;
        }

        public static float SqrHorizontalDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        public static InterestChange Classify(bool wasVisible, bool shouldBeVisible)
        {
            if (wasVisible == shouldBeVisible)
            {
                return InterestChange.None;
            }

            return shouldBeVisible ? InterestChange.Show : InterestChange.Hide;
        }
    }
}
