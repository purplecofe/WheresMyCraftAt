using ExileCore;
using ExileCore.Shared;
using InputHumanizer.Input;
using System;
using System.Threading;
using Vector2N = System.Numerics.Vector2;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class MouseHandler
{
    private static IInputController _inputController;

    // Testing seems fine, need to keep using this to feel confident
    public static async SyncTask<bool> AsyncInputHumanizerMoveMouse(Vector2N position, bool applyOffset, CancellationToken token)
    {
        var newPos = applyOffset ? GetRelativeWinPos(position) : position;
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(Main.Settings.DelayOptions.ActionTimeoutInSeconds * 2));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                var tryGetInputController = Main.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");

                if (tryGetInputController is null)
                {
                    Logging.Logging.Add($"{Main.Name}: Failed to get Input Controller. Have you installed InputHumanizer?", Enums.WheresMyCraftAt.LogMessageType.Error);

                    Main.Stop();
                    return false;
                }

                if ((_inputController = tryGetInputController(Main.Name)) is null)
                {
                    return false;
                }

                using (_inputController)
                {
                    if (!await _inputController.MoveMouse(newPos, token))
                    {
                        Logging.Logging.Add($"InputHumanizerMoveMouse: Failed to move mouse to desired position: {newPos}", Enums.WheresMyCraftAt.LogMessageType.Warning);
                    }
                }

                await GameHandler.AsyncWait(HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), ctsTimeout.Token);

                if (!IsMouseInPositionCondition(position))
                {
                    continue;
                }

                Logging.Logging.Add($"InputHumanizerMoveMouse: Mouse ended up in position: {GetCurrentMousePosition()}", Enums.WheresMyCraftAt.LogMessageType.Info);

                return true;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> AsyncSetMouseInPlace(Vector2N position, bool applyOffset, CancellationToken token)
    {
        Logging.Logging.Add($"Checking if mouse is in the desired position at {position} (Offset applied: {applyOffset}).", Enums.WheresMyCraftAt.LogMessageType.Info);

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => SetCursorPositionAction(position, applyOffset), () => IsMouseInPositionCondition(position),
            Main.Settings.DelayOptions.ActionTimeoutInSeconds, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token);

        Logging.Logging.Add($"Mouse position check result: {result} (Desired position: {position})", Enums.WheresMyCraftAt.LogMessageType.Info);

        return result;
    }

    public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, CancellationToken token) =>
        await AsyncMoveMouse(position, true, token);

    public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, bool applyOffset, CancellationToken token)
    {
        Logging.Logging.Add($"Moving mouse to position {position} (Offset applied: {applyOffset}).", Enums.WheresMyCraftAt.LogMessageType.Info);

        var normalizedPosition = NormalizePosition(position);
        // uncomment to enable InputHumanizer and comment line under.
        //var result = await AsyncInputHumanizerMoveMouse(normalizedPosition, applyOffset, token);
        var result = await AsyncSetMouseInPlace(normalizedPosition, applyOffset, token);

        Logging.Logging.Add($"Mouse move result: {result} (Target position: {normalizedPosition})", Enums.WheresMyCraftAt.LogMessageType.Info);

        return result;
    }

    public static Vector2N GetCurrentMousePosition() => new Vector2N(Main.GameController.IngameState.MousePosX, Main.GameController.IngameState.MousePosY);

    public static Vector2N GetRelativeWinPos(Vector2N position) => new Vector2N(position.X + Main.ClickWindowOffset.X, position.Y + Main.ClickWindowOffset.Y);

    public static bool IsMouseInPositionCondition(Vector2N position) => GetCurrentMousePosition() == position;

    public static void SetCursorPositionAction(Vector2N position, bool applyOffset) =>
        Input.SetCursorPos(applyOffset ? GetRelativeWinPos(position) : position);

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