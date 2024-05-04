using ExileCore.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence;

public class CraftingSequenceExecutor(IEnumerable<CraftingBase> itemsToSequence)
{
    public async SyncTask<bool> Execute(CancellationToken token)
    {
        var readFromInventory = Main.Settings.RunOptions.CraftInventoryInsteadOfCurrencyTab;
        foreach (var craftingBase in itemsToSequence)
        {
            var currentStepIndex = 0;
            CraftingStep previousStep = null;
            var stopwatch = new Stopwatch(); // Stopwatch to time each step
            var lastItemAddress = long.MinValue; // Add comparison to address somehow being the same as the last sequence, issue if there is.
            var endCraft = false;

            // Log initial item
            var asyncResult = readFromInventory
                ? new AsyncResult(await craftingBase.MethodReadInventoryItem(token))
                : new AsyncResult(await craftingBase.MethodReadStashItem(token));

            if (asyncResult.IsSuccess)
            {
                Logging.Logging.Add("## CraftingSequenceExecutor: Starting Item", LogMessageType.ItemData);
                ItemHandler.PrintHumanModListFromItem(asyncResult.Entity);
            }
            else
            {
                Logging.Logging.Add($"CraftingSequenceStep: Couldn't get StashSpecialSlot", LogMessageType.Error);
                Main.Stop();
            }

            while (!endCraft && currentStepIndex >= 0 && currentStepIndex < craftingBase.CraftingSteps.Count)
            {
                var currentStep = craftingBase.CraftingSteps[currentStepIndex];
                var success = false; // Defaulting success to false

                try
                {
                    // Log item mods before each step
                    asyncResult = readFromInventory
                        ? new AsyncResult(await craftingBase.MethodReadInventoryItem(token))
                        : new AsyncResult(await craftingBase.MethodReadStashItem(token));

                    if (asyncResult.IsSuccess)
                    {
                        var currentItemAddress = asyncResult.Address;
                        if (lastItemAddress != long.MinValue)
                        {
                            if (previousStep is { CheckType: not ConditionalCheckType.ConditionalCheckOnly })
                            {
                                if (lastItemAddress == currentItemAddress)
                                {
                                    Logging.Logging.Add($"CraftingSequenceStep: Item Address is the same as the last", LogMessageType.Special);
                                    Logging.Logging.Add($"CraftingSequenceStep: (True) LastAddress[{lastItemAddress:X}], CurrentAddress[{currentItemAddress:X}].", LogMessageType.Special);
                                }
                                else
                                {
                                    Logging.Logging.Add($"CraftingSequenceStep: Item Address is not the same as the last", LogMessageType.Special);
                                    Logging.Logging.Add($"CraftingSequenceStep: (False) LastAddress[{lastItemAddress:X}], CurrentAddress[{currentItemAddress:X}].", LogMessageType.Special);
                                }

                                ItemHandler.PrintHumanModListFromItem(asyncResult.Entity);
                            }
                        }

                        lastItemAddress = currentItemAddress;
                    }
                    else
                    {
                        Logging.Logging.Add($"CraftingSequenceStep: Couldn't get StashSpecialSlot", LogMessageType.Error);
                        Main.Stop();
                    }

                    // Info: Starting a new step
                    Logging.Logging.Add($"CraftingSequenceStep: Executing step [{currentStepIndex + 1}]", LogMessageType.Special);

                    stopwatch.Restart(); // Start timing

                    if (currentStep.ConditionalCheckGroups.Count != 0 && currentStep.CheckType == ConditionalCheckType.ConditionalCheckOnly)
                    {
                        // Count how many conditions are true and check if it meets or exceeds the required count
                        success = await EvaluateConditionsAsync(currentStep, token);

                        Logging.Logging.Add($"CraftingSequenceStep: All ConditionalChecks for ConditionalCheckOnly {success}", LogMessageType.Special);
                    }
                    else
                    {
                        // Execute the method if no prior conditional check or if it's not applicable
                        var methodResult = await currentStep.Method(token);

                        Logging.Logging.Add($"CraftingSequenceStep: Method result is {methodResult}", LogMessageType.Special);
                    }

                    if (currentStep.ConditionalCheckGroups.Count != 0 && currentStep.CheckType == ConditionalCheckType.ModifyThenCheck)
                    {
                        // Count how many conditions are true and check if it meets or exceeds the required count
                        success = await EvaluateConditionsAsync(currentStep, token);

                        Logging.Logging.Add($"CraftingSequenceStep: All ConditionalChecks after method are {success}", LogMessageType.Special);
                    }

                    if (currentStep.AutomaticSuccess)
                    {
                        success = true; // Override success if AutomaticSuccess is true
                        Logging.Logging.Add($"CraftingSequenceStep: AutomaticSuccess is {success}", LogMessageType.Special);
                    }

                    stopwatch.Stop(); // Stop timing after the step is executed

                    // Profiler: Time taken for step execution
                    Logging.Logging.Add($"CraftingSequenceStep: Step [{currentStepIndex + 1}] completed in {stopwatch.ElapsedMilliseconds} ms", LogMessageType.Profiler);
                    previousStep = currentStep;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Add($"CraftingSequenceExecutor: Exception caught while executing step {currentStepIndex + 1}:\n{ex}", LogMessageType.Error);

                    Logging.Logging.Add($"CraftingSequenceStep: Step [{currentStepIndex + 1}] failed after {stopwatch.ElapsedMilliseconds} ms", LogMessageType.Profiler);

                    return false;
                }

                // Determine the next step based on success or failure
                if (success)
                {
                    UpdateOperationStepsDictionary(currentStepIndex, true);

                    Logging.Logging.Add($"CraftingSequenceStep: Sequence result {currentStep.SuccessAction}", LogMessageType.Special);

                    switch (currentStep.SuccessAction)
                    {
                        case SuccessAction.Continue:
                            currentStepIndex++;
                            break;
                        case SuccessAction.End:
                            endCraft = true;
                            break; // End the execution of the sequence
                        case SuccessAction.GoToStep:
                            currentStepIndex = currentStep.SuccessActionStepIndex;
                            break;
                    }
                }
                else
                {
                    UpdateOperationStepsDictionary(currentStepIndex, false);

                    Logging.Logging.Add($"CraftingSequenceStep: Sequence result {currentStep.FailureAction}", LogMessageType.Special);

                    switch (currentStep.FailureAction)
                    {
                        case FailureAction.RepeatStep:
                            // Stay on the current step
                            break;
                        case FailureAction.Restart:
                            currentStepIndex = 0; // Restart from the first step
                            break;
                        case FailureAction.GoToStep:
                            currentStepIndex = currentStep.FailureActionStepIndex;
                            break;
                    }
                }

                // Info: Next step to be executed
                Logging.Logging.Add(currentStepIndex < craftingBase.CraftingSteps.Count - 1 // Check if it's not the last step
                    ? $"CraftingSequenceStep: Next step is [{currentStepIndex + 1}]"
                    // If it's the last step, you might want to log a different message or nothing at all
                    : "CraftingSequenceStep: Reached the last step in the sequence.", LogMessageType.Special);
            }

            // Log item mods at the end of crafting
            asyncResult = readFromInventory
                ? new AsyncResult(await craftingBase.MethodReadInventoryItem(token))
                : new AsyncResult(await craftingBase.MethodReadStashItem(token));

            if (asyncResult.IsSuccess)
            {
                Logging.Logging.Add("## CraftingSequenceExecutor: End Item", LogMessageType.ItemData);
                ItemHandler.PrintHumanModListFromItem(asyncResult.Entity);
            }
        }
        

        // Info: Sequence completed successfully
        Logging.Logging.Add("CraftingSequenceExecutor: Sequence execution completed successfully.", LogMessageType.Special);

        return true;

        static async SyncTask<bool> EvaluateConditionsAsync(CraftingStep currentStep, CancellationToken token)
        {
            var andResult = true; // Start true for AND logic
            var orResult = false; // Start false for OR logic

            foreach (var group in currentStep.ConditionalCheckGroups)
            {
                var trueCount = await CountTrueAsync(group.ConditionalChecks, token);

                if (!trueCount.result)
                {
                    Logging.Logging.Add("EvaluateConditionsAsync: At some point we couldn't find our item in the slot wanted, stopping", LogMessageType.Error);

                    Main.Stop();
                }

                switch (group.GroupType)
                {
                    case ConditionGroup.AND:
                        andResult &= trueCount.trueCount >= group.ConditionalsToBePassForSuccess;

                        Logging.Logging.Add($"AND Group Result: {andResult} (True Count: {trueCount.trueCount}, Required: {group.ConditionalsToBePassForSuccess})", LogMessageType.Evaluation);

                        break;
                    case ConditionGroup.OR:
                        orResult |= trueCount.trueCount >= group.ConditionalsToBePassForSuccess;

                        Logging.Logging.Add($"OR Group Result: {orResult} (True Count: {trueCount.trueCount}, Required: {group.ConditionalsToBePassForSuccess})", LogMessageType.Evaluation);

                        break;
                    case ConditionGroup.NOT:
                        if (trueCount.trueCount > 0)
                        {
                            Logging.Logging.Add("NOT Group Result: False (At least one condition is true)", LogMessageType.Evaluation);

                            return false;
                        }

                        Logging.Logging.Add("NOT Group Result: True (No conditions are true)", LogMessageType.Evaluation);

                        break;
                }

                if (andResult)
                {
                    continue;
                }

                Logging.Logging.Add("Exiting early due to AND group result being false", LogMessageType.Evaluation);
                return false;
            }

            var combinedResult = andResult || orResult;
            Logging.Logging.Add($"Final Combined Result: {combinedResult}", LogMessageType.Evaluation);
            return combinedResult;
        }

        static async SyncTask<(bool result, int trueCount)> CountTrueAsync(IEnumerable<Func<CancellationToken, SyncTask<(bool result, bool isMatch)>>> conditionalChecks, CancellationToken token)
        {
            var trueCount = 0;
            var allSuccessful = true;

            foreach (var condition in conditionalChecks)
            {
                var (result, isMatch) = await condition(token);

                if (!result)
                {
                    allSuccessful = false;
                    break;
                }

                if (isMatch)
                {
                    trueCount++;
                }
            }

            return (allSuccessful, trueCount);
        }

        static void UpdateOperationStepsDictionary(int step, bool pass)
        {
            Main.CurrentOperationStepCountList ??= [];

            if (Main.CurrentOperationStepCountList.TryGetValue(step, out var currentCount))
            {
                if (pass)
                {
                    Main.CurrentOperationStepCountList[step] = (currentCount.passCount + 1, currentCount.failCount, currentCount.totalCount + 1);
                }
                else
                {
                    Main.CurrentOperationStepCountList[step] = (currentCount.passCount, currentCount.failCount + 1, currentCount.totalCount + 1);
                }
            }
            else
            {
                Main.CurrentOperationStepCountList[step] = pass ? (1, 0, 1) : (0, 1, 1);
            }
        }
    }
}