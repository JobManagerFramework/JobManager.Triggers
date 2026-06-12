using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Trigger that fires based on a cron expression.
/// Supports standard five-field expressions and optional six-field expressions (with seconds).
/// Implements <see cref="ISchedulableTrigger"/> to support adaptive polling.
/// </summary>
public sealed class CronTrigger : ISchedulableTrigger
{
    private readonly CronExpression cronExpression;
    private readonly TimeZoneInfo timeZone;
    private readonly object sync = new();
    private DateTimeOffset? lastFired;

    /// <summary>
    /// Creates a new <see cref="CronTrigger"/> with the given cron expression.
    /// </summary>
    /// <param name="expression">
    /// A valid cron expression (5 fields). Use <paramref name="includeSeconds"/> to enable 6-field expressions.
    /// </param>
    /// <param name="timeZone">
    /// Optional time zone for evaluation. Defaults to <see cref="TimeZoneInfo.Utc"/> if not specified.
    /// </param>
    /// <param name="includeSeconds">
    /// If <c>true</c>, the expression is parsed as a 6-field cron expression that includes seconds.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expression"/> is null or whitespace.</exception>
    /// <exception cref="CronFormatException">Thrown when the cron expression is invalid.</exception>
    public CronTrigger(string expression, TimeZoneInfo? timeZone = null, bool includeSeconds = false)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Cron expression must not be null or whitespace.", nameof(expression));
        }

        var format = includeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard;
        cronExpression = CronExpression.Parse(expression, format);

        this.timeZone = timeZone ?? TimeZoneInfo.Utc;

        Id = Guid.NewGuid().ToString("D");
        Enabled = true;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <summary>
    /// The raw cron expression string.
    /// </summary>
    public string Expression
    {
        get { return cronExpression.ToString(); }
    }

    /// <summary>
    /// The time zone used for cron evaluation.
    /// </summary>
    public TimeZoneInfo TimeZone
    {
        get { return timeZone; }
    }

    /// <summary>
    /// For testing/inspection: the next calculated occurrence (UTC), or <c>null</c> if none.
    /// </summary>
    public DateTimeOffset? NextOccurrence
    {
        get
        {
            lock (sync)
            {
                return cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, timeZone);
            }
        }
    }

    /// <inheritdoc />
    public DateTimeOffset? GetNextFireTimeUtc()
    {
        lock (sync)
        {
            return cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, timeZone);
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
            // Bestimme das nächste Cron-Zeitfenster basierend auf der letzten Auslösung
            var fromTime = lastFired ?? now.AddSeconds(-1);
            var nextOccurrence = cronExpression.GetNextOccurrence(fromTime, timeZone);

            if (nextOccurrence.HasValue && now >= nextOccurrence.Value)
            {
                lastFired = now;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
