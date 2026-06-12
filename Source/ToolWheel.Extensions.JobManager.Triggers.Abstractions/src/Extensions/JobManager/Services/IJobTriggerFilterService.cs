using System.Collections.Generic;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service that manages the association between triggers and their filters at runtime.
/// Allows reading, adding and removing filters outside the builder pattern.
/// </summary>
public interface IJobTriggerFilterService
{
    /// <summary>
    /// Registers a filter for the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger to associate the filter with.</param>
    /// <param name="filter">The filter to register.</param>
    void Register(ITrigger trigger, ITriggerFilter filter);

    /// <summary>
    /// Removes a specific filter from the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger to remove the filter from.</param>
    /// <param name="filter">The filter to remove.</param>
    /// <returns><c>true</c> if the filter was removed; otherwise <c>false</c>.</returns>
    bool Remove(ITrigger trigger, ITriggerFilter filter);

    /// <summary>
    /// Removes all filters from the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger whose filters should be cleared.</param>
    void Clear(ITrigger trigger);

    /// <summary>
    /// Returns all filters registered for the specified trigger, ordered by <see cref="ITriggerFilter.Order"/>.
    /// </summary>
    /// <param name="trigger">The trigger whose filters to retrieve.</param>
    /// <returns>A read-only list of filters associated with the trigger.</returns>
    IReadOnlyList<ITriggerFilter> GetFilters(ITrigger trigger);

    /// <summary>
    /// Returns all trigger-filter associations.
    /// </summary>
    /// <returns>An enumerable of key-value pairs mapping triggers to their filters.</returns>
    IEnumerable<KeyValuePair<ITrigger, IReadOnlyList<ITriggerFilter>>> GetAll();
}
