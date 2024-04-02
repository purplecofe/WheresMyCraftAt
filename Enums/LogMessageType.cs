namespace WheresMyCraftAt.Enums;

public partial class WheresMyCraftAt
{
    public enum LogMessageType
    {
        Trace, // Extremely detailed information, for diagnosing specific issues. Might include extensive data output.
        Debug, // Detailed information on the flow through the system. Useful during development and debugging.
        Info, // Informational messages, indicating that things are working as expected.
        Warning, // Indications of possible issues or unexpected situations that are not critical.
        Error, // Error events of considerable importance that will prevent normal program execution, but might not require immediate attention.
        Critical, // Severe error events that might cause the application to terminate.
        Profiler, // Specific for performance profiling logs.
        Evaluation, // Specific for EvaluateConditions method in the crafting sequence executor.
        Special, // For messages that don't fit into other categories, can be used as per specific needs.
        ItemData, // For full ItemFilterLibrary logs.
        EndSessionStats // For end session logging of stats.
    }
}