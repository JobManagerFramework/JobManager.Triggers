namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Represents the known application lifecycle states.
/// </summary>
public enum AppStatus
{
    /// <summary>The application status is unknown or not yet determined.</summary>
    Unknown = 0,

    /// <summary>The application is starting up.</summary>
    Starting = 1,

    /// <summary>The application is running and fully operational.</summary>
    Running = 2,

    /// <summary>The application is shutting down gracefully.</summary>
    Stopping = 3,

    /// <summary>The application has stopped.</summary>
    Stopped = 4
}
