using ExileCore.Shared;
using System;
using System.Threading;

namespace WheresMyCraftAt.CraftingSequence
{
    public class CraftingSequenceBase
    {
        public enum SuccessAction
        {
            Continue,
            End,
            GoToStep
        }

        public enum FailureAction
        {
            RepeatStep,
            Restart,
            GoToStep
        }
        public enum ConditionalCheckTiming
        {
            BeforeMethod,
            AfterMethod
        }

        public class CraftingStep
        {
            public Func<CancellationToken, SyncTask<bool>> Method { get; set; }
            public Func<bool> ConditionalCheck { get; set; }
            public ConditionalCheckTiming CheckTiming { get; set; } = ConditionalCheckTiming.BeforeMethod;
            public bool AutomaticSuccess { get; set; } = false;
            public SuccessAction SuccessAction { get; set; }
            public int SuccessActionStepIndex { get; set; }
            public FailureAction FailureAction { get; set; }
            public int FailureActionStepIndex { get; set; }
        }
    }
}