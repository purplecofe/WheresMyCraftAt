using static WheresMyCraftAt.CraftingSequence.CraftingSequence;

namespace WheresMyCraftAt.Extensions;

public static class EnumExtensions
{
    public static AnyAction ToAnyAction(this FailureAction failureAction)
    {
        return failureAction switch
        {
            FailureAction.Continue => AnyAction.Continue,
            FailureAction.End => AnyAction.End,
            FailureAction.GoToStep => AnyAction.GoToStep,
            FailureAction.RepeatStep => AnyAction.RepeatStep,
            FailureAction.Restart => AnyAction.Restart,
        };
    }

    public static AnyAction ToAnyAction(this SuccessAction failureAction)
    {
        return failureAction switch
        {
            SuccessAction.Continue => AnyAction.Continue,
            SuccessAction.End => AnyAction.End,
            SuccessAction.GoToStep => AnyAction.GoToStep,
            SuccessAction.RepeatStep => AnyAction.RepeatStep,
        };
    }
}