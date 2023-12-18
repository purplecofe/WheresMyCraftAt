using ExileCore;
using ExileCore.Shared;
using System;
using System.Threading;

namespace WheresMyCraftAt.Handlers
{
    public static class ExecuteHandler
    {
        private static GameController GC;

        public static void Initialize(WheresMyCraftAt main)
        {
            GC = main.GameController;
        }

        public static async SyncTask<bool> ExecuteWithCancellationHandling(Func<bool> condition, int timeoutS, CancellationToken token)
        {
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    await GameHandler.AsyncWaitServerLatency(ctsTimeout.Token);

                    if (condition())
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public static async SyncTask<bool> ExecuteWithCancellationHandling(Action action, Func<bool> condition, int timeoutS, CancellationToken token)
        {
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    action();
                    await GameHandler.AsyncWaitServerLatency(ctsTimeout.Token);

                    if (condition()) return true;
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}