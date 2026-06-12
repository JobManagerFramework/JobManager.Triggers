using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Middleware;
using ToolWheel.Extensions.JobManager.Services;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Middleware;

[TestFixture]
public class JobHockMiddlewareTest
{
    [Test]
    public void Constructor_ThrowsOnNullTriggerService()
    {
        Assert.That(() => new JobHockMiddleware(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void InvokeAsync_ThrowsOnNullContext()
    {
        var svcMock = new Mock<IJobTriggerService>(MockBehavior.Strict);
        var middleware = new JobHockMiddleware(svcMock.Object);

        Assert.That(async () => await middleware.InvokeAsync(null!, () => Task.CompletedTask, CancellationToken.None), Throws.ArgumentNullException);
    }

    [Test]
    public void InvokeAsync_ThrowsOnNullNext()
    {
        var svcMock = new Mock<IJobTriggerService>(MockBehavior.Strict);
        var middleware = new JobHockMiddleware(svcMock.Object);

        var jobMock = new Mock<IJob>(MockBehavior.Strict);
        jobMock.SetupGet(j => j.Id).Returns("jobA");

        var jobTaskMock = new Mock<IJobTask>(MockBehavior.Strict);
        jobTaskMock.SetupGet(t => t.Status).Returns(JobTaskStatusEnum.Success);

        var contextMock = new Mock<IJobTaskContextBuilder>(MockBehavior.Strict);
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        Assert.That(async () => await middleware.InvokeAsync(contextMock.Object, null!, CancellationToken.None), Throws.ArgumentNullException);
    }

    [Test]
    public async Task InvokeAsync_InvokesNextAndSignalsMatchingCompletionTrigger()
    {
        // Arrange
        var svcMock = new Mock<IJobTriggerService>(MockBehavior.Strict);

        var jobMock = new Mock<IJob>(MockBehavior.Strict);
        jobMock.SetupGet(j => j.Id).Returns("jobA");

        var jobTaskMock = new Mock<IJobTask>(MockBehavior.Strict);
        jobTaskMock.SetupGet(t => t.Status).Returns(JobTaskStatusEnum.Success);

        var contextMock = new Mock<IJobTaskContextBuilder>(MockBehavior.Strict);
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        var completionTrigger = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success);

        var entries = new[]
        {
            new KeyValuePair<IJob, IReadOnlyList<ITrigger>>(jobMock.Object, (IReadOnlyList<ITrigger>)new ITrigger[] { completionTrigger })
        };

        svcMock.Setup(s => s.GetAll()).Returns(entries);

        var middleware = new JobHockMiddleware(svcMock.Object);

        var nextCalled = false;
        Task Next()
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await middleware.InvokeAsync(contextMock.Object, Next, CancellationToken.None);

        // Assert
        Assert.That(nextCalled, Is.True, "Middleware must invoke next.");

        var fired = await completionTrigger.ShouldFireAsync(new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow), CancellationToken.None);
        Assert.That(fired, Is.True, "Matching completion trigger should be signaled.");
    }

    [Test]
    public async Task InvokeAsync_DoesNotSignalForNonMatchingJobOrStatus()
    {
        // Arrange
        var svcMock = new Mock<IJobTriggerService>(MockBehavior.Strict);

        var jobMock = new Mock<IJob>(MockBehavior.Strict);
        jobMock.SetupGet(j => j.Id).Returns("jobA");

        var jobTaskMock = new Mock<IJobTask>(MockBehavior.Strict);
        jobTaskMock.SetupGet(t => t.Status).Returns(JobTaskStatusEnum.Success);

        var contextMock = new Mock<IJobTaskContextBuilder>(MockBehavior.Strict);
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        // Trigger watches a different job id
        var triggerDifferentJob = new JobCompletionHockTrigger("other", JobTaskStatusEnum.Success);

        // Trigger watches same job but different status
        var triggerDifferentStatus = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Failed);

        var entries = new[]
        {
            new KeyValuePair<IJob, IReadOnlyList<ITrigger>>(jobMock.Object, (IReadOnlyList<ITrigger>)new ITrigger[] { triggerDifferentJob, triggerDifferentStatus })
        };

        svcMock.Setup(s => s.GetAll()).Returns(entries);

        var middleware = new JobHockMiddleware(svcMock.Object);

        // Act
        await middleware.InvokeAsync(contextMock.Object, () => Task.CompletedTask, CancellationToken.None);

        // Assert
        var fired1 = await triggerDifferentJob.ShouldFireAsync(new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow), CancellationToken.None);
        var fired2 = await triggerDifferentStatus.ShouldFireAsync(new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.That(fired1, Is.False);
        Assert.That(fired2, Is.False);
    }

    [Test]
    public async Task InvokeAsync_IgnoresNonJobCompletionTriggerInstances()
    {
        // Arrange
        var svcMock = new Mock<IJobTriggerService>(MockBehavior.Strict);

        var jobMock = new Mock<IJob>(MockBehavior.Strict);
        jobMock.SetupGet(j => j.Id).Returns("jobA");

        var jobTaskMock = new Mock<IJobTask>(MockBehavior.Strict);
        jobTaskMock.SetupGet(t => t.Status).Returns(JobTaskStatusEnum.Success);

        var contextMock = new Mock<IJobTaskContextBuilder>(MockBehavior.Strict);
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        var nonCompletionMock = new Mock<ITrigger>(MockBehavior.Strict);
        nonCompletionMock.SetupGet(t => t.Id).Returns("t1");
        nonCompletionMock.SetupProperty(t => t.Enabled, true);
        nonCompletionMock.Setup(t => t.ShouldFireAsync(It.IsAny<TriggerContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var completion = new JobCompletionHockTrigger("jobA", JobTaskStatusEnum.Success);

        var entries = new[]
        {
            new KeyValuePair<IJob, IReadOnlyList<ITrigger>>(jobMock.Object, (IReadOnlyList<ITrigger>)new ITrigger[] { nonCompletionMock.Object, completion })
        };

        svcMock.Setup(s => s.GetAll()).Returns(entries);

        var middleware = new JobHockMiddleware(svcMock.Object);

        // Act
        await middleware.InvokeAsync(contextMock.Object, () => Task.CompletedTask, CancellationToken.None);

        // Assert: nonCompletion ignored (no exception) and completion signaled
        var fired = await completion.ShouldFireAsync(new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow), CancellationToken.None);
        Assert.That(fired, Is.True);
    }
}
