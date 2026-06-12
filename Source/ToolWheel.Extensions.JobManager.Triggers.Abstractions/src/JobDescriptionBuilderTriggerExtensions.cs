using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel;

/// <summary>
/// Extension methods for <see cref="IJobDescriptionBuilder"/> providing access to trigger configuration.
/// </summary>
public static class JobDescriptionBuilderTriggerExtensions
{
    /// <summary>
    /// Opens the trigger builder to configure triggers and optional filters for this job.
    /// </summary>
    /// <param name="builder">The job description builder to extend.</param>
    /// <returns>A <see cref="IJobDescriptionTriggerBuilder"/> for fluent trigger configuration.</returns>
    public static IJobDescriptionTriggerBuilder Trigger(this IJobDescriptionBuilder builder)
    {
        TriggerFeature? triggerFeature = null;
        TriggerFilterFeature? filterFeature = null;

        builder.WithFeature<TriggerFeature>(f => triggerFeature = f);
        builder.WithFeature<TriggerFilterFeature>(f => filterFeature = f);

        return new JobDescriptionTriggerBuilder(builder, triggerFeature!, filterFeature!);
    }
}
