using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ExileCore.Shared;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence;

public class CraftingSequenceExecutor(IReadOnlyList<CraftingStep> steps)
{
    public async SyncTask<bool> Execute(CancellationToken token)
    {
        var currentStepIndex = 0;
        var stopwatch = new Stopwatch(); // Stopwatch to time each step

        while (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var currentStep = steps[currentStepIndex];
            var success = false; // Defaulting success to false

            try
            {
                Logging.Logging.Add($"CraftingSequenceStep: Executing step [{currentStepIndex+1}]", LogMessageType.Info);
                stopwatch.Restart(); // Start timing

                if (currentStep.ConditionalChecks.Count != 0 && currentStep.CheckType == ConditionalCheckType.ConditionalCheckOnly)
                {
                    // Count how many conditions are true and check if it meets or exceeds the required count
                    success = currentStep.ConditionalChecks.Count(condition => condition()) >= currentStep.ConditionalsToBePassForSuccess;

                    Logging.Logging.Add(
                        $"CraftingSequenceStep: All ConditionalChecks for ConditionalCheckOnly {success}",
                        LogMessageType.Success
                    );
                }
                else
                {
                    // Execute the method if no prior conditional check or if it's not applicable
                    await currentStep.Method(token);
                    Logging.Logging.Add($"CraftingSequenceStep: Method is {success}", LogMessageType.Info);
                }

                if (currentStep.ConditionalChecks.Count != 0 &&
                    currentStep.CheckType == ConditionalCheckType.ModifyThenCheck)
                {
                    // Count how many conditions are true and check if it meets or exceeds the required count
                    success = currentStep.ConditionalChecks.Count(condition => condition()) >= currentStep.ConditionalsToBePassForSuccess;

                    Logging.Logging.Add(
                        $"CraftingSequenceStep: All ConditionalChecks after method are {success}",
                        LogMessageType.Success
                    );
                }

                if (currentStep.AutomaticSuccess)
                {
                    success = true; // Override success if AutomaticSuccess is true
                    Logging.Logging.Add($"CraftingSequenceStep: AutomaticSuccess is {success}", LogMessageType.Success);
                }

                stopwatch.Stop(); // Stop timing after the step is executed

                Logging.Logging.Add(
                    $"CraftingSequenceStep: Step [{currentStepIndex+1}] completed in {stopwatch.ElapsedMilliseconds} ms",
                    LogMessageType.Profiler
                );
            }
            catch (Exception ex)
            {
                Logging.Logging.Add(
                    $"CraftingSequenceExecutor: Exception caught while executing step {currentStepIndex+1}:\n{ex}",
                    LogMessageType.Error
                );

                Logging.Logging.Add(
                    $"CraftingSequenceStep: Step [{currentStepIndex + 1}] failed after {stopwatch.ElapsedMilliseconds} ms",
                    LogMessageType.Profiler
                );

                return false;
            }

            // Determine the next step based on success or failure
            if (success)
            {
                Logging.Logging.Add("CraftingSequenceStep: True", LogMessageType.Success);

                switch (currentStep.SuccessAction)
                {
                    case SuccessAction.Continue:
                        currentStepIndex++;
                        break;
                    case SuccessAction.End:
                        return true; // End the execution of the sequence
                    case SuccessAction.GoToStep:
                        currentStepIndex = currentStep.SuccessActionStepIndex;
                        break;
                }
            }
            else
            {
                Logging.Logging.Add("CraftingSequenceStep: False", LogMessageType.Error);

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

            Logging.Logging.Add($"CraftingSequenceStep: Next step is [{currentStepIndex+1}]", LogMessageType.Info);
        }

        return true;
    }
}