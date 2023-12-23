using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ExileCore.Shared;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class GameHandler
{
    public static async SyncTask<bool> AsyncWaitServerLatency(CancellationToken token)
    {
        await AsyncWait(Main.ServerLatency, token);
        return true;
    }

    public static async SyncTask<bool> AsyncWaitRandomRange(Vector2 relayRange, CancellationToken token)
    {
        await AsyncWait(HelperHandler.GetRandomTimeInRange(relayRange), token);
        return true;
    }

    public static async SyncTask<bool> AsyncWait(int delay, CancellationToken token)
    {
        await Task.Delay(delay, token);
        return true;
    }

    public static bool IsInGameCondition()
    {
        return Main.GameController?.Game?.IngameState?.InGame ?? false;
    }
}