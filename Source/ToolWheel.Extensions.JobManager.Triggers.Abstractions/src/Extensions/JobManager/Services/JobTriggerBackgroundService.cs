using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Services;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Background service that periodically evaluates all registered triggers
/// and executes associated jobs when a trigger fires.
/// Uses adaptive polling when triggers implement <see cref="ISchedulableTrigger"/>.
/// </summary>
public class JobTriggerBackgroundService : BackgroundService
{
    private IJobTriggerService? triggerService;
    private IJobTriggerFilterService? triggerFilterService;
    private IJobService? jobService;
    private ILogger<JobTriggerBackgroundService>? logger;
    private readonly IServiceProvider serviceProvider;

    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan JitterBuffer = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Initializes a new instance of <see cref="JobTriggerBackgroundService"/>.
    /// </summary>
    public JobTriggerBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Starts the background service and resolves required services from the configured <see cref="IServiceProvider"/>.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        triggerService = serviceProvider.GetRequiredService<IJobTriggerService>();
        triggerFilterService = serviceProvider.GetRequiredService<IJobTriggerFilterService>();
        jobService = serviceProvider.GetRequiredService<IJobService>();
        logger = serviceProvider.GetRequiredService<ILogger<JobTriggerBackgroundService>>();

        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures that when the host is shutting down we request cancellation for all running job tasks
    /// and wait for them to complete before the background service stops.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger = serviceProvider.GetRequiredService<ILogger<JobTriggerBackgroundService>>();

        try
        {
            var jobTaskService = serviceProvider.GetService<IJobTaskService>();
            if (jobTaskService != null)
            {
                logger.LogInformation("TriggerBackgroundService initiating graceful shutdown of job tasks.");
                await jobTaskService.CancelAllAndWaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while waiting for job tasks during shutdown.");
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger!.LogInformation("TriggerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan nextDelay;
            try
            {
                nextDelay = await EvaluateTriggersAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger!.LogError(ex, "Unhandled exception during trigger evaluation cycle.");
                nextDelay = DefaultInterval;
            }

            try
            {
                if (nextDelay > TimeSpan.Zero)
                {
                    await Task.Delay(nextDelay, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger!.LogInformation("TriggerBackgroundService stopped.");
    }

    /// <summary>
    /// Evaluates all registered triggers and fires associated jobs where applicable.
    /// Returns the recommended delay until the next evaluation cycle based on
    /// <see cref="ISchedulableTrigger.GetNextFireTimeUtc"/> hints.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the evaluation cycle.</param>
    /// <returns>The recommended <see cref="TimeSpan"/> to wait before the next evaluation.</returns>
    private async Task<TimeSpan> EvaluateTriggersAsync(CancellationToken cancellationToken)
    {
        var signalTime = DateTimeOffset.UtcNow;
        var jobTriggerPairs = triggerService!.GetAll();

        var hasNonSchedulableTriggers = false;
        DateTimeOffset? earliestNextFire = null;

        foreach (var pair in jobTriggerPairs)
        {
            var job = pair.Key;

            if (!job.Enabled)
            {
                continue;
            }

            foreach (var trigger in pair.Value)
            {
                if (!trigger.Enabled)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var context = new TriggerContext(job, signalTime);
                    var shouldFire = await trigger.ShouldFireAsync(context, cancellationToken).ConfigureAwait(false);

                    // Collect scheduling hints for adaptive delay
                    if (trigger is ISchedulableTrigger schedulable)
                    {
                        var nextFire = schedulable.GetNextFireTimeUtc();
                        if (nextFire.HasValue)
                        {
                            if (!earliestNextFire.HasValue || nextFire.Value < earliestNextFire.Value)
                            {
                                earliestNextFire = nextFire.Value;
                            }
                        }
                    }
                    else
                    {
                        hasNonSchedulableTriggers = true;
                    }

                    if (!shouldFire)
                    {
                        continue;
                    }

                    // Filter-Prüfung vor dem Starten der Execution-Pipeline
                    var filters = triggerFilterService!.GetFilters(trigger);

                    if (filters.Count > 0)
                    {
                        var filterContext = new TriggerFilterContext(trigger.Id, job, signalTime);
                        var filterStatus = await EvaluateFiltersAsync(filters, filterContext, cancellationToken).ConfigureAwait(false);

                        if (!filterStatus.ShouldExecute)
                        {
                            logger!.LogDebug(
                                "Trigger '{TriggerId}' for job '{JobId}' blocked by filter: {Reason}",
                                trigger.Id, job.Id, filterStatus.Message);
                            continue;
                        }
                    }

                    logger!.LogInformation("Trigger '{TriggerId}' fired for job '{JobId}'. Executing job.", trigger.Id, job.Id);
                    await jobService!.ExecuteAsync(job, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger!.LogError(ex, "Error evaluating trigger '{TriggerId}' for job '{JobId}'.", trigger.Id, job.Id);
                }
            }
        }

        return CalculateNextDelay(hasNonSchedulableTriggers, earliestNextFire, signalTime);
    }

    /// <summary>
    /// Calculates the optimal delay for the next evaluation cycle.
    /// </summary>
    /// <param name="hasNonSchedulableTriggers">Whether any trigger without scheduling hint exists.</param>
    /// <param name="earliestNextFire">The earliest known next fire time, if any.</param>
    /// <param name="now">The current timestamp used as baseline.</param>
    /// <returns>The recommended delay before the next evaluation cycle.</returns>
    private TimeSpan CalculateNextDelay(bool hasNonSchedulableTriggers, DateTimeOffset? earliestNextFire, DateTimeOffset now)
    {
        // If non-schedulable triggers exist, we must keep polling at the default interval
        if (hasNonSchedulableTriggers)
        {
            if (earliestNextFire.HasValue)
            {
                var untilNext = earliestNextFire.Value - now - JitterBuffer;
                var capped = untilNext > TimeSpan.Zero ? untilNext : TimeSpan.Zero;
                var result = capped < DefaultInterval ? capped : DefaultInterval;
                logger!.LogTrace("Adaptive delay (mixed): {Delay}", result);
                return result;
            }

            return DefaultInterval;
        }

        // All triggers are schedulable
        if (earliestNextFire.HasValue)
        {
            var untilNext = earliestNextFire.Value - now - JitterBuffer;
            var capped = untilNext > TimeSpan.Zero ? untilNext : TimeSpan.Zero;
            var result = capped < MaxInterval ? capped : MaxInterval;
            logger!.LogTrace("Adaptive delay (schedulable only): {Delay}, next fire at {NextFire}", result, earliestNextFire.Value);
            return result;
        }

        // No active triggers at all
        return MaxInterval;
    }

    /// <summary>
    /// Runs the given <paramref name="filters"/> against the provided <paramref name="context"/>.
    /// Returns blocked as soon as any filter denies execution (short-circuit).
    /// </summary>
    /// <param name="filters">The filters to evaluate, scoped to a specific trigger.</param>
    /// <param name="context">The trigger filter context to evaluate.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The aggregated <see cref="TriggerFilterStatus"/>.</returns>
    private async ValueTask<TriggerFilterStatus> EvaluateFiltersAsync(
        IEnumerable<ITriggerFilter> filters,
        ITriggerFilterContext context,
        CancellationToken cancellationToken)
    {
        var status = TriggerFilterStatus.Allow;

        foreach (var filter in filters.OrderBy(f => f.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (filter.IsAsync)
            {
                status = await filter.EvaluateAsync(context, status, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                filter.Evaluate(context, ref status);
            }

            if (!status.ShouldExecute)
            {
                return status;
            }
        }

        return status;
    }
}
