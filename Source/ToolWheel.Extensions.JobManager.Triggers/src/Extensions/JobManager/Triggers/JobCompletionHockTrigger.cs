using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Represents a trigger that fires when a specified job completes with a required status.
/// </summary>
public sealed class JobCompletionHockTrigger : ITrigger
{
    private readonly ConcurrentQueue<bool> signals = new();

    /// <summary>
    /// Creates a new <see cref="JobCompletionHockTrigger"/>.
    /// </summary>
    /// <param name="sourceJobId">The identifier of the job whose completion should trigger this instance.</param>
    /// <param name="requiredStatus">The status the source job must complete with for the trigger to fire.</param>
    public JobCompletionHockTrigger(string sourceJobId, JobTaskStatusEnum requiredStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceJobId, nameof(sourceJobId));

        SourceJobId = sourceJobId;
        RequiredStatus = requiredStatus;
        Id = Guid.NewGuid().ToString("D");
        Enabled = true;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets the identifier of the source job that this trigger monitors.
    /// </summary>
    public string SourceJobId { get; }

    /// <summary>
    /// Gets the required completion status of the source job for the trigger to fire.
    /// </summary>
    public JobTaskStatusEnum RequiredStatus { get; }

    /// <summary>
    /// Signals the occurrence of a job event when the specified job ID and status match the required criteria.
    /// </summary>
    /// <param name="jobId">The identifier of the job to check.</param>
    /// <param name="status">The status of the job to compare against the required status.</param>
    public void Signal(string jobId, JobTaskStatusEnum status)
    {
        if (!Enabled)
        {
            return;
        }

        if (string.Equals(jobId, SourceJobId, StringComparison.Ordinal) && status == RequiredStatus)
        {
            signals.Enqueue(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (cancellationToken.IsCancellationRequested || !Enabled)
        {
            return Task.FromResult(false);
        }

        // Consume exactly one signal per evaluation cycle.
        var fired = signals.TryDequeue(out _);
        return Task.FromResult(fired);
    }
}
