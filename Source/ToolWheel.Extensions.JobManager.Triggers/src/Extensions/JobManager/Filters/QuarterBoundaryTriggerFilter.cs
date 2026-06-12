using System;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Blocks trigger execution unless the current date falls within the configured
/// number of tolerance days from the start or end of the calendar quarter.
/// Quarters: Q1 = Jan–Mar, Q2 = Apr–Jun, Q3 = Jul–Sep, Q4 = Oct–Dec.
/// </summary>
public sealed class QuarterBoundaryTriggerFilter : ITriggerFilter
{
    private static readonly int[] QuarterStartMonths = [1, 4, 7, 10];

    private readonly QuarterBoundary _boundary;
    private readonly int _toleranceDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuarterBoundaryTriggerFilter"/> class.
    /// </summary>
    /// <param name="boundary">Whether to allow execution at the start or end of the quarter.</param>
    /// <param name="toleranceDays">
    /// Number of days from the boundary that are still considered valid. Defaults to <c>0</c>.
    /// </param>
    public QuarterBoundaryTriggerFilter(QuarterBoundary boundary, int toleranceDays = 0)
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
        var quarterStartMonth = GetQuarterStartMonth(localDate.Month);

        var isInBoundary = _boundary switch
        {
            QuarterBoundary.Start => IsNearQuarterStart(localDate, quarterStartMonth),
            QuarterBoundary.End => IsNearQuarterEnd(localDate, quarterStartMonth),
            _ => false
        };

        if (!isInBoundary)
        {
            var label = _boundary == QuarterBoundary.Start ? "start" : "end";
            status = TriggerFilterStatus.Blocked(
                $"Trigger '{context.TriggerId}' blocked: {localDate:yyyy-MM-dd} is not within {_toleranceDays} day(s) of quarter {label}.");
        }
    }

    private static int GetQuarterStartMonth(int month)
    {
        // Q1=1, Q2=4, Q3=7, Q4=10
        return QuarterStartMonths[(month - 1) / 3];
    }

    private bool IsNearQuarterStart(DateTime date, int quarterStartMonth)
    {
        var quarterStart = new DateTime(date.Year, quarterStartMonth, 1);
        var diff = (date.Date - quarterStart).TotalDays;

        return diff >= 0 && diff <= _toleranceDays;
    }

    private bool IsNearQuarterEnd(DateTime date, int quarterStartMonth)
    {
        // Quartalsende = letzter Tag des dritten Monats im Quartal
        var quarterEndMonth = quarterStartMonth + 2;
        var lastDay = DateTime.DaysInMonth(date.Year, quarterEndMonth);
        var quarterEnd = new DateTime(date.Year, quarterEndMonth, lastDay);
        var diff = (quarterEnd - date.Date).TotalDays;

        return diff >= 0 && diff <= _toleranceDays;
    }
}
