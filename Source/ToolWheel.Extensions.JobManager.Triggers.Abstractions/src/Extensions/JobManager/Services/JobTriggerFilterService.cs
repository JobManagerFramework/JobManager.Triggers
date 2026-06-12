using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IJobTriggerFilterService"/>.
/// Delegates persistence to <see cref="IExtensionOptionService"/>.
/// </summary>
public class JobTriggerFilterService : IJobTriggerFilterService
{
    private readonly IExtensionOptionService storage;
    private readonly ILogger<JobTriggerFilterService> logger;
    private readonly ConcurrentDictionary<string, ITrigger> triggers = new();

    /// <summary>
    /// Initializes a new instance of <see cref="JobTriggerFilterService"/>.
    /// </summary>
    /// <param name="storage">The storage backend for filter registrations. Must not be <c>null</c>.</param>
    /// <param name="logger">Logger for diagnostic messages. Must not be <c>null</c>.</param>
    public JobTriggerFilterService(IExtensionOptionService storage, ILogger<JobTriggerFilterService> logger)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Register(ITrigger trigger, ITriggerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));

        triggers.TryAdd(trigger.Id, trigger);
        storage.Create<ITriggerFilter>(trigger.Id, filter.GetType().FullName!, filter);

        logger.LogInformation(
            "Filter '{FilterType}' registered for trigger '{TriggerId}'.",
            filter.GetType().Name,
            trigger.Id);
    }

    /// <inheritdoc />
    public bool Remove(ITrigger trigger, ITriggerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));

        var removed = storage.Delete(trigger.Id, filter.GetType().FullName!);

        if (removed)
        {
            logger.LogInformation(
                "Filter '{FilterType}' removed from trigger '{TriggerId}'.",
                filter.GetType().Name,
                trigger.Id);
        }
        else
        {
            logger.LogWarning(
                "Filter '{FilterType}' was not found for trigger '{TriggerId}'.",
                filter.GetType().Name,
                trigger.Id);
        }

        return removed;
    }

    /// <inheritdoc />
    public void Clear(ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));

        storage.DeleteAll(trigger.Id);

        logger.LogInformation("All filters cleared for trigger '{TriggerId}'.", trigger.Id);
    }

    /// <inheritdoc />
    public IReadOnlyList<ITriggerFilter> GetFilters(ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));

        return storage.ReadAll<ITriggerFilter>(trigger.Id).OrderBy(f => f.Order).ToArray();
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<ITrigger, IReadOnlyList<ITriggerFilter>>> GetAll()
    {
        return triggers.Values
            .Select(trigger => new KeyValuePair<ITrigger, IReadOnlyList<ITriggerFilter>>(
                trigger,
                storage.ReadAll<ITriggerFilter>(trigger.Id).OrderBy(f => f.Order).ToArray()))
            .ToArray();
    }
}
