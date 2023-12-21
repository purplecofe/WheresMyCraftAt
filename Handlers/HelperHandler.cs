using ExileCore;
using System;
using System.Numerics;

namespace WheresMyCraftAt.Handlers
{
    public static class HelperHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static int GetRandomTimeInRange(Vector2 timeRange)
        {
            Random random = new Random();
            int minMilliseconds = (int)timeRange.X;
            int maxMilliseconds = (int)timeRange.Y;

            return random.Next(minMilliseconds, maxMilliseconds + 1);
        }
    }
}