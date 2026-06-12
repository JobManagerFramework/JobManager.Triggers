using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Triggers;

namespace ToolWheel.Extensions.JobManager.Configuration;

[TestFixture]
public class JobDescriptionTriggerBuilderTest
{
    [Test]
    public void Constructor_ThrowsOnNullJobDescriptionBuilder()
    {
        var feature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();

        Assert.That(() => new JobDescriptionTriggerBuilder(null!, feature, filterFeature), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullFeature()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var filterFeature = new TriggerFilterFeature();

        Assert.That(() => new JobDescriptionTriggerBuilder(parent, null!, filterFeature), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_ThrowsOnNullFilterFeature()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var feature = new TriggerFeature();

        Assert.That(() => new JobDescriptionTriggerBuilder(parent, feature, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void WithTrigger_ThrowsOnNull()
    {
        var parent = new Mock<IJobDescriptionBuilder>().Object;
        var feature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();
        var builder = new JobDescriptionTriggerBuilder(parent, feature, filterFeature);

        Assert.That(() => builder.WithTrigger(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void WithTrigger_AddsTriggerToFeature_And_ReturnsFilterBuilder()
    {
        var parentMock = new Mock<IJobDescriptionBuilder>();
        var parent = parentMock.Object;
        var feature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();
        var builder = new JobDescriptionTriggerBuilder(parent, feature, filterFeature);

        var triggerMock = new Mock<ITrigger>();
        triggerMock.SetupGet(t => t.Id).Returns("trigger-1");

        var returned = builder.WithTrigger(triggerMock.Object);

        Assert.That(returned, Is.InstanceOf<IJobDescriptionTriggerFilterBuilder>());
        Assert.That(feature.Triggers, Has.Count.EqualTo(1));
        Assert.That(feature.Triggers[0], Is.SameAs(triggerMock.Object));
    }

    [Test]
    public void And_ReturnsParentBuilder()
    {
        var parentMock = new Mock<IJobDescriptionBuilder>();
        var parent = parentMock.Object;
        var feature = new TriggerFeature();
        var filterFeature = new TriggerFilterFeature();
        var builder = new JobDescriptionTriggerBuilder(parent, feature, filterFeature);

        var returned = builder.Job;

        Assert.That(returned, Is.SameAs(parent));
    }
}
