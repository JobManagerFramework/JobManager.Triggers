using System;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Filters;

[TestFixture]
public class DayOfWeekTriggerFilterTest
{
    [Test]
    public void Constructor_Throws_When_NoDaysProvided()
    {
        // null array
        Assert.Throws<ArgumentException>(() => new DayOfWeekTriggerFilter(null!));

        // empty array via params (no arguments)
        Assert.Throws<ArgumentException>(() => new DayOfWeekTriggerFilter());
    }

    [Test]
    public void Order_Is_10()
    {
        var filter = new DayOfWeekTriggerFilter(DayOfWeek.Monday);
        Assert.That(filter.Order, Is.EqualTo(10));
    }

    [Test]
    public void Evaluate_Allows_When_DayIsIncluded()
    {
        var firedAt = new DateTimeOffset(new DateTime(2026, 2, 16)); // 16.02.2026 (Monday)
        var context = new TriggerFilterContext("trigger-1", null, firedAt);
        var filter = new DayOfWeekTriggerFilter(DayOfWeek.Monday, DayOfWeek.Tuesday);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status, Is.EqualTo(TriggerFilterStatus.Allow));
        Assert.That(status.ShouldExecute, Is.True);
    }

    [Test]
    public void Evaluate_Blocks_When_DayIsNotIncluded()
    {
        var firedAt = new DateTimeOffset(new DateTime(2026, 2, 16)); // Monday
        var context = new TriggerFilterContext("trigger-1", null, firedAt);
        var filter = new DayOfWeekTriggerFilter(DayOfWeek.Tuesday);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.False);
        Assert.That(status.Message, Does.Contain("trigger-1"));
        Assert.That(status.Message, Does.Contain("Monday"));
        Assert.That(status.Message, Does.Contain("is not in the allowed days"));
    }

    [Test]
    public void Evaluate_DoesNotChange_When_AlreadyBlocked()
    {
        var firedAt = new DateTimeOffset(new DateTime(2026, 2, 16)); // Monday
        var context = new TriggerFilterContext("trigger-1", null, firedAt);
        var filter = new DayOfWeekTriggerFilter(DayOfWeek.Tuesday);

        var status = TriggerFilterStatus.Block;
        filter.Evaluate(context, ref status);

        Assert.That(status, Is.EqualTo(TriggerFilterStatus.Block));
        Assert.That(status.ShouldExecute, Is.False);
    }
}
