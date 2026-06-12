using System;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Provides contextual information about the trigger event being evaluated by a filter.
/// </summary>
public interface ITriggerFilterContext
{
    /// <summary>
    /// Gets the unique identifier of the trigger that fired.
    /// </summary>
    string TriggerId { get; }

    /// <summary>
    /// Gets the associated job, if available.
    /// </summary>
    IJob? Job { get; }

    /// <summary>
    /// Gets the timestamp when the trigger fired.
    /// </summary>
    DateTimeOffset FiredAt { get; }
}
