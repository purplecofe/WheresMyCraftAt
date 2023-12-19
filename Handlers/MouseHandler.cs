using ExileCore;
using ExileCore.Shared;
using System;
using System.Threading;
using Vector2N = System.Numerics.Vector2;

namespace WheresMyCraftAt.Handlers
{
    public static class MouseHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static async SyncTask<bool> AsyncIsMouseInPlace(Vector2N position, CancellationToken token) =>
            await AsyncIsMouseInPlace(position, true, token);

        public static async SyncTask<bool> AsyncIsMouseInPlace(Vector2N position, bool applyOffset, CancellationToken token)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                action: () => SetCursorPositionAction(position, applyOffset),
                condition: () => IsMouseInPositionCondition(position),
                timeoutS: Main.Settings.ActionTimeoutInSeconds,
                token: token);
        }

        public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, CancellationToken token) =>
            await AsyncMoveMouse(position, true, token);

        public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, bool applyOffset, CancellationToken token)
        {
            Main.DebugPrint($"Wanted Position({position})\n"
                          + $"NormalizedPosition({NormalizePosition(position)})", 
                WheresMyCraftAt.LogMessageType.Info);

            return await AsyncIsMouseInPlace(NormalizePosition(position), applyOffset, token);
        }

        public static Vector2N GetCurrentMousePosition() =>
            new(GC.IngameState.MousePosX, GC.IngameState.MousePosY);

        public static Vector2N GetRelativeWinPos(Vector2N position)
        {
            return new Vector2N(
                position.X + Main.ClickWindowOffset.X,
                position.Y + Main.ClickWindowOffset.Y
            );
        }

        public static bool IsMouseInPositionCondition(Vector2N position) => 
            GetCurrentMousePosition() == position;

        public static void SetCursorPositionAction(Vector2N position, bool applyOffset) =>
            Input.SetCursorPos(applyOffset ? GetRelativeWinPos(position) : position);

        private static Vector2N NormalizePosition(Vector2N position)
        {
            // Round to nearest integer
            int x = (int)Math.Round(position.X);
            int y = (int)Math.Round(position.Y);

            /* Might use this at a later date to verify we are clicking within bounds of the game window */

            // Check and adjust the coordinates to ensure they're within the desired range
            // Example: Ensure x and y are between 0 and 1920 for x, and 0 and 1080 for y (or your screen resolution)
            //x = Math.Clamp(x, 0, 1920);
            //y = Math.Clamp(y, 0, 1080);

            return new Vector2N(x, y);
        }
    }
}