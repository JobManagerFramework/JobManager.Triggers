using System;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Provides contextual information to a trigger during evaluation.
/// </summary>
/// <param name="Job">The job associated with the trigger being evaluated.</param>
/// <param name="SignalTimestamp">The timestamp of the current evaluation cycle.</param>
public sealed record TriggerContext(IJob Job, DateTimeOffset SignalTimestamp);
