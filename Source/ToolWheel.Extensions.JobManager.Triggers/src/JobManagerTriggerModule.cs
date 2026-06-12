using Microsoft.Extensions.DependencyInjection;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Middleware;

namespace ToolWheel;

/// <summary>
/// Configures trigger infrastructure for the job manager.
/// Registers the <see cref="JobHockMiddleware"/> that signals <see cref="Triggers.JobCompletionHockTrigger"/> instances
/// after job execution. Core trigger services and <see cref="Services.TriggerBackgroundService"/>
/// are registered by the core job manager.
/// </summary>
public class JobManagerTriggerModule : IJobManagerModulDescription
{
    public string ModuleName { get => "ToolWheel.Extensions.JobManager.Triggers"; }

    /// <summary>
    /// Configures the job manager with trigger infrastructure.
    /// </summary>
    /// <param name="builder">The configuration builder used to register trigger services. Cannot be null.</param>
    public void ModuleConfiguration(IJobManagerConfigurationBuilder builder)
    {
        // Register the middleware that signals JobCompletionTrigger instances after job execution.
        builder.AddMiddleware<JobHockMiddleware>();
    }
}
