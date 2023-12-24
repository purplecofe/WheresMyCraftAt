using SharpDX;
using System;
using Vector2 = System.Numerics.Vector2;

namespace WheresMyCraftAt.Handlers;

public static class HelperHandler
{
    private const float PerlinTimeResetThreshold = 10000.0f; // Set your desired reset threshold
    private static readonly Random random = new();

    private static float perlinTime; // This variable will keep track of the "time" for Perlin noise

    public static int GetRandomTimeInRange(Vector2 timeRange)
    {
        var minMilliseconds = (int)timeRange.X;
        var maxMilliseconds = (int)timeRange.Y;
        return random.Next(minMilliseconds, maxMilliseconds + 1);
    }

    public static int GetRandomValue(int minValue = 0, int maxValue = 100)
    {
        // Clamp the values to ensure min is not above 100 and max is not below 5
        minValue = Math.Clamp(minValue, 5, 100);
        maxValue = Math.Clamp(maxValue, 5, 100);

        // Correct the range if min is greater than max
        if (minValue > maxValue)
        {
            minValue = 25;
            maxValue = 75;
        }

        // The upper bound of Random.Next is exclusive, hence adding 1 to include maxValue in the range.
        return random.Next(minValue, maxValue + 1);
    }

    public static Vector2 GetRandomPointWithinWithPerlin(this RectangleF rect, float floor, float ceiling)
    {
        // Increment the Perlin time, reset if it exceeds the threshold
        perlinTime += GetRandomValue(1, 10);

        if (perlinTime > PerlinTimeResetThreshold)
            perlinTime = 0.0f;

        // Generate a Perlin noise value for shrink percentage
        var noiseValue = PerlinNoiseHandler.Generate(perlinTime / 100.0f, 0);
        var range = ceiling - floor;
        var dynamicShrinkPercentage = (noiseValue + 1) * 0.5f * range + floor;
        dynamicShrinkPercentage = Math.Clamp(dynamicShrinkPercentage, floor, ceiling);

        Logging.Logging.Add(
            $"Generated Perlin noise value: {noiseValue} (Dynamic shrink percentage: {dynamicShrinkPercentage})",
            Enums.WheresMyCraftAt.LogMessageType.Debug
        );

        // Calculate the shrinkage in terms of width and height
        var shrinkWidth = rect.Width * dynamicShrinkPercentage / 100f;
        var shrinkHeight = rect.Height * dynamicShrinkPercentage / 100f;

        // Create a smaller rectangle
        var smallerRect = new RectangleF(
            rect.Left + shrinkWidth / 2,
            rect.Top + shrinkHeight / 2,
            rect.Width - shrinkWidth,
            rect.Height - shrinkHeight
        );

        // Generate a random point within the smaller rectangle
        var x = (int)(random.NextDouble() * smallerRect.Width + smallerRect.Left);
        var y = (int)(random.NextDouble() * smallerRect.Height + smallerRect.Top);
        return new Vector2(x, y);
    }

    public static bool IsAddressSameCondition(long addressFirst, long addressSecond) => addressFirst == addressSecond;
}