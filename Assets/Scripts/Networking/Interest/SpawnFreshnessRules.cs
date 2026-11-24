namespace AlienFarms.Networking.Interest
{
    public static class SpawnFreshnessRules
    {
        public static bool IsFresh(int spawnTick, int nowTick, int tickRate, float windowSeconds = 1f)
        {
            int age = unchecked(nowTick - spawnTick);
            float windowTicks = tickRate * windowSeconds;
            // NGO buffers client ServerTime, so a newly received spawn tick can be slightly ahead.
            return tickRate > 0 && age >= -windowTicks && age <= windowTicks;
        }
    }
}
