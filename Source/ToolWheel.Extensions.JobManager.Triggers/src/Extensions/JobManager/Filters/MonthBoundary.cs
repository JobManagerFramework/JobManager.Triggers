namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Determines whether the trigger fires at the beginning or end of a month.
/// </summary>
public enum MonthBoundary
{
    /// <summary>Allow execution at the start of the month.</summary>
    Start,

    /// <summary>Allow execution at the end of the month.</summary>
    End
}
