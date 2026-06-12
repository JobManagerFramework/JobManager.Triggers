using System;

namespace ToolWheel.Extensions.JobManager.Filters;

/// <summary>
/// Represents the evaluation result of a trigger filter.
/// Contains whether the trigger should proceed and an optional message.
/// </summary>
/// <param name="ShouldExecute">Indicates whether the trigger is allowed to fire.</param>
/// <param name="Message">A human-readable message describing the filter decision.</param>
public sealed record TriggerFilterStatus(bool ShouldExecute, string Message) : IEquatable<TriggerFilterStatus>
{
    /// <summary>
    /// A singleton instance indicating the trigger is allowed to fire.
    /// </summary>
    public static readonly TriggerFilterStatus Allow = new(true, "Trigger execution allowed.");

    /// <summary>
    /// A singleton instance indicating the trigger is blocked.
    /// </summary>
    public static readonly TriggerFilterStatus Block = new(false, "Trigger execution blocked.");

    /// <summary>
    /// Creates a blocking status with a custom message.
    /// </summary>
    /// <param name="reason">The reason the trigger was blocked.</param>
    /// <returns>A new <see cref="TriggerFilterStatus"/> with <see cref="ShouldExecute"/> set to <c>false</c>.</returns>
    public static TriggerFilterStatus Blocked(string reason)
    {
        return new TriggerFilterStatus(false, reason);
    }

    /// <summary>
    /// Determines equality based on <see cref="ShouldExecute"/> only.
    /// </summary>
    public bool Equals(TriggerFilterStatus? other)
    {
        return other is not null && ShouldExecute == other.ShouldExecute;
    }

    /// <summary>
    /// Returns a hash code based on <see cref="ShouldExecute"/>.
    /// </summary>
    public override int GetHashCode()
    {
        return ShouldExecute.GetHashCode();
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        return $"{(ShouldExecute ? "Allow" : "Block")}: {Message}";
    }
}
