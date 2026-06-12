using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;

namespace ToolWheel.Extensions.JobManager.Triggers;

[TestFixture]
public sealed class IntervalTriggerTest
{
    // Dummy-Methode, deren MethodInfo an das Mock-IJob übergeben wird
    private static void DummyJobMethod()
    {
    }

    private Mock<IJob> CreateJobMock()
    {
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns(Guid.NewGuid().ToString("D"));
        jobMock.SetupGet(j => j.TargetMethod).Returns(typeof(IntervalTriggerTest).GetMethod(nameof(DummyJobMethod), BindingFlags.NonPublic | BindingFlags.Static)!);
        jobMock.SetupGet(j => j.TargetObject).Returns((object?)null);
        jobMock.SetupGet(j => j.Name).Returns("DummyJob");
        jobMock.SetupGet(j => j.Description).Returns("Test job for IntervalTrigger");
        jobMock.SetupGet(j => j.Enabled).Returns(true);
        jobMock.SetupGet(j => j.JobLogger).Returns((Microsoft.Extensions.Logging.ILogger?)null);
        return jobMock;
    }

    [Test]
    public void Constructor_Throws_On_NonPositiveInterval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalTrigger(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalTrigger(TimeSpan.FromMilliseconds(-1)));
    }

    [Test]
    public void Constructor_StartAt_TakesPrecedence_Over_FireImmediately()
    {
        var startAt = new DateTimeOffset(2026, 2, 10, 10, 0, 0, TimeSpan.Zero);
        var trigger = new IntervalTrigger(TimeSpan.FromMinutes(5), fireImmediately: true, startAt: startAt);

        Assert.That(trigger.NextDue, Is.EqualTo(startAt));
    }

    [Test]
    public void Constructor_FireImmediately_SetsNextDue_ToNowApproximate()
    {
        var before = DateTimeOffset.UtcNow;
        var trigger = new IntervalTrigger(TimeSpan.FromMinutes(1), fireImmediately: true);
        var after = DateTimeOffset.UtcNow;

        // NextDue should be between before and after (approximate)
        Assert.That(trigger.NextDue, Is.GreaterThanOrEqualTo(before));
        Assert.That(trigger.NextDue, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public async Task ShouldFireAsync_ReturnsFalse_When_CancellationRequested()
    {
        var startAt = DateTimeOffset.UtcNow;
        var trigger = new IntervalTrigger(TimeSpan.FromMinutes(1), startAt: startAt);
        var jobMock = CreateJobMock();
        var context = new TriggerContext(jobMock.Object, startAt);
        var canceledToken = new CancellationToken(true);

        var result = await trigger.ShouldFireAsync(context, canceledToken);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ShouldFireAsync_ReturnsFalse_When_TriggerDisabled()
    {
        var startAt = DateTimeOffset.UtcNow;
        var trigger = new IntervalTrigger(TimeSpan.FromMinutes(1), startAt: startAt);
        trigger.Enabled = false;
        var jobMock = CreateJobMock();
        var context = new TriggerContext(jobMock.Object, startAt);

        var result = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ShouldFireAsync_Fires_When_Due_And_SchedulesNext()
    {
        var now = new DateTimeOffset(2026, 2, 10, 11, 0, 0, TimeSpan.Zero);
        var interval = TimeSpan.FromMinutes(15);
        var trigger = new IntervalTrigger(interval, startAt: now);
        var jobMock = CreateJobMock();
        var context = new TriggerContext(jobMock.Object, now);

        var fired = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(fired, Is.True);
        Assert.That(trigger.NextDue, Is.EqualTo(now + interval));
    }

    [Test]
    public async Task ShouldFireAsync_DoesNotFire_When_NotDue()
    {
        var now = new DateTimeOffset(2026, 2, 10, 11, 0, 0, TimeSpan.Zero);
        var interval = TimeSpan.FromMinutes(15);
        var trigger = new IntervalTrigger(interval, startAt: now + TimeSpan.FromMinutes(1));
        var jobMock = CreateJobMock();
        var context = new TriggerContext(jobMock.Object, now);

        var fired = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(fired, Is.False);
        Assert.That(trigger.NextDue, Is.EqualTo(now + TimeSpan.FromMinutes(1)));
    }

    [Test]
    public async Task ShouldFireAsync_OnlyFiresOnce_PerOccurrence()
    {
        var now = new DateTimeOffset(2026, 2, 10, 11, 0, 0, TimeSpan.Zero);
        var interval = TimeSpan.FromMinutes(5);
        var trigger = new IntervalTrigger(interval, startAt: now);
        var jobMock = CreateJobMock();
        var context = new TriggerContext(jobMock.Object, now);

        var first = await trigger.ShouldFireAsync(context, CancellationToken.None);
        var second = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
    }
}
