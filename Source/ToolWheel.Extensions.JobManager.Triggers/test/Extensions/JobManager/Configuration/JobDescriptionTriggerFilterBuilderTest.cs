using System;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Filters;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

[TestFixture]
public class JobDescriptionTriggerFilterBuilderTest
{
    [Test]
    public void Constructor_ThrowsOnNullJobDescriptionBuilder()
    {
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        Assert.That(() => new JobDescriptionTriggerFilterBuilder(null!, triggerBuilder, triggerFeature, filterFeature, Mock.Of<ITrigger>()), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullTriggerBuilder()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        Assert.That(() => new JobDescriptionTriggerFilterBuilder(parent, null!, triggerFeature, filterFeature, Mock.Of<ITrigger>()), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullTriggerFeature()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var filterFeature = new TriggerFilterFeature();

        Assert.That(() => new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, null!, filterFeature, Mock.Of<ITrigger>()), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullFilterFeature()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();

        Assert.That(() => new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, triggerFeature, null!, Mock.Of<ITrigger>()), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullCurrentTrigger()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        Assert.That(() => new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, triggerFeature, filterFeature, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void WithFilter_ThrowsOnNull()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("trigger-1");

        var builder = new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, triggerFeature, filterFeature, triggerMock.Object);

        Assert.That(() => builder.WithFilter(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void WithFilter_AddsFilterToFeature_And_ReturnsBuilder()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("trigger-1");

        var filterMock = new Mock<ITriggerFilter>();

        var builder = new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, triggerFeature, filterFeature, triggerMock.Object);

        var returned = builder.WithFilter(filterMock.Object);

        Assert.That(returned, Is.SameAs(builder));
        var filters = filterFeature.GetFilters(triggerMock.Object.Id);
        Assert.That(filters, Has.Count.EqualTo(1));
        Assert.That(filters[0], Is.SameAs(filterMock.Object));
    }

    [Test]
    public void WithTimeWindow_AddsTimeWindowFilter_And_ReturnsBuilder()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("trigger-1");

        var builder = new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, triggerFeature, filterFeature, triggerMock.Object);

        var from = new TimeOnly(9, 0);
        var to = new TimeOnly(17, 30);

        var returned = builder.WithTimeWindow(from, to);

        Assert.That(returned, Is.SameAs(builder));
        var filters = filterFeature.GetFilters(triggerMock.Object.Id);
        Assert.That(filters, Has.Count.EqualTo(1));
        Assert.That(filters[0], Is.InstanceOf<TimeWindowTriggerFilter>());
    }

    [Test]
    public void WithTrigger_AddsTriggerToFeature_And_SetsCurrentTrigger()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var triggerBuilder = new Mock<IJobDescriptionTriggerBuilder>().Object;
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        var initialTrigger = new Mock<ITrigger>();
        initialTrigger.SetupGet(t => t.Id).Returns("initial");

        var newTrigger = new Mock<ITrigger>();
        newTrigger.SetupGet(t => t.Id).Returns("new-trigger");

        var builder = new JobDescriptionTriggerFilterBuilder(parent, triggerBuilder, triggerFeature, filterFeature, initialTrigger.Object);

        var returned = builder.WithTrigger(newTrigger.Object);

        Assert.That(returned, Is.SameAs(builder));
        Assert.That(triggerFeature.Triggers, Has.Count.EqualTo(1));
        Assert.That(triggerFeature.Triggers[0], Is.SameAs(newTrigger.Object));

        // verify that subsequent filters are associated with the new trigger
        var filterMock = new Mock<ITriggerFilter>();
        builder.WithFilter(filterMock.Object);
        var filters = filterFeature.GetFilters(newTrigger.Object.Id);
        Assert.That(filters, Has.Count.EqualTo(1));
        Assert.That(filters[0], Is.SameAs(filterMock.Object));
    }

    [Test]
    public void And_ReturnsParentBuilder()
    {
        var parentMock = new Mock<IJobDescriptionBuilder>();
        var parent = parentMock.Object;
        var triggerBuilderMock = new Mock<IJobDescriptionTriggerBuilder>();
        var triggerFeature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        var currentTrigger = new Mock<ITrigger>();
        currentTrigger.SetupGet(t => t.Id).Returns("trigger-1");

        var builder = new JobDescriptionTriggerFilterBuilder(parent, triggerBuilderMock.Object, triggerFeature, filterFeature, currentTrigger.Object);

        var returned = builder.Job;

        Assert.That(returned, Is.SameAs(parent));
    }
}
