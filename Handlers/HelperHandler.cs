using System;
using System.Numerics;

namespace WheresMyCraftAt.Handlers;

public static class HelperHandler
{
    public static int GetRandomTimeInRange(Vector2 timeRange)
    {
        var random = new Random();
        var minMilliseconds = (int)timeRange.X;
        var maxMilliseconds = (int)timeRange.Y;

        return random.Next(minMilliseconds, maxMilliseconds + 1);
    }
}