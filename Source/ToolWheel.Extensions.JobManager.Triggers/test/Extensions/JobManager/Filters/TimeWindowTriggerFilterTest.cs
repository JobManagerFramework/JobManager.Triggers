using System;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Filters;

[TestFixture]
public class TimeWindowTriggerFilterTest
{
    [Test]
    public void Order_Is_10()
    {
        var filter = new TimeWindowTriggerFilter(new TimeOnly(9, 0), new TimeOnly(17, 0));

        Assert.That(filter.Order, Is.EqualTo(10));
    }

    [Test]
    public void Evaluate_Allows_When_Inside_Window()
    {
        var from = new TimeOnly(9, 0);
        var to = new TimeOnly(17, 0);
        var filter = new TimeWindowTriggerFilter(from, to);

        var firedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Local);
        var ctxMock = new Mock<ITriggerFilterContext>();
        ctxMock.SetupGet(c => c.FiredAt).Returns(new DateTimeOffset(firedAt));
        ctxMock.SetupGet(c => c.TriggerId).Returns("trigger-1");

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(ctxMock.Object, ref status);

        Assert.That(status.ShouldExecute, Is.True);
    }

    [Test]
    public void Evaluate_Blocks_When_Outside_Window()
    {
        var from = new TimeOnly(9, 0);
        var to = new TimeOnly(17, 0);
        var filter = new TimeWindowTriggerFilter(from, to);

        var firedAt = new DateTime(2026, 2, 12, 8, 0, 0, DateTimeKind.Local);
        var ctxMock = new Mock<ITriggerFilterContext>();
        ctxMock.SetupGet(c => c.FiredAt).Returns(new DateTimeOffset(firedAt));
        ctxMock.SetupGet(c => c.TriggerId).Returns("trigger-1");

        var status = TriggerFilterStatus.Allow;
        filter.Evaluate(ctxMock.Object, ref status);

        Assert.That(status.ShouldExecute, Is.False);
        Assert.That(status.Message, Does.Contain("outside the allowed window"));
    }
}
