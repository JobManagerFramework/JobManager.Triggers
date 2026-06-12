using System.Collections.Generic;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service that manages the association between jobs and their triggers at runtime.
/// </summary>
public interface IJobTriggerService
{
    /// <summary>
    /// Registers a trigger for the specified job.
    /// </summary>
    /// <param name="job">The job to associate the trigger with.</param>
    /// <param name="trigger">The trigger to register.</param>
    void Register(IJob job, ITrigger trigger);

    /// <summary>
    /// Removes a specific trigger from the specified job.
    /// </summary>
    /// <param name="job">The job to remove the trigger from.</param>
    /// <param name="trigger">The trigger to remove.</param>
    /// <returns><c>true</c> if the trigger was removed; otherwise <c>false</c>.</returns>
    bool Remove(IJob job, ITrigger trigger);

    /// <summary>
    /// Returns all triggers registered for the specified job.
    /// </summary>
    /// <param name="job">The job whose triggers to retrieve.</param>
    /// <returns>A read-only list of triggers associated with the job.</returns>
    IReadOnlyList<ITrigger> GetTriggers(IJob job);

    /// <summary>
    /// Returns all job-trigger associations.
    /// </summary>
    /// <returns>An enumerable of key-value pairs mapping jobs to their triggers.</returns>
    IEnumerable<KeyValuePair<IJob, IReadOnlyList<ITrigger>>> GetAll();
}
