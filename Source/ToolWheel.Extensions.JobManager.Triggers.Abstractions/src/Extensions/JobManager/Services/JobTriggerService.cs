using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IJobTriggerService"/>.
/// Delegates persistence to <see cref="IExtensionOptionService"/>.
/// </summary>
public class JobTriggerService : IJobTriggerService
{
    private readonly IExtensionOptionService storage;
    private readonly ILogger<JobTriggerService> logger;
    private readonly ConcurrentDictionary<string, IJob> jobs = new();

    /// <summary>
    /// Initializes a new instance of <see cref="JobTriggerService"/>.
    /// </summary>
    /// <param name="storage">The storage backend for trigger registrations. Must not be <c>null</c>.</param>
    /// <param name="logger">Logger for diagnostic messages. Must not be <c>null</c>.</param>
    public JobTriggerService(IExtensionOptionService storage, ILogger<JobTriggerService> logger)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Register(IJob job, ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));

        jobs.TryAdd(job.Id, job);
        storage.Create(job.Id, trigger.Id, trigger);

        logger.LogInformation("Trigger '{TriggerId}' registered for job '{JobId}'.", trigger.Id, job.Id);
    }

    /// <inheritdoc />
    public bool Remove(IJob job, ITrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        ArgumentNullException.ThrowIfNull(trigger, nameof(trigger));

        var removed = storage.Delete(job.Id, trigger.Id);

        if (removed)
        {
            logger.LogInformation("Trigger '{TriggerId}' removed from job '{JobId}'.", trigger.Id, job.Id);
        }
        else
        {
            logger.LogWarning("Trigger '{TriggerId}' was not found for job '{JobId}'.", trigger.Id, job.Id);
        }

        return removed;
    }

    /// <inheritdoc />
    public IReadOnlyList<ITrigger> GetTriggers(IJob job)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        return storage.ReadAll<ITrigger>(job.Id);
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<IJob, IReadOnlyList<ITrigger>>> GetAll()
    {
        return jobs.Values
            .Select(job => new KeyValuePair<IJob, IReadOnlyList<ITrigger>>(job, storage.ReadAll<ITrigger>(job.Id)))
            .ToArray();
    }
}
