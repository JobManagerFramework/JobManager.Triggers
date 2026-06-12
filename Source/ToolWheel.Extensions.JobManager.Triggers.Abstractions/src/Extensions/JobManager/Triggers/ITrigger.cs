using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Represents a trigger that determines whether an associated job should be executed.
/// Implementations decide autonomously if the trigger should fire.
/// </summary>
public interface ITrigger
{
    /// <summary>
    /// Gets the unique identifier of the trigger.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the trigger is enabled.
    /// Disabled triggers will not be evaluated.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Evaluates whether the trigger should fire.
    /// </summary>
    /// <param name="context">The context providing information about the current evaluation cycle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the evaluation.</param>
    /// <returns><c>true</c> if the trigger should fire and the associated job should be executed; otherwise <c>false</c>.</returns>
    Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken);
}
