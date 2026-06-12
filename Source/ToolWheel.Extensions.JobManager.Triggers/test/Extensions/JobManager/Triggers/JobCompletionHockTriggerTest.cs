using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using ToolWheel.Extensions.JobManager;

namespace ToolWheel.Extensions.JobManager.Triggers;

[TestFixture]
public class JobCompletionHockTriggerTest
{
    [Test]
    public void Constructor_ThrowsOnNullOrWhitespaceSourceJobId()
    {
        Assert.That(() => new JobCompletionHockTrigger(null!, JobTaskStatusEnum.Success), Throws.ArgumentNullException);
        Assert.That(() => new JobCompletionHockTrigger(string.Empty, JobTaskStatusEnum.Success), Throws.ArgumentException);
        Assert.That(() => new JobCompletionHockTrigger("   ", JobTaskStatusEnum.Success), Throws.ArgumentException);
    }

    [Test]
    public void Constructor_InitializesProperties()
    {
        var trigger = new JobCompletionHockTrigger("source-job", JobTaskStatusEnum.Failed);

        Assert.That(trigger.SourceJobId, Is.EqualTo("source-job"));
        Assert.That(trigger.RequiredStatus, Is.EqualTo(JobTaskStatusEnum.Failed));
        Assert.That(trigger.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(trigger.Enabled, Is.True);
    }

    [Test]
    public async Task Signal_EnqueuesOnlyOnMatchingJobIdAndStatus_And_IsConsumedOncePerEvaluation()
    {
        var trigger = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success);

        var jobMock = CreateJobMock("jobA");
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        // Signal matching job and status
        trigger.Signal("jobA", JobTaskStatusEnum.Success);

        var first = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(first, Is.True, "Expected first evaluation to consume the queued signal.");

        var second = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(second, Is.False, "Expected no more signals after consumption.");
    }

    [Test]
    public async Task Signal_DoesNotEnqueue_WhenDisabled()
    {
        var trigger = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success)
        {
            Enabled = false
        };

        var jobMock = CreateJobMock("jobA");
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        trigger.Signal("jobA", JobTaskStatusEnum.Success);

        var fired = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(fired, Is.False, "Disabled trigger must not fire even when signaled.");
    }

    [Test]
    public async Task Signal_DoesNotEnqueue_ForDifferentJobOrStatus()
    {
        var trigger = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success);

        var jobMock = CreateJobMock("jobA");
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        // Different job id
        trigger.Signal("other", JobTaskStatusEnum.Success);
        var fired1 = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(fired1, Is.False);

        // Different status
        trigger.Signal("jobA", JobTaskStatusEnum.Failed);
        var fired2 = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(fired2, Is.False);
    }

    [Test]
    public void ShouldFireAsync_ThrowsOnNullContext()
    {
        var trigger = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success);

        Assert.That(() => trigger.ShouldFireAsync(null!, CancellationToken.None), Throws.ArgumentNullException);
    }

    [Test]
    public async Task ShouldFireAsync_ReturnsFalseWhenCancellationRequested_AndSignalIsPreserved()
    {
        var trigger = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success);

        // Signal the trigger
        trigger.Signal("jobA", JobTaskStatusEnum.Success);

        var jobMock = CreateJobMock("jobA");
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Cancellation requested -> should return false and not consume the signal
        var firedWhenCancelled = await trigger.ShouldFireAsync(ctx, cts.Token);
        Assert.That(firedWhenCancelled, Is.False);

        // With a non-cancelled token the previously queued signal should still be available
        var firedAfter = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(firedAfter, Is.True);
    }

    // Helper: create an IJob mock and initialize only required properties
    private Mock<IJob> CreateJobMock(string id)
    {
        var mock = new Mock<IJob>();
        mock.SetupGet(j => j.Id).Returns(id);
        mock.SetupGet(j => j.TargetMethod).Returns(GetType().GetMethod(nameof(Noop), BindingFlags.Instance | BindingFlags.NonPublic)!);
        mock.SetupGet(j => j.TargetObject).Returns(this);
        mock.SetupGet(j => j.Name).Returns("fake");
        mock.SetupGet(j => j.Description).Returns("fake job");
        mock.SetupGet(j => j.Enabled).Returns(true);
        mock.SetupGet(j => j.JobLogger).Returns((Microsoft.Extensions.Logging.ILogger?)null);
        return mock;
    }

    private void Noop()
    {
    }
}
