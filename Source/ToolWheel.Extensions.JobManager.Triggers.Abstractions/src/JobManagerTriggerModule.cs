using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel;

/// <summary>
/// Registers the trigger infrastructure into the job manager configuration.
/// Automatically discovered and applied by the auto-configuration mechanism when this assembly is referenced.
/// </summary>
public class JobManagerTriggerModule : IJobManagerModulDescription
{
    public string ModuleName { get => "ToolWheel.Extensions.JobManager.Triggers.Abstractions"; }

    /// <inheritdoc/>
    public void ModuleConfiguration(IJobManagerConfigurationBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddHostedService<JobTriggerBackgroundService>();

            services.AddSingleton<IJobTriggerService, JobTriggerService>();
            services.AddSingleton<IJobTriggerFilterService, JobTriggerFilterService>();
        });
    }
}
