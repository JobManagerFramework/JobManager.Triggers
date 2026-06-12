using System;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Default implementation of <see cref="ITriggerFilterContext"/>.
/// </summary>
/// <param name="TriggerId">The unique identifier of the trigger.</param>
/// <param name="Job">The associated job, or <c>null</c> if not available.</param>
/// <param name="FiredAt">The timestamp when the trigger fired.</param>
public sealed record TriggerFilterContext(string TriggerId, IJob? Job, DateTimeOffset FiredAt) : ITriggerFilterContext;
