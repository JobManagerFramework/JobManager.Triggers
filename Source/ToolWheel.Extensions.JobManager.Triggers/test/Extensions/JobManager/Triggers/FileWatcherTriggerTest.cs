using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager;

namespace ToolWheel.Extensions.JobManager.Triggers;

[TestFixture]
public class FileWatcherTriggerTest
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
    public void Constructor_Throws_When_PathNullOrEmpty()
    {
        Assert.Throws<ArgumentException>(() => new FileWatcherTrigger(null!));
        Assert.Throws<ArgumentException>(() => new FileWatcherTrigger(string.Empty));
        Assert.Throws<ArgumentException>(() => new FileWatcherTrigger("   "));
    }

    [Test]
    public void Constructor_Sets_Id_And_Enabled()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());
        Assert.That(trigger.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(trigger.Enabled, Is.True);
    }

    [Test]
    public void ShouldFireAsync_Throws_When_ContextIsNull()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());
        Assert.ThrowsAsync<ArgumentNullException>(async () => await trigger.ShouldFireAsync(null!, CancellationToken.None));
    }

    [Test]
    public async Task ShouldFireAsync_ReturnsFalse_When_NoPendingEvents()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.False);
        Assert.That(trigger.LastEvent, Is.Null);
    }

    [Test]
    public async Task OnFileEvent_Enqueues_And_ShouldFireAsync_Dequeues()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        // Invoke private OnFileEvent(FileSystemEventArgs) via reflection
        var onFileEvent = typeof(FileWatcherTrigger).GetMethod("OnFileEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(onFileEvent, Is.Not.Null, "Private method OnFileEvent not found via reflection.");

        var args = new FileSystemEventArgs(WatcherChangeTypes.Created, "someDir", "file.txt");
        onFileEvent!.Invoke(trigger, new object[] { null!, args });

        var fired = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(fired, Is.True);
        Assert.That(trigger.LastEvent, Is.Not.Null);
        Assert.That(trigger.LastEvent, Is.InstanceOf<FileSystemEventArgs>());
        Assert.That(trigger.LastEvent!.Name, Is.EqualTo("file.txt"));
    }

    [Test]
    public async Task OnRenamedEvent_Enqueues_RenamedEvent()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());
        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);

        var onRenamed = typeof(FileWatcherTrigger).GetMethod("OnRenamedEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(onRenamed, Is.Not.Null, "Private method OnRenamedEvent not found via reflection.");

        var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, "someDir", "new.txt", "old.txt");
        onRenamed!.Invoke(trigger, new object[] { null!, args });

        var fired = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(fired, Is.True);
        Assert.That(trigger.LastEvent, Is.InstanceOf<RenamedEventArgs>());
        var renamed = (RenamedEventArgs)trigger.LastEvent!;
        Assert.That(renamed.OldName, Is.EqualTo("old.txt"));
        Assert.That(renamed.Name, Is.EqualTo("new.txt"));
    }

    [Test]
    public async Task ShouldFireAsync_ReturnsFalse_When_Disabled()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());
        trigger.Enabled = false;

        // simulate event
        var onFileEvent = typeof(FileWatcherTrigger).GetMethod("OnFileEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Created, "someDir", "file.txt");
        onFileEvent!.Invoke(trigger, new object[] { null!, args });

        var jobMock = CreateJobMock();
        var ctx = new TriggerContext(jobMock.Object, DateTimeOffset.UtcNow);
        var result = await trigger.ShouldFireAsync(ctx, CancellationToken.None);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Dispose_Disables_Watcher_And_Is_Idempotent()
    {
        var trigger = new FileWatcherTrigger(Path.GetTempPath());

        // access private _watcher to assert EnableRaisingEvents changes
        var watcherField = typeof(FileWatcherTrigger).GetField("_watcher", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(watcherField, Is.Not.Null, "_watcher field not found via reflection.");
        var watcher = (FileSystemWatcher)watcherField!.GetValue(trigger)!;

        Assert.That(watcher.EnableRaisingEvents, Is.True);

        trigger.Dispose();
        Assert.That(watcher.EnableRaisingEvents, Is.False);

        // calling Dispose again must not throw
        Assert.DoesNotThrow(() => trigger.Dispose());
    }
}
