using ExileCore;
using ExileCore.Shared;
using System.Threading;
using System.Windows.Forms;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class KeyHandler
    {
        public static async SyncTask<bool> AsyncButtonPress(Keys button, CancellationToken token) =>
            await AsyncIsButtonDown(button, token) && await AsyncIsButtonUp(button, token);

        public static async SyncTask<bool> AsyncIsButtonDown(Keys button, CancellationToken token)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                action: () => PerformButtonAction(button, pressDown: true),
                condition: () => Input.IsKeyDown(button),
                timeoutS: Main.Settings.ActionTimeoutInSeconds,
                loopDelay: HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
                token: token);
        }

        public static async SyncTask<bool> AsyncIsButtonUp(Keys button, CancellationToken token)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                action: () => PerformButtonAction(button, pressDown: false),
                condition: () => !Input.IsKeyDown(button),
                timeoutS: Main.Settings.ActionTimeoutInSeconds,
                loopDelay: HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
                token: token);
        }

        public static async SyncTask<bool> AsyncSetButtonDown(Keys button, CancellationToken token) =>
            await AsyncIsButtonDown(button, token);

        public static void PerformButtonAction(Keys button, bool pressDown)
        {
            if (pressDown)
            {
                if (button == Keys.LButton) Input.LeftDown();
                else if (button == Keys.RButton) Input.RightDown();
                else Input.KeyDown(button);
            }
            else
            {
                if (button == Keys.LButton) Input.LeftUp();
                else if (button == Keys.RButton) Input.RightUp();
                else Input.KeyUp(button);
            }
        }
    }
}