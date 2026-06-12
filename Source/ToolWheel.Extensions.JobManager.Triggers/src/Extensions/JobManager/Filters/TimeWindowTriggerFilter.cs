using System;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Blocks trigger execution outside a configured time-of-day window.
/// </summary>
public sealed class TimeWindowTriggerFilter : ITriggerFilter
{
    private readonly TimeOnly _from;
    private readonly TimeOnly _to;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeWindowTriggerFilter"/> class.
    /// </summary>
    /// <param name="from">Earliest allowed time of day (inclusive).</param>
    /// <param name="to">Latest allowed time of day (exclusive).</param>
    public TimeWindowTriggerFilter(TimeOnly from, TimeOnly to)
    {
        _from = from;
        _to = to;
    }

    /// <inheritdoc />
    public int Order
    {
        get { return 10; }
    }

    /// <inheritdoc />
    public void Evaluate(ITriggerFilterContext context, ref TriggerFilterStatus status)
    {
        var timeOfDay = TimeOnly.FromDateTime(context.FiredAt.LocalDateTime);

        if (!timeOfDay.IsBetween(_from, _to))
        {
            status = TriggerFilterStatus.Blocked(
                $"Trigger '{context.TriggerId}' blocked: {timeOfDay} is outside the allowed window {_from}–{_to}.");
        }
    }
}
