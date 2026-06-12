using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Services;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Job manager feature that stores trigger definitions for a job description.
/// Attached to an <see cref="IJobDescription"/> via the feature mechanism.
/// </summary>
public class TriggerFeature : IJobManagerFeature
{
    private readonly List<ITrigger> triggers = new();
    private readonly Dictionary<string, List<ITriggerFilter>> triggerFilterMap = new();

    /// <summary>
    /// Gets the triggers configured for the associated job.
    /// </summary>
    public IReadOnlyList<ITrigger> Triggers
    {
        get { return triggers.AsReadOnly(); }
    }

    /// <summary>
    /// Adds a trigger to this feature.
    /// </summary>
    /// <param name="trigger">The trigger to add.</param>
    public void Add(ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        triggers.Add(trigger);
    }

    /// <summary>
    /// Adds a filter for a specific trigger.
    /// </summary>
    /// <param name="trigger">The trigger the filter belongs to.</param>
    /// <param name="filter">The filter to add.</param>
    public void AddFilter(ITrigger trigger, ITriggerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));

        if (!triggerFilterMap.TryGetValue(trigger.Id, out var filters))
        {
            filters = new List<ITriggerFilter>();
            triggerFilterMap[trigger.Id] = filters;
        }

        filters.Add(filter);
    }

    /// <summary>
    /// Returns the filters configured for the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger whose filters to retrieve.</param>
    /// <returns>A read-only list of filters, or an empty list if none are configured.</returns>
    public IReadOnlyList<ITriggerFilter> GetFilters(ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));

        if (triggerFilterMap.TryGetValue(trigger.Id, out var filters))
        {
            return filters.AsReadOnly();
        }

        return Array.Empty<ITriggerFilter>();
    }

    /// <inheritdoc />
    public void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job)
    {
        var triggerService = serviceProvider.GetRequiredService<IJobTriggerService>();
        var filterService = serviceProvider.GetRequiredService<IJobTriggerFilterService>();
        var filterFeature = jobDescription.GetFeature<TriggerFilterFeature>();

        foreach (var trigger in triggers)
        {
            triggerService.Register(job, trigger);

            var filters = filterFeature?.GetFilters(trigger.Id) ?? GetFilters(trigger);

            foreach (var filter in filters)
            {
                filterService.Register(trigger, filter);
            }
        }
    }
}
