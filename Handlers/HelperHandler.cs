using System;
using System.Numerics;

namespace WheresMyCraftAt.Handlers
{
    public static class HelperHandler
    {
        public static int GetRandomTimeInRange(Vector2 timeRange)
        {
            Random random = new Random();
            int minMilliseconds = (int)timeRange.X;
            int maxMilliseconds = (int)timeRange.Y;

            return random.Next(minMilliseconds, maxMilliseconds + 1);
        }
    }
}