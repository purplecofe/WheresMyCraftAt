using ExileCore;
using ExileCore.Shared;
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

        public static async SyncTask<bool> AsyncIsMouseInPlace(Vector2N position, CancellationToken token)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                action: () => SetCursorPositionAction(position),
                condition: () => IsMouseInPositionCondition(position),
                timeoutS: Main.Settings.ActionTimeoutInSeconds,
                token: token);
        }

        public static async SyncTask<bool> AsyncMoveMouse(Vector2N position, CancellationToken token) => await AsyncIsMouseInPlace(position, token);

        public static Vector2N GetCurrentMousePosition() => new(GC.IngameState.MousePosX, GC.IngameState.MousePosY);

        public static Vector2N GetRelativeWinPos(Vector2N position)
        {
            return new Vector2N(
                position.X + Main._clickWindowOffset.X,
                position.Y + Main._clickWindowOffset.Y
            );
        }

        public static bool IsMouseInPositionCondition(Vector2N position) => GetCurrentMousePosition() == position;

        public static void SetCursorPositionAction(Vector2N position) => Input.SetCursorPos(GetRelativeWinPos(position));
    }
}