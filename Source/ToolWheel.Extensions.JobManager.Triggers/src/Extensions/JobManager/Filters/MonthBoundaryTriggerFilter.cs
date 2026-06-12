using System;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Blocks trigger execution unless the current date falls within the configured
/// number of tolerance days from the start or end of the month.
/// </summary>
public sealed class MonthBoundaryTriggerFilter : ITriggerFilter
{
    private readonly MonthBoundary _boundary;
    private readonly int _toleranceDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthBoundaryTriggerFilter"/> class.
    /// </summary>
    /// <param name="boundary">Whether to allow execution at the start or end of the month.</param>
    /// <param name="toleranceDays">
    /// Number of days from the boundary that are still considered valid.
    /// For <see cref="MonthBoundary.Start"/>: day 1 through day 1 + toleranceDays.
    /// For <see cref="MonthBoundary.End"/>: last day minus toleranceDays through last day.
    /// Defaults to <c>0</c> (exact boundary day only).
    /// </param>
    public MonthBoundaryTriggerFilter(MonthBoundary boundary, int toleranceDays = 0)
    {
        if (toleranceDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceDays), "Value must be zero or positive.");
        }

        _boundary = boundary;
        _toleranceDays = toleranceDays;
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

        var localDate = context.FiredAt.LocalDateTime;
        var day = localDate.Day;
        var daysInMonth = DateTime.DaysInMonth(localDate.Year, localDate.Month);

        var isInBoundary = _boundary switch
        {
            MonthBoundary.Start => day <= 1 + _toleranceDays,
            MonthBoundary.End => day >= daysInMonth - _toleranceDays,
            _ => false
        };

        if (!isInBoundary)
        {
            var label = _boundary == MonthBoundary.Start ? "start" : "end";
            status = TriggerFilterStatus.Blocked(
                $"Trigger '{context.TriggerId}' blocked: Day {day} is not within {_toleranceDays} day(s) of month {label}.");
        }
    }
}
