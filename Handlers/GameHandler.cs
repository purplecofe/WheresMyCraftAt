using ExileCore;
using ExileCore.Shared;
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
            await Task.Delay(Main.ServerLatency, token);

            return true;
        }

        public static bool IsInGameCondition() => GC?.Game?.IngameState?.InGame ?? false;
    }
}