using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Represents a filter that is evaluated before a trigger initiates job execution.
/// Implementations inspect the <see cref="ITriggerFilterContext"/> and return a <see cref="TriggerFilterStatus"/>
/// to indicate whether the trigger should proceed or be blocked.
/// </summary>
public interface ITriggerFilter
{
    /// <summary>
    /// Evaluates the filter synchronously.
    /// Default implementation allows execution; override for synchronous filter logic.
    /// </summary>
    /// <param name="context">The trigger context to evaluate.</param>
    /// <param name="status">
    /// The current filter status. Implementations should update this value.
    /// Passed by reference so the caller can observe changes.
    /// </param>
    void Evaluate(ITriggerFilterContext context, ref TriggerFilterStatus status) { }

    /// <summary>
    /// Evaluates the filter asynchronously.
    /// Use this for filters that require I/O operations (e.g., database or network calls).
    /// </summary>
    /// <param name="context">The trigger context to evaluate.</param>
    /// <param name="status">The current filter status to evaluate against.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing the updated <see cref="TriggerFilterStatus"/>.
    /// </returns>
    ValueTask<TriggerFilterStatus> EvaluateAsync(ITriggerFilterContext context, TriggerFilterStatus status, CancellationToken cancellationToken)
        => ValueTask.FromResult(status);

    /// <summary>
    /// Indicates whether this filter requires asynchronous evaluation.
    /// When <c>true</c>, the pipeline will use <see cref="EvaluateAsync"/>; otherwise <see cref="Evaluate"/>.
    /// </summary>
    bool IsAsync => false;

    /// <summary>
    /// Gets the execution order of this filter. Lower values execute first.
    /// Default is <c>0</c>.
    /// </summary>
    int Order => 0;
}
