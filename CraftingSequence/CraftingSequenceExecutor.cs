using ExileCore;
using ExileCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using static WheresMyCraftAt.CraftingSequence.CraftingSequenceBase;

namespace WheresMyCraftAt.CraftingSequence
{
    public class CraftingSequenceExecutor
    {
        private List<CraftingStep> steps;
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public CraftingSequenceExecutor(List<CraftingStep> steps)
        {
            this.steps = steps;
        }

        public async SyncTask<bool> Execute(CancellationToken token)
        {
            int currentStepIndex = 0;

            while (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                var currentStep = steps[currentStepIndex];
                bool success = true;

                try
                {
                    // Execute the conditional check before the method, if specified and applicable
                    if (currentStep.ConditionalCheck != null && currentStep.CheckTiming == ConditionalCheckTiming.BeforeMethod)
                    {
                        if (!currentStep.ConditionalCheck())
                        {
                            success = await currentStep.Method(token);
                        }
                    }
                    else
                    {
                        // Directly execute the method if no prior conditional check or if it's not applicable
                        success = await currentStep.Method(token);
                    }

                    // Override success if AutomaticSuccess is true
                    if (currentStep.AutomaticSuccess)
                    {
                        success = true;
                    }

                    // Execute the conditional check after the method, if specified
                    if (currentStep.ConditionalCheck != null && currentStep.CheckTiming == ConditionalCheckTiming.AfterMethod)
                    {
                        success = success && currentStep.ConditionalCheck();
                    }
                }
                catch (Exception ex)
                {
                    Main.DebugPrint($"CraftingSequenceExecutor: Exception caught while executing step {currentStepIndex}:\n{ex}", WheresMyCraftAt.LogMessageType.Error);
                    return false;
                }

                // Determine the next step based on success or failure
                if (success)
                {
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
            }

            return true;
        }
    }
}