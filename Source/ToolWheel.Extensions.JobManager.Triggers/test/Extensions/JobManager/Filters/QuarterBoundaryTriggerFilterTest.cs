using System;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Filters;

[TestFixture]
public class QuarterBoundaryTriggerFilterTest
{
    [Test]
    public void Constructor_Throws_When_NegativeTolerance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QuarterBoundaryTriggerFilter(QuarterBoundary.Start, -1));
    }

    [Test]
    public void Order_Is_10()
    {
        var filter = new QuarterBoundaryTriggerFilter(QuarterBoundary.Start);
        Assert.That(filter.Order, Is.EqualTo(10));
    }

    [Test]
    public void Evaluate_Allows_When_In_StartToleranceRange()
    {
        // Q2 starts in April. tolerance 2 => allowed days: 1..3 of April
        var firedAt = new DateTimeOffset(new DateTime(2026, 4, 2));
        var context = new TriggerFilterContext("q-start-1", null, firedAt);
        var filter = new QuarterBoundaryTriggerFilter(QuarterBoundary.Start, 2);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.True);
        Assert.That(status, Is.EqualTo(TriggerFilterStatus.Allow));
    }

    [Test]
    public void Evaluate_Blocks_When_NotIn_StartToleranceRange()
    {
        // Q2 start is April 1. tolerance 0 => only April 1 allowed
        var firedAt = new DateTimeOffset(new DateTime(2026, 4, 4));
        var context = new TriggerFilterContext("q-start-2", null, firedAt);
        var filter = new QuarterBoundaryTriggerFilter(QuarterBoundary.Start, 0);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.False);
        Assert.That(status.Message, Does.Contain("q-start-2"));
        Assert.That(status.Message, Does.Contain("quarter start"));
    }

    [Test]
    public void Evaluate_Allows_When_In_EndToleranceRange()
    {
        // Q1 ends March 31. tolerance 2 => allowed days: 29..31 of March
        var firedAt = new DateTimeOffset(new DateTime(2026, 3, 30));
        var context = new TriggerFilterContext("q-end-1", null, firedAt);
        var filter = new QuarterBoundaryTriggerFilter(QuarterBoundary.End, 2);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.True);
    }

    [Test]
    public void Evaluate_Blocks_When_NotIn_EndToleranceRange()
    {
        // Q1 ends March 31. tolerance 0 => only March 31 allowed
        var firedAt = new DateTimeOffset(new DateTime(2026, 3, 30));
        var context = new TriggerFilterContext("q-end-2", null, firedAt);
        var filter = new QuarterBoundaryTriggerFilter(QuarterBoundary.End, 0);

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(context, ref status);

        Assert.That(status.ShouldExecute, Is.False);
        Assert.That(status.Message, Does.Contain("q-end-2"));
        Assert.That(status.Message, Does.Contain("quarter end"));
    }

    [Test]
    public void Evaluate_DoesNotChange_When_AlreadyBlocked()
    {
        var firedAt = new DateTimeOffset(new DateTime(2026, 5, 15));
        var context = new TriggerFilterContext("q-blocked", null, firedAt);
        var filter = new QuarterBoundaryTriggerFilter(QuarterBoundary.Start);

        var status = TriggerFilterStatus.Block;
        filter.Evaluate(context, ref status);

        Assert.That(status, Is.EqualTo(TriggerFilterStatus.Block));
        Assert.That(status.ShouldExecute, Is.False);
    }
}
