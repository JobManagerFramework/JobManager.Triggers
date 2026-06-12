using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Storage;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class TriggerFilterServiceTest
{
    private sealed class LowOrderFilter : ITriggerFilter { public int Order => 1; }
    private sealed class HighOrderFilter : ITriggerFilter { public int Order => 10; }

    private static JobTriggerFilterService CreateService()
    {
        var storageMock = new Mock<IExtensionOptionService>();
        var storage = new Dictionary<(string, string), ITriggerFilter>();

        storageMock.Setup(s => s.Create<ITriggerFilter>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITriggerFilter>()))
            .Callback<string, string, ITriggerFilter>((ownerId, itemKey, filter) => 
                storage[(ownerId, itemKey)] = filter);

        storageMock.Setup(s => s.ReadAll<ITriggerFilter>(It.IsAny<string>()))
            .Returns<string>(ownerId => 
                storage.Where(kvp => kvp.Key.Item1 == ownerId).Select(kvp => kvp.Value).ToList());

        storageMock.Setup(s => s.Delete(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((ownerId, itemKey) => 
                storage.Remove((ownerId, itemKey)));

        storageMock.Setup(s => s.DeleteAll(It.IsAny<string>()))
            .Callback<string>(ownerId => 
            {
                var keysToRemove = storage.Keys.Where(k => k.Item1 == ownerId).ToList();
                foreach (var key in keysToRemove)
                    storage.Remove(key);
            });

        return new JobTriggerFilterService(storageMock.Object, new Mock<ILogger<JobTriggerFilterService>>().Object);
    }

    [Test]
    public void Constructor_ThrowsOnNullStorage()
    {
        Assert.That(
            () => new JobTriggerFilterService(null!, new Mock<ILogger<JobTriggerFilterService>>().Object),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.That(
            () => new JobTriggerFilterService(new Mock<IExtensionOptionService>().Object, null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Register_ThrowsOnNullTrigger()
    {
        var svc = CreateService();
        var filter = new Mock<ITriggerFilter>().Object;

        Assert.That(() => svc.Register(null!, filter), Throws.ArgumentNullException);
    }

    [Test]
    public void Register_ThrowsOnNullFilter()
    {
        var svc = CreateService();
        var trigger = new Mock<ITrigger>().Object;

        Assert.That(() => svc.Register(trigger, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Register_AddsFilter_And_GetFiltersReturnsIt()
    {
        var svc = CreateService();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("t1");

        var filterMock = new Mock<ITriggerFilter>();
        filterMock.SetupGet(f => f.Order).Returns(5);

        svc.Register(triggerMock.Object, filterMock.Object);

        var filters = svc.GetFilters(triggerMock.Object);
        Assert.That(filters, Has.Count.EqualTo(1));
        Assert.That(filters[0], Is.SameAs(filterMock.Object));
    }

    [Test]
    public void Remove_ReturnsTrue_When_FilterExists()
    {
        var svc = CreateService();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("t1");

        var filterMock = new Mock<ITriggerFilter>();
        filterMock.SetupGet(f => f.Order).Returns(1);

        svc.Register(triggerMock.Object, filterMock.Object);

        Assert.That(svc.Remove(triggerMock.Object, filterMock.Object), Is.True);
        Assert.That(svc.GetFilters(triggerMock.Object), Is.Empty);
    }

    [Test]
    public void Remove_ReturnsFalse_When_NoFiltersRegistered()
    {
        var svc = CreateService();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("t1");

        var filterMock = new Mock<ITriggerFilter>();

        Assert.That(svc.Remove(triggerMock.Object, filterMock.Object), Is.False);
    }

    [Test]
    public void Clear_RemovesAllFiltersForTrigger()
    {
        var svc = CreateService();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("t1");

        var filter1 = new Mock<ITriggerFilter>();
        var filter2 = new Mock<ITriggerFilter>();
        svc.Register(triggerMock.Object, filter1.Object);
        svc.Register(triggerMock.Object, filter2.Object);

        svc.Clear(triggerMock.Object);

        Assert.That(svc.GetFilters(triggerMock.Object), Is.Empty);
    }

    [Test]
    public void GetFilters_ReturnsOrderedByFilterOrder()
    {
        var svc = CreateService();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("t1");

        var low = new LowOrderFilter();
        var high = new HighOrderFilter();

        svc.Register(triggerMock.Object, high);
        svc.Register(triggerMock.Object, low);

        var filters = svc.GetFilters(triggerMock.Object).ToList();

        Assert.That(filters, Has.Count.EqualTo(2));
        Assert.That(filters[0].Order, Is.LessThan(filters[1].Order));
        Assert.That(filters[0], Is.SameAs(low));
        Assert.That(filters[1], Is.SameAs(high));
    }

    [Test]
    public void GetAll_ReturnsAllRegisteredTriggerEntries()
    {
        var svc = CreateService();

        var t1 = new Mock<ITrigger>();
        t1.SetupGet(t => t.Id).Returns("t1");
        var f1 = new LowOrderFilter();
        svc.Register(t1.Object, f1);

        var t2 = new Mock<ITrigger>();
        t2.SetupGet(t => t.Id).Returns("t2");
        var f2a = new LowOrderFilter();
        var f2b = new HighOrderFilter();
        svc.Register(t2.Object, f2a);
        svc.Register(t2.Object, f2b);

        var all = svc.GetAll().ToList();

        Assert.That(all, Has.Count.EqualTo(2));

        var entry1 = all.Single(e => e.Key == t1.Object);
        Assert.That(entry1.Value, Has.Count.EqualTo(1));
        Assert.That(entry1.Value[0], Is.SameAs(f1));

        var entry2 = all.Single(e => e.Key == t2.Object);
        Assert.That(entry2.Value, Has.Count.EqualTo(2));
        Assert.That(entry2.Value[0].Order, Is.LessThan(entry2.Value[1].Order));
    }
}
