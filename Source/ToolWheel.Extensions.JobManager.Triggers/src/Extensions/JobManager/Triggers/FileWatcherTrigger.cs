using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Triggers;

/// <summary>
/// Trigger that fires when file system changes (create, modify, delete, rename)
/// are detected in a watched directory.
/// Implements <see cref="IDisposable"/> to release the underlying <see cref="FileSystemWatcher"/>.
/// </summary>
public sealed class FileWatcherTrigger : ITrigger, IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentQueue<FileSystemEventArgs> _pendingEvents = new();
    private readonly object _sync = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="FileWatcherTrigger"/> that watches the given directory.
    /// </summary>
    /// <param name="path">The directory path to watch.</param>
    /// <param name="filter">
    /// Optional file filter pattern (e.g. <c>"*.json"</c>). Defaults to <c>"*.*"</c>.
    /// </param>
    /// <param name="includeSubdirectories">Whether to watch subdirectories. Defaults to <c>false</c>.</param>
    /// <param name="changeTypes">
    /// The <see cref="NotifyFilters"/> to observe. Defaults to
    /// <see cref="NotifyFilters.FileName"/> | <see cref="NotifyFilters.LastWrite"/>.
    /// </param>
    public FileWatcherTrigger(
        string path,
        string filter = "*.*",
        bool includeSubdirectories = false,
        NotifyFilters changeTypes = NotifyFilters.FileName | NotifyFilters.LastWrite)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be null or empty.", nameof(path));
        }

        _watcher = new FileSystemWatcher(path, filter)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = changeTypes,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnRenamedEvent;

        Id = Guid.NewGuid().ToString("D");
        Enabled = true;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets the most recent file system event that caused the trigger to fire,
    /// or <c>null</c> if no event has been consumed yet.
    /// </summary>
    public FileSystemEventArgs? LastEvent { get; private set; }

    /// <inheritdoc />
    public Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (cancellationToken.IsCancellationRequested || !Enabled)
        {
            return Task.FromResult(false);
        }

        if (_pendingEvents.TryDequeue(out var fsEvent))
        {
            LastEvent = fsEvent;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Releases the <see cref="FileSystemWatcher"/> and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileEvent;
        _watcher.Changed -= OnFileEvent;
        _watcher.Deleted -= OnFileEvent;
        _watcher.Renamed -= OnRenamedEvent;
        _watcher.Dispose();
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        _pendingEvents.Enqueue(e);
    }

    private void OnRenamedEvent(object sender, RenamedEventArgs e)
    {
        _pendingEvents.Enqueue(e);
    }
}
