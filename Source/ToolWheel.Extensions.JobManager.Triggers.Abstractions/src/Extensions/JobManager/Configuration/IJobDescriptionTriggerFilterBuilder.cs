using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Builder interface for adding trigger filters to a specific trigger, with the ability
/// to continue adding more triggers or return to the parent job description builder.
/// </summary>
public interface IJobDescriptionTriggerFilterBuilder
{
    /// <summary>
    /// Adds the given <see cref="ITriggerFilter"/> to the current trigger.
    /// </summary>
    /// <param name="filter">The filter instance to attach.</param>
    /// <returns>This builder for further filter configuration.</returns>
    IJobDescriptionTriggerFilterBuilder WithFilter(ITriggerFilter filter);

    /// <summary>
    /// Adds another trigger to the job and switches context to configure its filters.
    /// </summary>
    /// <param name="trigger">The next trigger to add.</param>
    /// <returns>A filter builder scoped to the new trigger.</returns>
    IJobDescriptionTriggerFilterBuilder WithTrigger(ITrigger trigger);

    /// <summary>
    /// Completes trigger and filter configuration and returns to the parent job description builder.
    /// </summary>
    /// <returns>The parent <see cref="IJobDescriptionBuilder"/>.</returns>
    IJobDescriptionBuilder Job { get; }

    /// <summary>
    /// Returns to the trigger builder to add more triggers to the job description.
    /// </summary>
    IJobDescriptionTriggerBuilder Trigger { get; }
}
