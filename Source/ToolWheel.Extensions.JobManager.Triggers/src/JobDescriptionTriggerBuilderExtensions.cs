using System;
using System.IO;
using ToolWheel.Extensions.JobManager;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel;

/// <summary>
/// Extension methods for <see cref="IJobDescriptionTriggerBuilder"/> providing
/// convenience methods for common trigger types.
/// </summary>
public static class JobDescriptionTriggerBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="JobCompletionHockTrigger"/> that fires when the specified job completes with the given status.
    /// </summary>
    /// <param name="builder">The trigger builder to configure.</param>
    /// <param name="sourceJobId">The identifier of the job whose completion should trigger this job.</param>
    /// <param name="requiredStatus">The completion status that must match for the trigger to fire. Defaults to <see cref="JobTaskStatusEnum.Success"/>.</param>
    /// <returns>The builder for further trigger configuration.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithJobCompletedHock(this IJobDescriptionTriggerBuilder builder, string sourceJobId, JobTaskStatusEnum requiredStatus = JobTaskStatusEnum.Success)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceJobId, nameof(sourceJobId));

        var trigger = new JobCompletionHockTrigger(sourceJobId, requiredStatus);
        return builder.WithTrigger(trigger);
    }

    /// <summary>
    /// Adds an interval-based trigger to the job description builder.
    /// </summary>
    /// <param name="builder">The job description trigger builder to configure.</param>
    /// <param name="interval">The time interval between trigger executions.</param>
    /// <param name="startAt">The optional start time for the trigger.</param>
    /// <param name="fireImmediately">Indicates whether the trigger should fire immediately upon scheduling.</param>
    /// <returns>A builder for further configuring the job description trigger.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithInterval(this IJobDescriptionTriggerBuilder builder, TimeSpan interval, DateTimeOffset? startAt = null, bool fireImmediately = false)
    {
        var trigger = new IntervalTrigger(interval, fireImmediately, startAt);

        return builder.WithTrigger(trigger);
    }

    /// <summary>
    /// Adds a <see cref="FileWatcherTrigger"/> that watches the specified directory.
    /// </summary>
    /// <param name="builder">The trigger builder.</param>
    /// <param name="path">The directory path to watch.</param>
    /// <param name="filter">Optional file filter (e.g. <c>"*.json"</c>). Defaults to <c>"*.*"</c>.</param>
    /// <param name="includeSubdirectories">Whether to watch subdirectories.</param>
    /// <param name="changeTypes">The <see cref="NotifyFilters"/> to observe.</param>
    /// <returns>A filter builder scoped to the new trigger.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithFileWatcherTrigger(
        this IJobDescriptionTriggerBuilder builder,
        string path,
        string filter = "*.*",
        bool includeSubdirectories = false,
        NotifyFilters changeTypes = NotifyFilters.FileName | NotifyFilters.LastWrite)
    {
        var trigger = new FileWatcherTrigger(path, filter, includeSubdirectories, changeTypes);
        return builder.WithTrigger(trigger);
    }

    /// <summary>
    /// Adds a <see cref="FileWatcherTrigger"/> that watches the specified directory.
    /// Can be used when chaining from an existing filter builder.
    /// </summary>
    /// <param name="builder">The filter builder (used when chaining triggers).</param>
    /// <param name="path">The directory path to watch.</param>
    /// <param name="filter">Optional file filter (e.g. <c>"*.json"</c>). Defaults to <c>"*.*"</c>.</param>
    /// <param name="includeSubdirectories">Whether to watch subdirectories.</param>
    /// <param name="changeTypes">The <see cref="NotifyFilters"/> to observe.</param>
    /// <returns>A filter builder scoped to the new trigger.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithFileWatcherTrigger(
        this IJobDescriptionTriggerFilterBuilder builder,
        string path,
        string filter = "*.*",
        bool includeSubdirectories = false,
        NotifyFilters changeTypes = NotifyFilters.FileName | NotifyFilters.LastWrite)
    {
        var trigger = new FileWatcherTrigger(path, filter, includeSubdirectories, changeTypes);
        return builder.WithTrigger(trigger);
    }

    /// <summary>
    /// Adds an <see cref="AppStatusChangedTrigger"/> that fires on the specified status transitions.
    /// </summary>
    /// <param name="builder">The trigger builder.</param>
    /// <param name="targetStatuses">The application statuses that should trigger execution.</param>
    /// <returns>A filter builder scoped to the new trigger.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithAppStatusChangedTrigger(
        this IJobDescriptionTriggerBuilder builder,
        params AppStatus[] targetStatuses)
    {
        var trigger = new AppStatusChangedTrigger(targetStatuses);
        return builder.WithTrigger(trigger);
    }

    /// <summary>
    /// Adds an <see cref="AppStatusChangedTrigger"/> that fires on the specified status transitions.
    /// Can be used when chaining from an existing filter builder.
    /// </summary>
    /// <param name="builder">The filter builder (used when chaining triggers).</param>
    /// <param name="targetStatuses">The application statuses that should trigger execution.</param>
    /// <returns>A filter builder scoped to the new trigger.</returns>
    public static IJobDescriptionTriggerFilterBuilder WithAppStatusChangedTrigger(
        this IJobDescriptionTriggerFilterBuilder builder,
        params AppStatus[] targetStatuses)
    {
        var trigger = new AppStatusChangedTrigger(targetStatuses);
        return builder.WithTrigger(trigger);
    }
}
