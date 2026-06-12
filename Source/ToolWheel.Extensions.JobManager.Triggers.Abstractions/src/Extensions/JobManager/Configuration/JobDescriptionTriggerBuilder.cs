using System;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Fluent builder for adding triggers to a job description.
/// After adding a trigger, delegates to <see cref="IJobDescriptionTriggerFilterBuilder"/>
/// for optional filter configuration.
/// </summary>
public class JobDescriptionTriggerBuilder : IJobDescriptionTriggerBuilder
{
    private readonly TriggerFeature triggerFeature;
    private readonly TriggerFilterFeature filterFeature;

    /// <summary>
    /// Initializes a new instance of <see cref="JobDescriptionTriggerBuilder"/>.
    /// </summary>
    /// <param name="jobDescriptionBuilder">The parent builder to return to after trigger configuration.</param>
    /// <param name="triggerFeature">The trigger feature to populate.</param>
    /// <param name="filterFeature">The filter feature for per-trigger filter storage.</param>
    public JobDescriptionTriggerBuilder(
        IJobDescriptionBuilder jobDescriptionBuilder,
        TriggerFeature triggerFeature,
        TriggerFilterFeature filterFeature)
    {
        ArgumentNullException.ThrowIfNull(jobDescriptionBuilder, nameof(jobDescriptionBuilder));
        ArgumentNullException.ThrowIfNull(triggerFeature, nameof(triggerFeature));
        ArgumentNullException.ThrowIfNull(filterFeature, nameof(filterFeature));

        Job = jobDescriptionBuilder;
        this.triggerFeature = triggerFeature;
        this.filterFeature = filterFeature;
    }

    /// <inheritdoc />
    public IJobDescriptionTriggerFilterBuilder WithTrigger(ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        triggerFeature.Add(trigger);

        return new JobDescriptionTriggerFilterBuilder(Job, this, triggerFeature, filterFeature, trigger);
    }

    /// <inheritdoc />
    public IJobDescriptionBuilder Job { get; }
}
