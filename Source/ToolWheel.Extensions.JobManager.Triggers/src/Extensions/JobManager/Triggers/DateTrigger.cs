using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// A trigger that fires once at a specific date (optionally with time).
/// </summary>
public sealed class DateTrigger : ITrigger
{
    private readonly DateTimeOffset _occurrence;
    private int _fired;

    /// <summary>
    /// Creates a date-only trigger that fires at the specified date at midnight (local time).
    /// </summary>
    /// <param name="date">The date on which the trigger should fire.</param>
    /// <param name="id">Optional identifier for the trigger. If null or empty a new GUID is used.</param>
    public DateTrigger(DateOnly date, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("D");
        }

        Id = id;
        var localDateTime = date.ToDateTime(TimeOnly.MinValue);
        _occurrence = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
        Enabled = true;
        Interlocked.Exchange(ref _fired, 0);
    }

    /// <summary>
    /// Creates a date+time trigger that fires at the specified local time on the given date.
    /// </summary>
    /// <param name="date">The date on which the trigger should fire.</param>
    /// <param name="time">The time of day when the trigger should fire.</param>
    /// <param name="id">Optional identifier for the trigger. If null or empty a new GUID is used.</param>
    public DateTrigger(DateOnly date, TimeOnly time, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("D");
        }

        Id = id;
        var localDateTime = date.ToDateTime(time);
        _occurrence = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
        Enabled = true;
        Interlocked.Exchange(ref _fired, 0);
    }

    /// <summary>
    /// Creates a trigger for the exact <see cref="DateTimeOffset"/> occurrence.
    /// </summary>
    /// <param name="dateTimeOffset">Exact date/time with offset when the trigger should fire.</param>
    /// <param name="id">Optional identifier for the trigger. If null or empty a new GUID is used.</param>
    public DateTrigger(DateTimeOffset dateTimeOffset, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("D");
        }

        Id = id;
        _occurrence = dateTimeOffset;
        Enabled = true;
        Interlocked.Exchange(ref _fired, 0);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <summary>
    /// Resets the trigger so it can fire again for the same configured occurrence.
    /// Thread-safe.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _fired, 0);
    }

    /// <inheritdoc />
    public Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!Enabled)
        {
            return Task.FromResult(false);
        }

        // If already fired, do not fire again.
        if (Interlocked.CompareExchange(ref _fired, 0, 0) == 1)
        {
            return Task.FromResult(false);
        }

        // Fire once when the signal timestamp reaches or passes the configured occurrence.
        if (context.SignalTimestamp >= _occurrence)
        {
            Interlocked.Exchange(ref _fired, 1);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
