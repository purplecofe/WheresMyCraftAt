using ExileCore;
using ExileCore.Shared;
using System.Threading;
using System.Windows.Forms;

namespace WheresMyCraftAt.Handlers
{
    public static class KeyHandler

    {
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
        }

        public static async SyncTask<bool> AsyncButtonPress(Keys button, CancellationToken token) =>
            await AsyncIsButtonDown(button, token) && await AsyncIsButtonUp(button, token);

        public static async SyncTask<bool> AsyncIsButtonDown(Keys button, CancellationToken token)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                action: () => PerformButtonAction(button, true),
                condition: () => Input.GetKeyState(button),
                timeoutS: Main.Settings.ActionTimeoutInSeconds,
                token: token);
        }

        public static async SyncTask<bool> AsyncIsButtonUp(Keys button, CancellationToken token)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                action: () => PerformButtonAction(button, false),
                condition: () => !Input.GetKeyState(button),
                timeoutS: Main.Settings.ActionTimeoutInSeconds,
                token: token);
        }

        public static async SyncTask<bool> AsyncSetButtonDown(Keys button, CancellationToken token) =>
            await AsyncIsButtonDown(button, token);

        public static void PerformButtonAction(Keys button, bool press)
        {
            if (press)
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