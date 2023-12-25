using ExileCore.Shared;
using System;
using System.Threading;

namespace WheresMyCraftAt.Handlers;

public static class ExecuteHandler
{
    public static async SyncTask<bool> AsyncExecuteWithCancellationHandling(Func<bool> condition, int timeoutS,
        CancellationToken token)
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

    public static async SyncTask<bool> AsyncExecuteWithCancellationHandling(Func<bool> condition, int timeoutS,
        int loopDelay, CancellationToken token)
    {
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                await GameHandler.AsyncWait(loopDelay, ctsTimeout.Token);

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

    public static async SyncTask<bool> AsyncExecuteWithCancellationHandling(Action action, Func<bool> condition,
        int timeoutS, int loopDelay, CancellationToken token)
    {
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                action();
                await GameHandler.AsyncWait(loopDelay, ctsTimeout.Token);

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
}