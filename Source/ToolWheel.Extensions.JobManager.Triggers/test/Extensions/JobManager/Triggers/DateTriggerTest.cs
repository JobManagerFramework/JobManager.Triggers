using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager;

namespace ToolWheel.Extensions.JobManager.Triggers;

[TestFixture]
public class DateTriggerTest
{
    private Mock<IJob> CreateJobMock()
    {
        var method = typeof(object).GetMethod(nameof(object.ToString))!;
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
    public async Task DateOnlyConstructor_Fires_At_MidnightLocal()
    {
        var date = new DateOnly(2026, 2, 16);
        var localDateTime = date.ToDateTime(TimeOnly.MinValue);
        var occurrence = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));

        var trigger = new DateTrigger(date);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, occurrence);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DateAndTimeConstructor_Fires_At_SpecifiedLocalTime()
    {
        var date = new DateOnly(2026, 2, 16);
        var time = new TimeOnly(10, 30);
        var localDateTime = date.ToDateTime(time);
        var occurrence = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));

        var trigger = new DateTrigger(date, time);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, occurrence);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DateTimeOffsetConstructor_Uses_ExactOffset()
    {
        var occurrence = new DateTimeOffset(2026, 2, 16, 12, 0, 0, TimeSpan.FromHours(1));
        var trigger = new DateTrigger(occurrence);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, occurrence);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ShouldNotFire_Before_Occurrence()
    {
        var occurrence = new DateTimeOffset(2026, 2, 16, 12, 0, 0, TimeSpan.Zero);
        var trigger = new DateTrigger(occurrence);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, occurrence.AddSeconds(-1));

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task Fires_Only_Once_Until_Reset()
    {
        var occurrence = new DateTimeOffset(2026, 2, 16, 12, 0, 0, TimeSpan.Zero);
        var trigger = new DateTrigger(occurrence);
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, occurrence);

        var first = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(first, Is.True);

        var second = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(second, Is.False);

        trigger.Reset();

        var third = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(third, Is.True);
    }

    [Test]
    public async Task DoesNotFire_When_Disabled()
    {
        var occurrence = new DateTimeOffset(2026, 2, 16, 12, 0, 0, TimeSpan.Zero);
        var trigger = new DateTrigger(occurrence)
        {
            Enabled = false
        };
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, occurrence);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldFireAsync_Throws_When_ContextIsNull()
    {
        var occurrence = new DateTimeOffset(2026, 2, 16, 12, 0, 0, TimeSpan.Zero);
        var trigger = new DateTrigger(occurrence);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await trigger.ShouldFireAsync(null!, CancellationToken.None));
    }

    [Test]
    public void Constructors_Generate_Id_When_NotProvided()
    {
        var t1 = new DateTrigger(new DateOnly(2026, 2, 16));
        var t2 = new DateTrigger(new DateOnly(2026, 2, 16), new TimeOnly(0, 0));
        var t3 = new DateTrigger(new DateTimeOffset(2026, 2, 16, 0, 0, 0, TimeSpan.Zero));

        Assert.That(t1.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(t2.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(t3.Id, Is.Not.Null.And.Not.Empty);
    }
}
