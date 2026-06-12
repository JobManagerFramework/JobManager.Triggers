using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Storage;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class TriggerServiceTest
{
    private static JobTriggerService CreateService()
    {
        var storageMock = new Mock<IExtensionOptionService>();
        var store = new Dictionary<string, Dictionary<string, ITrigger>>();

        storageMock.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITrigger>()))
            .Callback<string, string, ITrigger>((jobId, triggerId, trigger) =>
            {
                if (!store.ContainsKey(jobId))
                {
                    store[jobId] = new Dictionary<string, ITrigger>();
                }
                store[jobId][triggerId] = trigger;
            });

        storageMock.Setup(s => s.Delete(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((jobId, triggerId) =>
            {
                if (store.ContainsKey(jobId) && store[jobId].ContainsKey(triggerId))
                {
                    store[jobId].Remove(triggerId);
                    return true;
                }
                return false;
            });

        storageMock.Setup(s => s.ReadAll<ITrigger>(It.IsAny<string>()))
            .Returns<string>(jobId =>
            {
                if (store.ContainsKey(jobId))
                {
                    return store[jobId].Values.ToList();
                }
                return new List<ITrigger>();
            });

        return new JobTriggerService(storageMock.Object, new Mock<ILogger<JobTriggerService>>().Object);
    }

    [Test]
    public void Constructor_ThrowsOnNullStorage()
    {
        Assert.That(
            () => new JobTriggerService(null!, new Mock<ILogger<JobTriggerService>>().Object),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.That(
            () => new JobTriggerService(new Mock<IExtensionOptionService>().Object, null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Register_AddsTrigger_And_GetAllContainsEntry()
    {
        var service = CreateService();

        var job = new Mock<IJob>();
        job.SetupGet(j => j.Id).Returns("job-1");

        var trigger = new Mock<ITrigger>();
        trigger.SetupGet(t => t.Id).Returns("trigger-1");

        service.Register(job.Object, trigger.Object);

        var triggers = service.GetTriggers(job.Object);
        Assert.That(triggers, Has.Count.EqualTo(1));
        Assert.That(triggers, Has.Member(trigger.Object));

        var all = service.GetAll().ToArray();
        Assert.That(all, Has.Length.EqualTo(1));
        Assert.That(all[0].Key, Is.SameAs(job.Object));
        Assert.That(all[0].Value, Has.Count.EqualTo(1));
        Assert.That(all[0].Value, Has.Member(trigger.Object));
    }

    [Test]
    public void Remove_ReturnsTrue_WhenTriggerExists()
    {
        var service = CreateService();

        var job = new Mock<IJob>();
        job.SetupGet(j => j.Id).Returns("job-2");

        var trigger = new Mock<ITrigger>();
        trigger.SetupGet(t => t.Id).Returns("trigger-2");

        service.Register(job.Object, trigger.Object);

        var removed = service.Remove(job.Object, trigger.Object);
        Assert.That(removed, Is.True);
        Assert.That(service.GetTriggers(job.Object), Is.Empty);
    }

    [Test]
    public void Remove_ReturnsFalse_WhenTriggerNotRegistered()
    {
        var service = CreateService();

        var job = new Mock<IJob>();
        job.SetupGet(j => j.Id).Returns("job-3");

        var trigger = new Mock<ITrigger>();
        trigger.SetupGet(t => t.Id).Returns("trigger-3");

        Assert.That(service.Remove(job.Object, trigger.Object), Is.False);

        var registered = new Mock<ITrigger>();
        registered.SetupGet(t => t.Id).Returns("trigger-3-registered");
        service.Register(job.Object, registered.Object);

        Assert.That(service.Remove(job.Object, trigger.Object), Is.False);
    }

    [Test]
    public void Register_NullArguments_ThrowArgumentNullException()
    {
        var service = CreateService();

        var job = new Mock<IJob>();
        job.SetupGet(j => j.Id).Returns("job-4");

        var trigger = new Mock<ITrigger>();
        trigger.SetupGet(t => t.Id).Returns("trigger-4");

        Assert.That(() => service.Register(null!, trigger.Object), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => service.Register(job.Object, null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void GetTriggers_NullJob_ThrowsArgumentNullException()
    {
        var service = CreateService();

        Assert.That(() => service.GetTriggers(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void GetAll_ReturnsSnapshot_IndependentOfLaterChanges()
    {
        var service = CreateService();

        var job = new Mock<IJob>();
        job.SetupGet(j => j.Id).Returns("job-5");

        var trigger = new Mock<ITrigger>();
        trigger.SetupGet(t => t.Id).Returns("trigger-5");

        service.Register(job.Object, trigger.Object);
        var snapshot = service.GetAll().ToArray();

        Assert.That(service.Remove(job.Object, trigger.Object), Is.True);

        // Snapshot must still contain the previously registered trigger
        Assert.That(snapshot, Has.Length.EqualTo(1));
        Assert.That(snapshot[0].Key, Is.SameAs(job.Object));
        Assert.That(snapshot[0].Value, Has.Count.EqualTo(1));
        Assert.That(snapshot[0].Value, Has.Member(trigger.Object));

        // Current state should be empty
        Assert.That(service.GetTriggers(job.Object), Is.Empty);
    }
}
