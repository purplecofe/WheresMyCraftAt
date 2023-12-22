using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class KeyHandler
{
    public static async SyncTask<bool> AsyncButtonPress(Keys button, CancellationToken token)
    {
        return await AsyncIsButtonDown(button, token) && await AsyncIsButtonUp(button, token);
    }

    public static async SyncTask<bool> AsyncIsButtonDown(Keys button, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => PerformButtonAction(button, true),
            () => Input.IsKeyDown(button),
            Main.Settings.ActionTimeoutInSeconds,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token);
    }

    public static async SyncTask<bool> AsyncIsButtonUp(Keys button, CancellationToken token)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => PerformButtonAction(button, false),
            () => !Input.IsKeyDown(button),
            Main.Settings.ActionTimeoutInSeconds,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token);
    }

    public static async SyncTask<bool> AsyncSetButtonDown(Keys button, CancellationToken token)
    {
        return await AsyncIsButtonDown(button, token);
    }

    public static void PerformButtonAction(Keys button, bool pressDown)
    {
        if (pressDown)
        {
            switch (button)
            {
                case Keys.LButton:
                    Input.LeftDown();
                    break;
                case Keys.RButton:
                    Input.RightDown();
                    break;
                default:
                    Input.KeyDown(button);
                    break;
            }
        }
        else
        {
            switch (button)
            {
                case Keys.LButton:
                    Input.LeftUp();
                    break;
                case Keys.RButton:
                    Input.RightUp();
                    break;
                default:
                    Input.KeyUp(button);
                    break;
            }
        }
    }
}