using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Trigger that fires at a fixed interval.
/// Implements <see cref="ISchedulableTrigger"/> to support adaptive polling.
/// </summary>
public sealed class IntervalTrigger : ISchedulableTrigger
{
    private readonly TimeSpan interval;
    private DateTimeOffset nextDue;
    private readonly object sync = new();

    /// <summary>
    /// Creates a new <see cref="IntervalTrigger"/> where the first firing can be scheduled immediately or at a specific start time.
    /// </summary>
    /// <param name="interval">The interval between firings. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="fireImmediately">Optional, if true, the trigger is due immediately on the next evaluation cycle.</param>
    /// <param name="startAt">Optional explicit first scheduled time. If provided it takes precedence over <paramref name="fireImmediately"/>.</param>
    public IntervalTrigger(TimeSpan interval, bool fireImmediately = false, DateTimeOffset? startAt = null)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
        }

        this.interval = interval;

        if (startAt.HasValue)
        {
            nextDue = startAt.Value;
        }
        else if (fireImmediately)
        {
            nextDue = DateTimeOffset.UtcNow;
        }
        else
        {
            nextDue = DateTimeOffset.UtcNow + interval;
        }

        Id = Guid.NewGuid().ToString("D");
        Enabled = true;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <summary>
    /// The configured interval between firings.
    /// </summary>
    public TimeSpan Interval
    {
        get { return interval; }
    }

    /// <summary>
    /// For testing/inspection: next scheduled due time (UTC).
    /// </summary>
    public DateTimeOffset NextDue
    {
        get
        {
            lock (sync)
            {
                return nextDue;
            }
        }
    }

    /// <inheritdoc />
    public DateTimeOffset? GetNextFireTimeUtc()
    {
        lock (sync)
        {
            return nextDue;
        }
    }

    /// <inheritdoc />
    public Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(false);
        }

        if (!Enabled)
        {
            return Task.FromResult(false);
        }

        var now = context.SignalTimestamp;

        lock (sync)
        {
            if (now >= nextDue)
            {
                // schedule next before releasing lock to avoid duplicate firings from concurrent evaluations
                nextDue = now + interval;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
