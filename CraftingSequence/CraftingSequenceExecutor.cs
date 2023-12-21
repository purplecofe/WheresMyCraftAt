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
                bool success = false;  // Defaulting success to false

                try
                {
                    if (currentStep.ConditionalCheck != null && currentStep.CheckTiming == ConditionalCheckTiming.BeforeMethod)
                    {
                        if (currentStep.ConditionalCheck())
                        {
                            // If the conditional check before the method is true, skip method execution
                            success = true;
                            Main.DebugPrint($"CraftingSequenceStep: ConditionalCheck is {success}", WheresMyCraftAt.LogMessageType.Success);
                        }
                        else
                        {
                            // If the conditional check before the method is false, execute the method
                            success = await currentStep.Method(token);
                            Main.DebugPrint($"CraftingSequenceStep: Method is {success}", WheresMyCraftAt.LogMessageType.Success);
                        }
                    }
                    else
                    {
                        // Execute the method if no prior conditional check or if it's not applicable
                        success = await currentStep.Method(token);
                        Main.DebugPrint($"CraftingSequenceStep: Method is {success}", WheresMyCraftAt.LogMessageType.Success);
                    }

                    if (currentStep.ConditionalCheck != null && currentStep.CheckTiming == ConditionalCheckTiming.AfterMethod)
                    {
                        // Execute the conditional check after the method, if specified
                        success = success && currentStep.ConditionalCheck();
                        Main.DebugPrint($"CraftingSequenceStep: ConditionalCheck is {success}", WheresMyCraftAt.LogMessageType.Success);
                    }

                    if (currentStep.AutomaticSuccess)
                    {
                        success = true;  // Override success if AutomaticSuccess is true
                        Main.DebugPrint($"CraftingSequenceStep: AutomaticSuccess is {success}", WheresMyCraftAt.LogMessageType.Success);
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
                    Main.DebugPrint($"CraftingSequenceStep: True", WheresMyCraftAt.LogMessageType.Success);
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
                    Main.DebugPrint($"CraftingSequenceStep: False", WheresMyCraftAt.LogMessageType.Error);
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