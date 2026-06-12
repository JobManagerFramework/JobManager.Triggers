using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Triggers;

[TestFixture]
public class AppStatusChangedTriggerTest
{
    private Mock<IJob> CreateJobMock()
    {
        var method = typeof(object).GetMethod(nameof(ToString))!;
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("job-1");
        jobMock.SetupGet(j => j.TargetMethod).Returns(method);
        jobMock.SetupGet(j => j.TargetObject).Returns((object?)null);
        jobMock.SetupGet(j => j.Name).Returns("fake-job");
        jobMock.SetupGet(j => j.Description).Returns("fake job for tests");
        jobMock.SetupGet(j => j.Enabled).Returns(true);
        return jobMock;
    }

    [Test]
    public void Constructor_Throws_When_NoStatusesProvided()
    {
        Assert.Throws<ArgumentException>(() => new AppStatusChangedTrigger(null!));
        Assert.Throws<ArgumentException>(() => new AppStatusChangedTrigger());
    }

    [Test]
    public void Constructor_Sets_Id_And_Enabled_Defaults()
    {
        var t1 = new AppStatusChangedTrigger(AppStatus.Running);
        var t2 = new AppStatusChangedTrigger(AppStatus.Running);

        Assert.That(t1.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(t2.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(t1.Id, Is.Not.EqualTo(t2.Id));
        Assert.That(t1.Enabled, Is.True);
        Assert.That(t1.CurrentStatus, Is.EqualTo(AppStatus.Unknown));
    }

    [Test]
    public async Task Signal_Enqueues_When_TargetStatus_And_ShouldFireAsync_ReturnsTrue()
    {
        var trigger = new AppStatusChangedTrigger(AppStatus.Running);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        trigger.Signal(AppStatus.Running);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.True);
        Assert.That(trigger.LastFiredStatus, Is.EqualTo(AppStatus.Running));
    }

    [Test]
    public async Task Signal_DoesNotEnqueue_When_SameStatus()
    {
        var trigger = new AppStatusChangedTrigger(AppStatus.Unknown);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        // CurrentStatus initially Unknown; signaling Unknown should not enqueue
        trigger.Signal(AppStatus.Unknown);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldFireAsync_Throws_When_ContextIsNull()
    {
        var trigger = new AppStatusChangedTrigger(AppStatus.Running);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await trigger.ShouldFireAsync(null!, CancellationToken.None));
    }

    [Test]
    public async Task ShouldFireAsync_ReturnsFalse_When_Cancelled_Or_Disabled()
    {
        var trigger = new AppStatusChangedTrigger(AppStatus.Running);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        // cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        trigger.Signal(AppStatus.Running);
        var cancelledResult = await trigger.ShouldFireAsync(ctx, cts.Token);
        Assert.That(cancelledResult, Is.False);

        // disabled trigger
        trigger = new AppStatusChangedTrigger(AppStatus.Running);
        trigger.Enabled = false;
        trigger.Signal(AppStatus.Running);
        var disabledResult = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(disabledResult, Is.False);
    }

    [Test]
    public async Task MultipleSignals_AreDequeued_Sequentially()
    {
        var trigger = new AppStatusChangedTrigger(
            AppStatus.Running,
            AppStatus.Stopping);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        trigger.Signal(AppStatus.Running);
        trigger.Signal(AppStatus.Stopping);

        var first = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(first, Is.True);
        Assert.That(trigger.LastFiredStatus, Is.EqualTo(AppStatus.Running));

        var second = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(second, Is.True);
        Assert.That(trigger.LastFiredStatus, Is.EqualTo(AppStatus.Stopping));

        var third = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(third, Is.False);
    }
}
