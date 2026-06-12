using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Blocks trigger execution on days not included in the configured set of allowed weekdays.
/// </summary>
public sealed class DayOfWeekTriggerFilter : ITriggerFilter
{
    private readonly HashSet<DayOfWeek> _allowedDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfWeekTriggerFilter"/> class.
    /// </summary>
    /// <param name="allowedDays">The days of the week on which execution is allowed. Must contain at least one day.</param>
    public DayOfWeekTriggerFilter(params DayOfWeek[] allowedDays)
    {
        if (allowedDays is null || allowedDays.Length == 0)
        {
            throw new ArgumentException("At least one allowed day must be specified.", nameof(allowedDays));
        }

        _allowedDays = new HashSet<DayOfWeek>(allowedDays);
    }

    /// <inheritdoc />
    public int Order
    {
        get { return 10; }
    }

    /// <inheritdoc />
    public void Evaluate(ITriggerFilterContext context, ref TriggerFilterStatus status)
    {
        if (!status.ShouldExecute)
        {
            return;
        }

        var dayOfWeek = context.FiredAt.LocalDateTime.DayOfWeek;

        if (!_allowedDays.Contains(dayOfWeek))
        {
            status = TriggerFilterStatus.Blocked(
                $"Trigger '{context.TriggerId}' blocked: {dayOfWeek} is not in the allowed days.");
        }
    }
}
