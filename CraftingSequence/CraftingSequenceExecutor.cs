using ExileCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence
{
    public class CraftingSequenceExecutor(List<CraftingStep> steps)
    {
        private readonly List<CraftingStep> steps = steps;

        public async SyncTask<bool> Execute(CancellationToken token)
        {
            int currentStepIndex = 0;

            while (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                var currentStep = steps[currentStepIndex];
                bool success = false;  // Defaulting success to false

                try
                {
                    Logging.Add($"CraftingSequenceStep: Executing step [{currentStepIndex}]", LogMessageType.Info);

                    if (currentStep.ConditionalChecks.Count != 0 && currentStep.CheckTiming == ConditionalCheckTiming.BeforeMethodRun)
                    {
                        // All conditions must be true for success
                        success = currentStep.ConditionalChecks.All(condition => condition());
                        Logging.Add($"CraftingSequenceStep: All ConditionalChecks before method are {success}", LogMessageType.Success);

                        if (!success)
                        {
                            // If any conditional check before the method is false, execute the method
                            await currentStep.Method(token);
                            Logging.Add($"CraftingSequenceStep: Method is {success}", LogMessageType.Info);
                        }
                    }
                    else
                    {
                        // Execute the method if no prior conditional check or if it's not applicable
                        await currentStep.Method(token);
                        Logging.Add($"CraftingSequenceStep: Method is {success}", LogMessageType.Info);
                    }

                    if (currentStep.ConditionalChecks.Count != 0 && currentStep.CheckTiming == ConditionalCheckTiming.AfterMethodRun)
                    {
                        // Execute the conditional check after the method, if specified
                        success = currentStep.ConditionalChecks.All(condition => condition());
                        Logging.Add($"CraftingSequenceStep: All ConditionalChecks after method are {success}", LogMessageType.Success);
                    }

                    if (currentStep.AutomaticSuccess)
                    {
                        success = true;  // Override success if AutomaticSuccess is true
                        Logging.Add($"CraftingSequenceStep: AutomaticSuccess is {success}", LogMessageType.Success);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Add($"CraftingSequenceExecutor: Exception caught while executing step {currentStepIndex}:\n{ex}", LogMessageType.Error);
                    return false;
                }

                // Determine the next step based on success or failure
                if (success)
                {
                    Logging.Add($"CraftingSequenceStep: True", LogMessageType.Success);
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
                    Logging.Add($"CraftingSequenceStep: False", LogMessageType.Error);
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

                Logging.Add($"CraftingSequenceStep: Next step is [{currentStepIndex}]", LogMessageType.Info);
            }

            return true;
        }
    }
}