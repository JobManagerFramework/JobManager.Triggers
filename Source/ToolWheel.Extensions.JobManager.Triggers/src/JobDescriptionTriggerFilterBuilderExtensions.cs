using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Filters;

namespace ToolWheel;

/// <summary>
/// Provides extension methods for configuring job description trigger filters.
/// </summary>
public static class JobDescriptionTriggerFilterBuilderExtensions
{
    /// <summary>
    /// Adds a time window filter to the job description trigger filter builder.
    /// </summary>
    /// <param name="builder">The job description trigger filter builder to configure.</param>
    /// <param name="from">The start time of the time window.</param>
    /// <param name="to">The end time of the time window.</param>
    /// <returns>The updated job description trigger filter builder.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithTimeWindow(this IJobDescriptionTriggerFilterBuilder builder, TimeOnly from, TimeOnly to)
    {
        builder.WithFilter(new TimeWindowTriggerFilter(from, to));

        return builder;
    }

    /// <summary>
    /// Adds a day-of-week filter that blocks execution on days not in the allowed set.
    /// </summary>
    /// <param name="builder">The job description trigger filter builder to configure.</param>
    /// <param name="allowedDays">The days of the week on which execution is allowed.</param>
    /// <returns>The updated job description trigger filter builder.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithAllowedDays(
        this IJobDescriptionTriggerFilterBuilder builder,
        params DayOfWeek[] allowedDays)
    {
        builder.WithFilter(new DayOfWeekTriggerFilter(allowedDays));

        return builder;
    }

    /// <summary>
    /// Adds a filter that only allows execution near the start or end of a month.
    /// </summary>
    /// <param name="builder">The job description trigger filter builder to configure.</param>
    /// <param name="boundary">Whether to allow at month start or month end.</param>
    /// <param name="toleranceDays">Number of tolerance days from the boundary. Defaults to 0.</param>
    /// <returns>The updated job description trigger filter builder.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithMonthBoundary(
        this IJobDescriptionTriggerFilterBuilder builder,
        MonthBoundary boundary,
        int toleranceDays = 0)
    {
        builder.WithFilter(new MonthBoundaryTriggerFilter(boundary, toleranceDays));

        return builder;
    }

    /// <summary>
    /// Adds a filter that only allows execution near the start or end of a calendar quarter.
    /// </summary>
    /// <param name="builder">The job description trigger filter builder to configure.</param>
    /// <param name="boundary">Whether to allow at quarter start or quarter end.</param>
    /// <param name="toleranceDays">Number of tolerance days from the boundary. Defaults to 0.</param>
    /// <returns>The updated job description trigger filter builder.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithQuarterBoundary(
        this IJobDescriptionTriggerFilterBuilder builder,
        QuarterBoundary boundary,
        int toleranceDays = 0)
    {
        builder.WithFilter(new QuarterBoundaryTriggerFilter(boundary, toleranceDays));

        return builder;
    }
}
