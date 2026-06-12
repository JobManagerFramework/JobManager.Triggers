using System;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Extended trigger interface for triggers that can predict their next fire time.
/// Implementing this interface allows the background service to use adaptive polling
/// instead of a fixed evaluation interval, reducing unnecessary CPU usage.
/// </summary>
public interface ISchedulableTrigger : ITrigger
{
    /// <summary>
    /// Gets the next predicted fire time in UTC, or <c>null</c> if the trigger
    /// will not fire again (e.g. a completed one-time trigger).
    /// </summary>
    /// <returns>The next fire time as <see cref="DateTimeOffset"/>, or <c>null</c>.</returns>
    DateTimeOffset? GetNextFireTimeUtc();
}
