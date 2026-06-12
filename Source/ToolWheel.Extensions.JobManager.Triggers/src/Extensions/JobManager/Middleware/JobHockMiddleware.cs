using System;
using System.Threading;
using System.Threading.Tasks;
using ToolWheel.Extensions.JobManager.Middleware;
using ToolWheel.Extensions.JobManager.Services;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Middleware;

/// <summary>
/// Execution middleware that monitors job completion and signals all registered
/// <see cref="JobCompletionHockTrigger"/> instances when a job finishes.
/// </summary>
public class JobHockMiddleware : IExecutionMiddleware
{
    private readonly IJobTriggerService triggerService;

    /// <summary>
    /// Initializes a new instance of <see cref="JobHockMiddleware"/>.
    /// </summary>
    /// <param name="triggerService">The trigger service used to look up registered triggers.</param>
    public JobHockMiddleware(IJobTriggerService triggerService)
    {
        this.triggerService = triggerService ?? throw new ArgumentNullException(nameof(triggerService));
    }

    /// <inheritdoc />
    public async Task InvokeAsync(IJobTaskContextBuilder context, Func<Task> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(next, nameof(next));

        // Execute the rest of the pipeline first.
        await next().ConfigureAwait(false);

        // After completion, determine the final status and signal all matching triggers.
        var completedJobId = context.Job.Id;
        var completedStatus = context.JobTask.Status;

        foreach (var entry in triggerService.GetAll())
        {
            foreach (var trigger in entry.Value)
            {
                if (trigger is JobCompletionHockTrigger completionTrigger)
                {
                    completionTrigger.Signal(completedJobId, completedStatus);
                }
            }
        }
    }
}
