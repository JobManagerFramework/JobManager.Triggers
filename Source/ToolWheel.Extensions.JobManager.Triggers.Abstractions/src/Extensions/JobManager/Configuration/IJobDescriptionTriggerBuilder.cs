using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Builder interface used to add triggers to a job description.
/// After adding a trigger, configuration switches to the filter builder
/// to optionally attach filters before continuing with more triggers.
/// </summary>
public interface IJobDescriptionTriggerBuilder
{
    /// <summary>
    /// Adds the given <see cref="ITrigger"/> to the job description
    /// and switches to the filter builder for this trigger.
    /// </summary>
    /// <param name="trigger">Trigger instance to attach.</param>
    /// <returns>A filter builder scoped to the added trigger.</returns>
    IJobDescriptionTriggerFilterBuilder WithTrigger(ITrigger trigger);

    /// <summary>
    /// Completes trigger configuration and returns to the parent job description builder.
    /// </summary>
    IJobDescriptionBuilder Job { get; }
}
