using ExileCore.Shared;
using System;
using System.Collections.Generic;
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
            BeforeMethodRun,
            AfterMethodRun
        }

        public class CraftingStep
        {
            public Func<CancellationToken, SyncTask<bool>> Method { get; set; }
            public List<Func<bool>> ConditionalChecks { get; set; } = [];
            public ConditionalCheckTiming CheckTiming { get; set; } = ConditionalCheckTiming.AfterMethodRun;
            public bool AutomaticSuccess { get; set; } = false;
            public SuccessAction SuccessAction { get; set; }
            public int SuccessActionStepIndex { get; set; }
            public FailureAction FailureAction { get; set; }
            public int FailureActionStepIndex { get; set; }
        }

        public class CraftingStepInput
        {
            public string CurrencyItem { get; set; } = string.Empty;
            public bool AutomaticSuccess { get; set; } = false;

            //public ItemRarity ItemRarityWanted { get; set; } = ItemRarity.Normal;
            public SuccessAction SuccessAction { get; set; } = SuccessAction.Continue;

            public int SuccessActionStepIndex { get; set; } = 1;
            public FailureAction FailureAction { get; set; } = FailureAction.Restart;
            public int FailureActionStepIndex { get; set; } = 1;
            public List<string> ConditionalCheckKeys { get; set; } = [];
            public ConditionalCheckTiming CheckTiming { get; set; } = ConditionalCheckTiming.AfterMethodRun;
        }
    }
}