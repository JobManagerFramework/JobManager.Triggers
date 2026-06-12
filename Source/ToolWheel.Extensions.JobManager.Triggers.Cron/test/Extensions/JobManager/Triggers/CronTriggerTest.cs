using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Triggers;

[TestFixture]
public sealed class CronTriggerTest
{
    // Dummy-Methode, deren MethodInfo an das Mock-IJob übergeben wird
    private static void DummyJobMethod()
    {
    }

    private Mock<IJob> CreateJobMock()
    {
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns(Guid.NewGuid().ToString("D"));
        jobMock
            .SetupGet(j => j.TargetMethod)
            .Returns(typeof(CronTriggerTest).GetMethod(nameof(DummyJobMethod), BindingFlags.NonPublic | BindingFlags.Static)!);
        jobMock.SetupGet(j => j.TargetObject).Returns((object?)null);
        jobMock.SetupGet(j => j.Name).Returns("DummyJob");
        jobMock.SetupGet(j => j.Description).Returns("Test job for CronTrigger");
        jobMock.SetupGet(j => j.Enabled).Returns(true);
        jobMock.SetupGet(j => j.JobLogger).Returns((Microsoft.Extensions.Logging.ILogger?)null);
        return jobMock;
    }

    [Test]
    public void Constructor_Throws_On_NullOrWhitespaceExpression()
    {
        Assert.Throws<ArgumentException>(() => new CronTrigger(null!));
        Assert.Throws<ArgumentException>(() => new CronTrigger(string.Empty));
        Assert.Throws<ArgumentException>(() => new CronTrigger("   "));
    }

    [Test]
    public async Task ShouldFireAsync_Returns_False_When_CancellationRequested()
    {
        var trigger = new CronTrigger("0 * * * *"); // beliebiger gültiger Ausdruck
        var context = new TriggerContext(CreateJobMock().Object, DateTimeOffset.UtcNow);
        var canceledToken = new CancellationToken(true);

        var result = await trigger.ShouldFireAsync(context, canceledToken);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ShouldFireAsync_Returns_False_When_TriggerDisabled()
    {
        var trigger = new CronTrigger("0 * * * *");
        trigger.Enabled = false;
        var context = new TriggerContext(CreateJobMock().Object, DateTimeOffset.UtcNow);

        var result = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ShouldFireAsync_Fires_When_TimeMatches_WithSeconds()
    {
        // Feste, deterministische Zeit vermeiden Flakiness
        var now = new DateTimeOffset(2026, 2, 10, 12, 34, 56, TimeSpan.Zero);
        // Cronos 6-Felder (Seconds Minute Hour Day Month DayOfWeek)
        var expr = $"{now.Second} {now.Minute} {now.Hour} * * *";
        var trigger = new CronTrigger(expr, TimeZoneInfo.Utc, includeSeconds: true);
        var context = new TriggerContext(CreateJobMock().Object, now);

        var result = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ShouldFireAsync_DoesNotFire_When_TimeInFuture()
    {
        var now = new DateTimeOffset(2026, 2, 10, 12, 34, 56, TimeSpan.Zero);
        var future = now.AddMinutes(1);
        var expr = $"{future.Second} {future.Minute} {future.Hour} * * *";
        var trigger = new CronTrigger(expr, TimeZoneInfo.Utc, includeSeconds: true);
        var context = new TriggerContext(CreateJobMock().Object, now);

        var result = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ShouldFireAsync_OnlyFiresOnce_PerOccurrence()
    {
        var now = new DateTimeOffset(2026, 2, 10, 12, 34, 56, TimeSpan.Zero);
        var expr = $"{now.Second} {now.Minute} {now.Hour} * * *";
        var trigger = new CronTrigger(expr, TimeZoneInfo.Utc, includeSeconds: true);
        var context = new TriggerContext(CreateJobMock().Object, now);

        var first = await trigger.ShouldFireAsync(context, CancellationToken.None);
        var second = await trigger.ShouldFireAsync(context, CancellationToken.None);

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
    }
}
