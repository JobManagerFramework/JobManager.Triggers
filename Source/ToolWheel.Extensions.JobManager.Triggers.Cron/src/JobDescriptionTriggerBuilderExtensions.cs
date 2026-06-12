using System;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel;

/// <summary>
/// Cron-specific extension methods for <see cref="JobDescriptionTriggerBuilder"/>.
/// </summary>
public static class JobDescriptionTriggerBuilderExtensions
{
    /// <summary>
    /// Adds a cron-based trigger to the job.
    /// </summary>
    /// <param name="builder">The trigger builder.</param>
    /// <param name="cronExpression">A valid cron expression (5 fields by default).</param>
    /// <param name="timeZone">Optional time zone for evaluation. Defaults to UTC.</param>
    /// <param name="includeSeconds">If <c>true</c>, parses a 6-field cron expression with seconds.</param>
    /// <returns>The trigger builder for further chaining.</returns>
    public static IJobDescriptionTriggerBuilder WithCron(this IJobDescriptionTriggerBuilder builder, string cronExpression, TimeZoneInfo? timeZone = null, bool includeSeconds = false)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var trigger = new CronTrigger(cronExpression, timeZone, includeSeconds);
        builder.WithTrigger(trigger);

        return builder;
    }
}
