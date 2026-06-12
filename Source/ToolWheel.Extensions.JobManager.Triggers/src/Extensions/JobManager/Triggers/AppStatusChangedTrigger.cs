using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Trigger that fires when the application status transitions to one of the configured target states.
/// Status changes are signalled externally via <see cref="Signal(AppStatus)"/>.
/// </summary>
public sealed class AppStatusChangedTrigger : ITrigger
{
    private readonly HashSet<AppStatus> _targetStatuses;
    private readonly ConcurrentQueue<AppStatus> _pendingTransitions = new();
    private AppStatus _currentStatus = AppStatus.Unknown;
    private readonly object _sync = new();

    /// <summary>
    /// Creates a new <see cref="AppStatusChangedTrigger"/> that fires for the specified target statuses.
    /// </summary>
    /// <param name="targetStatuses">
    /// One or more <see cref="AppStatus"/> values that should cause the trigger to fire.
    /// Must contain at least one value.
    /// </param>
    public AppStatusChangedTrigger(params AppStatus[] targetStatuses)
    {
        if (targetStatuses is null || targetStatuses.Length == 0)
        {
            throw new ArgumentException("At least one target status must be specified.", nameof(targetStatuses));
        }

        _targetStatuses = new HashSet<AppStatus>(targetStatuses);
        Id = Guid.NewGuid().ToString("D");
        Enabled = true;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets the current application status known to this trigger.
    /// </summary>
    public AppStatus CurrentStatus
    {
        get
        {
            lock (_sync)
            {
                return _currentStatus;
            }
        }
    }

    /// <summary>
    /// Gets the last status transition that caused the trigger to fire,
    /// or <c>null</c> if the trigger has not yet fired.
    /// </summary>
    public AppStatus? LastFiredStatus { get; private set; }

    /// <summary>
    /// Signals a status change to the trigger. If the new status matches one
    /// of the configured target statuses, the trigger will fire on the next evaluation.
    /// </summary>
    /// <param name="newStatus">The new application status.</param>
    public void Signal(AppStatus newStatus)
    {
        lock (_sync)
        {
            if (_currentStatus == newStatus)
            {
                return;
            }

            _currentStatus = newStatus;
        }

        if (_targetStatuses.Contains(newStatus))
        {
            _pendingTransitions.Enqueue(newStatus);
        }
    }

    /// <inheritdoc />
    public Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (cancellationToken.IsCancellationRequested || !Enabled)
        {
            return Task.FromResult(false);
        }

        if (_pendingTransitions.TryDequeue(out var status))
        {
            LastFiredStatus = status;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
