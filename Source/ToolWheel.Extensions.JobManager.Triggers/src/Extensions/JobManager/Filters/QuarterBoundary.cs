namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Determines whether the trigger fires at the beginning or end of a quarter.
/// </summary>
public enum QuarterBoundary
{
    /// <summary>Allow execution at the start of the quarter.</summary>
    Start,

    /// <summary>Allow execution at the end of the quarter.</summary>
    End
}
