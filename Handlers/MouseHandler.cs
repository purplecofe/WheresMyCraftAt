using ExileCore;
using ExileCore.Shared;
using System;
using System.Threading;
using Vector2N = System.Numerics.Vector2;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class MouseHandler
{
    public static async SyncTask<bool> AsyncIsMouseInPlace(Vector2N position, bool applyOffset, CancellationToken token)
    {
        Logging.Logging.Add(
            $"Checking if mouse is in the desired position at {position} (Offset applied: {applyOffset}).",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => SetCursorPositionAction(position, applyOffset),
            () => IsMouseInPositionCondition(position),
            Main.Settings.ActionTimeoutInSeconds,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );

        Logging.Logging.Add(
            $"Mouse position check result: {result} (Desired position: {position})",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        return result;
    }

    public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, CancellationToken token)
    {
        return await AsyncMoveMouse(position, true, token);
    }

    public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, bool applyOffset, CancellationToken token)
    {
        Logging.Logging.Add(
            $"Moving mouse to position {position} (Offset applied: {applyOffset}).",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        var normalizedPosition = NormalizePosition(position);
        var result = await AsyncIsMouseInPlace(normalizedPosition, applyOffset, token);

        Logging.Logging.Add(
            $"Mouse move result: {result} (Target position: {normalizedPosition})",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        return result;
    }

    public static Vector2N GetCurrentMousePosition()
    {
        return new Vector2N(Main.GameController.IngameState.MousePosX, Main.GameController.IngameState.MousePosY);
    }

    public static Vector2N GetRelativeWinPos(Vector2N position)
    {
        return new Vector2N(position.X + Main.ClickWindowOffset.X, position.Y + Main.ClickWindowOffset.Y);
    }

    public static bool IsMouseInPositionCondition(Vector2N position)
    {
        return GetCurrentMousePosition() == position;
    }

    public static void SetCursorPositionAction(Vector2N position, bool applyOffset)
    {
        Input.SetCursorPos(
            applyOffset
                ? GetRelativeWinPos(position)
                : position
        );
    }

    private static Vector2N NormalizePosition(Vector2N position)
    {
        // Round to nearest integer
        var x = (int)Math.Round(position.X);
        var y = (int)Math.Round(position.Y);
        /* Might use this at a later date to verify we are clicking within bounds of the game window */

        // Check and adjust the coordinates to ensure they're within the desired range
        // Example: Ensure x and y are between 0 and 1920 for x, and 0 and 1080 for y (or your screen resolution)
        //x = Math.Clamp(x, 0, 1920);
        //y = Math.Clamp(y, 0, 1080);
        return new Vector2N(x, y);
    }
}