using System;

namespace MediaPlaybackLib
{
    public static class TimeSpanExtensions
    {
        public static bool InRange(this TimeSpan value, TimeSpan start, TimeSpan end)
        {
            return value >= start && value <= end;
        }

        public static TimeSpan EnsureInRange(this TimeSpan value, TimeSpan start, TimeSpan end)
        {
            if (value < start)
                return start;
            if (value > end)
                return end;
            return value;
        }

        public static TimeSpan Max(this TimeSpan value, params TimeSpan[] maxValues)
        {
            foreach (var maxValue in maxValues)
            {
                if (value < maxValue)
                    value = maxValue;
            }
            return value;
        }

        public static TimeSpan Min(this TimeSpan value, params TimeSpan[] minValues)
        {
            foreach (var minValue in minValues)
            {
                if (value > minValue)
                    value = minValue;
            }
            return value;
        }
    }
}
