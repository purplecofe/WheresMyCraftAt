using ExileCore;
using ExileCore.Shared;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace WheresMyCraftAt.Handlers
{
    public static class GameHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

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

        public static bool IsInGameCondition() => GC?.Game?.IngameState?.InGame ?? false;
    }
}