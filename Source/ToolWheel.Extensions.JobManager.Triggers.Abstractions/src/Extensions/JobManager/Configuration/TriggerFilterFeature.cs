using System;
using System.Collections.Generic;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Job manager feature that stores trigger filter associations per trigger.
/// Attached to an <see cref="IJobDescription"/> via the feature mechanism.
/// Registration of filters at runtime is handled by <see cref="TriggerFeature.Apply"/>.
/// </summary>
public class TriggerFilterFeature : IJobManagerFeature
{
    private readonly Dictionary<string, List<ITriggerFilter>> filtersByTriggerId = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds a filter for the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger to associate the filter with.</param>
    /// <param name="filter">The filter to add.</param>
    public void Add(ITrigger trigger, ITriggerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));

        if (!filtersByTriggerId.TryGetValue(trigger.Id, out var filters))
        {
            filters = new List<ITriggerFilter>();
            filtersByTriggerId[trigger.Id] = filters;
        }

        filters.Add(filter);
    }

    /// <summary>
    /// Gets the filters registered for the specified trigger.
    /// </summary>
    /// <param name="triggerId">The trigger identifier.</param>
    /// <returns>A read-only list of filters, or an empty list if none are registered.</returns>
    public IReadOnlyList<ITriggerFilter> GetFilters(string triggerId)
    {
        if (filtersByTriggerId.TryGetValue(triggerId, out var filters))
        {
            return filters.AsReadOnly();
        }

        return Array.Empty<ITriggerFilter>();
    }

    /// <inheritdoc />
    public void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job)
    {
        // Data-only feature; registration is handled by TriggerFeature.Apply.
    }
}
