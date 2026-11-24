using UnityEngine;

namespace AlienFarms.Networking.Interest
{
    public readonly struct InterestRadii
    {
        public float EnterSqr { get; }
        public float ExitSqr { get; }

        private InterestRadii(float enterSqr, float exitSqr)
        {
            EnterSqr = enterSqr;
            ExitSqr = exitSqr;
        }

        public static InterestRadii Create(float enter, float exit)
        {
            exit = Mathf.Max(enter, exit);
            return new InterestRadii(enter * enter, exit * exit);
        }
    }
}
