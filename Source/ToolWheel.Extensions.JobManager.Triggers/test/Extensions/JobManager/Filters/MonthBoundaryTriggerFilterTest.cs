using System;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Filters;

[TestFixture]
public class MonthBoundaryTriggerFilterTest
{
    [Test]
    public void Constructor_Throws_When_NegativeTolerance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthBoundaryTriggerFilter(MonthBoundary.Start, -1));
    }

    [Test]
    public void Order_Is_10()
    {
        var filter = new MonthBoundaryTriggerFilter(MonthBoundary.Start);
        Assert.That(filter.Order, Is.EqualTo(10));
    }

    [Test]
    public void Evaluate_Allows_When_In_StartToleranceRange()
    {
        // tolerance 2 -> allowed days: 1..3
        var firedAt = new DateTimeOffset(new DateTime(2026, 2, 3)); // 3rd Feb 2026
        var context = new TriggerFilterContext("trigger-1", null, firedAt);
        var filter = new MonthBoundaryTriggerFilter(MonthBoundary.Start, 2);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status, Is.EqualTo(TriggerFilterStatus.Allow));
        Assert.That(status.ShouldExecute, Is.True);
    }

    [Test]
    public void Evaluate_Blocks_When_NotIn_StartToleranceRange()
    {
        var firedAt = new DateTimeOffset(new DateTime(2026, 2, 2)); // 2nd Feb 2026
        var context = new TriggerFilterContext("trigger-1", null, firedAt);
        var filter = new MonthBoundaryTriggerFilter(MonthBoundary.Start); // tolerance 0 -> only day 1 allowed

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.False);
        Assert.That(status.Message, Does.Contain("trigger-1"));
        Assert.That(status.Message, Does.Contain("Day 2"));
        Assert.That(status.Message, Does.Contain("start"));
    }

    [Test]
    public void Evaluate_Allows_When_In_EndToleranceRange()
    {
        // January 2026 has 31 days. tolerance 1 -> allowed days: 30..31
        var firedAt = new DateTimeOffset(new DateTime(2026, 1, 30));
        var context = new TriggerFilterContext("trigger-2", null, firedAt);
        var filter = new MonthBoundaryTriggerFilter(MonthBoundary.End, 1);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.True);
    }

    [Test]
    public void Evaluate_Blocks_When_NotIn_EndToleranceRange()
    {
        // January 2026 has 31 days. tolerance 0 -> only day 31 allowed
        var firedAt = new DateTimeOffset(new DateTime(2026, 1, 30));
        var context = new TriggerFilterContext("trigger-2", null, firedAt);
        var filter = new MonthBoundaryTriggerFilter(MonthBoundary.End);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.False);
        Assert.That(status.Message, Does.Contain("trigger-2"));
        Assert.That(status.Message, Does.Contain("Day 30"));
        Assert.That(status.Message, Does.Contain("end"));
    }

    [Test]
    public void Evaluate_DoesNotChange_When_AlreadyBlocked()
    {
        var firedAt = new DateTimeOffset(new DateTime(2026, 2, 15));
        var context = new TriggerFilterContext("trigger-3", null, firedAt);
        var filter = new MonthBoundaryTriggerFilter(MonthBoundary.Start);

        var status = TriggerFilterStatus.Block;
        filter.Evaluate(context, ref status);

        Assert.That(status, Is.EqualTo(TriggerFilterStatus.Block));
        Assert.That(status.ShouldExecute, Is.False);
    }
}
