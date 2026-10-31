namespace AlienFarms.Input
{
    /// <summary>
    /// Plain C# edge detector that turns a continuous axis (stick, dpad) into discrete steps: -1 or +1 once when the axis
    /// crosses the threshold, 0 otherwise. Holding does not repeat; returning inside the threshold re-arms it.
    /// </summary>
    public sealed class AxisStepDetector
    {
        // Sign of the step currently held (-1, 0, +1).
        private int _heldSign;

        public int Update(float axis, float threshold)
        {
            int sign = axis >= threshold ? 1 : axis <= -threshold ? -1 : 0;
            if (sign == _heldSign)
            {
                return 0;
            }

            _heldSign = sign;
            return sign;
        }

        public void Reset() => _heldSign = 0;
    }
}
