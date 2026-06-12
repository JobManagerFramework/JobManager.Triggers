using System;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Fluent builder for adding filters to a specific trigger, with the ability
/// to chain additional triggers or return to the parent builder.
/// </summary>
public class JobDescriptionTriggerFilterBuilder : IJobDescriptionTriggerFilterBuilder
{
    private readonly TriggerFeature triggerFeature;
    private readonly TriggerFilterFeature filterFeature;
    private ITrigger currentTrigger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobDescriptionTriggerFilterBuilder"/>.
    /// </summary>
    /// <param name="jobDescriptionBuilder">The parent builder to return to.</param>
    /// <param name="jobDescriptionTriggerBuilder">The trigger builder used to add further triggers.</param>
    /// <param name="triggerFeature">The trigger feature that stores triggers.</param>
    /// <param name="filterFeature">The filter feature that stores per-trigger filters.</param>
    /// <param name="currentTrigger">The trigger currently being configured.</param>
    public JobDescriptionTriggerFilterBuilder(
        IJobDescriptionBuilder jobDescriptionBuilder,
        IJobDescriptionTriggerBuilder jobDescriptionTriggerBuilder,
        TriggerFeature triggerFeature,
        TriggerFilterFeature filterFeature,
        ITrigger currentTrigger)
    {
        ArgumentNullException.ThrowIfNull(jobDescriptionBuilder, nameof(jobDescriptionBuilder));
        ArgumentNullException.ThrowIfNull(jobDescriptionTriggerBuilder, nameof(jobDescriptionTriggerBuilder));
        ArgumentNullException.ThrowIfNull(triggerFeature, nameof(triggerFeature));
        ArgumentNullException.ThrowIfNull(filterFeature, nameof(filterFeature));
        ArgumentNullException.ThrowIfNull(currentTrigger, nameof(currentTrigger));

        Job = jobDescriptionBuilder;
        Trigger = jobDescriptionTriggerBuilder;
        this.triggerFeature = triggerFeature;
        this.filterFeature = filterFeature;
        this.currentTrigger = currentTrigger;
    }

    /// <inheritdoc />
    public IJobDescriptionTriggerFilterBuilder WithFilter(ITriggerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));
        filterFeature.Add(currentTrigger, filter);

        return this;
    }

    /// <inheritdoc />
    public IJobDescriptionTriggerFilterBuilder WithTrigger(ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        triggerFeature.Add(trigger);
        currentTrigger = trigger;

        return this;
    }

    /// <inheritdoc />
    public IJobDescriptionBuilder Job { get; }

    /// <inheritdoc />
    public IJobDescriptionTriggerBuilder Trigger { get; }
}
