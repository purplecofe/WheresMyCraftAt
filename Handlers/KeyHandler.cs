using ExileCore;
using ExileCore.Shared;
using System.Threading;
using System.Windows.Forms;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class KeyHandler
{
    public static async SyncTask<bool> AsyncButtonPress(Keys button, CancellationToken token)
    {
        Logging.Logging.LogMessage($"Attempting to press button: {button}", Enums.WheresMyCraftAt.LogMessageType.Info);
        var isButtonDown = await AsyncIsButtonDown(button, token);
        var isButtonUp = await AsyncIsButtonUp(button, token);

        Logging.Logging.LogMessage($"Button press result for {button}: Down - {isButtonDown}, Up - {isButtonUp}", Enums.WheresMyCraftAt.LogMessageType.Info);

        return isButtonDown && isButtonUp;
    }

    public static async SyncTask<bool> AsyncIsButtonDown(Keys button, CancellationToken token)
    {
        Logging.Logging.LogMessage($"Checking if button is down: {button}", Enums.WheresMyCraftAt.LogMessageType.Info);

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => PerformButtonAction(button, true), () => Input.IsKeyDown(button),
            Main.Settings.DelayOptions.ActionTimeoutInSeconds, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token);

        Logging.Logging.LogMessage($"Button down check result for {button}: {result}", Enums.WheresMyCraftAt.LogMessageType.Info);

        return result;
    }

    public static async SyncTask<bool> AsyncIsButtonUp(Keys button, CancellationToken token)
    {
        Logging.Logging.LogMessage($"Checking if button is up: {button}", Enums.WheresMyCraftAt.LogMessageType.Info);

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => PerformButtonAction(button, false), () => !Input.IsKeyDown(button),
            Main.Settings.DelayOptions.ActionTimeoutInSeconds, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token);

        Logging.Logging.LogMessage($"Button up check result for {button}: {result}", Enums.WheresMyCraftAt.LogMessageType.Info);

        return result;
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