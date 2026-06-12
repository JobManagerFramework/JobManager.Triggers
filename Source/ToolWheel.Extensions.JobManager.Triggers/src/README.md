# ToolWheel.Extensions.JobManager.Triggers

Built-in trigger and filter implementations for the ToolWheel Job Manager. Provides ready-to-use triggers for common scheduling scenarios and filters for temporal gating.

## Package Info

| Property | Value |
|---|---|
| Target Framework | `net8.0; net10.0` |
| NuGet Package ID | `ToolWheel.Extensions.JobManager.Triggers` |
| Project Reference | `ToolWheel.Extensions.JobManager.Abstractions` |
| Package Dependencies | `Microsoft.Extensions.Hosting.Abstractions` |

## Auto Configuration

This package ships `JobManagerTriggerConfigurator` implementing `IAutoFeatureConfigurator`. When `AddJobManager()` scans loaded assemblies it automatically:

1. Registers `ITriggerService` and `ITriggerFilterService` as singletons.
2. Registers `TriggerBackgroundService` as a hosted service.
3. Adds `JobHockMiddleware` to the execution pipeline.

**No manual registration is required** – referencing the package is sufficient.

Per-job trigger and filter registrations are applied via `TriggerFeature.Apply`, which is called by `IJobService.Add` for each job.

---

## Triggers

### `IntervalTrigger`

Fires at a fixed time interval. Implements `ISchedulableTrigger` for adaptive polling.

```csharp
jobs.Add("heartbeat", worker.Ping)
    .Enabled()
    .Trigger()
        .WithInterval(TimeSpan.FromMinutes(5))
    .And();

// Fire immediately on the first evaluation
.WithInterval(TimeSpan.FromSeconds(30), fireImmediately: true)

// Start at a specific point in time
.WithInterval(TimeSpan.FromHours(1), startAt: DateTimeOffset.Parse("2025-01-01T08:00:00Z"))
```

### `DateTrigger`

Fires once at a specific date (and optionally time). After firing the trigger does not fire again unless `Reset()` is called.

```csharp
// Fire at midnight on a specific date (local time)
var trigger = new DateTrigger(new DateOnly(2025, 6, 15));

// Fire at a specific date and time
var trigger = new DateTrigger(new DateOnly(2025, 6, 15), new TimeOnly(14, 30));

// Fire at an exact DateTimeOffset
var trigger = new DateTrigger(DateTimeOffset.Parse("2025-06-15T14:30:00+02:00"));

// Allow the trigger to fire again
trigger.Reset();
```

### `FileWatcherTrigger`

Fires when file-system changes are detected in a watched directory. Wraps `FileSystemWatcher` and queues `Created`, `Changed`, `Deleted` and `Renamed` events.

```csharp
jobs.Add("file-import", worker.Import)
    .Enabled()
    .Trigger()
        .WithFileWatcherTrigger(
            path: @"C:\Data\Incoming",
            filter: "*.csv",
            includeSubdirectories: true)
    .And();
```

The `LastEvent` property provides access to the `FileSystemEventArgs` that caused the trigger to fire. The trigger implements `IDisposable` to release the underlying watcher.

### `AppStatusChangedTrigger`

Fires when the application transitions to one of the configured target states. Status changes are signalled externally via `Signal(AppStatus)`.

```csharp
jobs.Add("startup-task", worker.Initialize)
    .Enabled()
    .Trigger()
        .WithAppStatusChangedTrigger(AppStatus.Running)
    .And();
```

`AppStatus` values: `Unknown`, `Starting`, `Running`, `Stopping`, `Stopped`.

### `JobCompletionHockTrigger`

Fires when a specific job completes with a required status. `JobHockMiddleware` automatically signals all registered `JobCompletionHockTrigger` instances after each job execution.

```csharp
jobs.Add("post-process", worker.PostProcess)
    .Enabled()
    .Trigger()
        .WithJobCompletedHock("import-job", JobTaskStatusEnum.Success)
    .And();
```

---

## Filters

Filters are evaluated after a trigger fires. If a filter blocks, the job execution is skipped for that trigger cycle.

### `TimeWindowTriggerFilter`

Blocks execution outside a time-of-day window.

```csharp
.Trigger()
    .WithInterval(TimeSpan.FromMinutes(10))
        .WithTimeWindow(new TimeOnly(8, 0), new TimeOnly(18, 0))
.And();
```

### `DayOfWeekTriggerFilter`

Blocks execution on days not in the allowed set.

```csharp
.WithAllowedDays(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                 DayOfWeek.Thursday, DayOfWeek.Friday)
```

### `MonthBoundaryTriggerFilter`

Allows execution only near the start or end of a month.

```csharp
// First 3 days of each month (toleranceDays: 2 → days 1–3)
.WithMonthBoundary(MonthBoundary.Start, toleranceDays: 2)

// Last 2 days of each month (toleranceDays: 1 → last 2 days)
.WithMonthBoundary(MonthBoundary.End, toleranceDays: 1)
```

### `QuarterBoundaryTriggerFilter`

Allows execution only near the start or end of a calendar quarter.

```csharp
// First day of each quarter only
.WithQuarterBoundary(QuarterBoundary.Start, toleranceDays: 0)

// Last 5 days of each quarter
.WithQuarterBoundary(QuarterBoundary.End, toleranceDays: 4)
```

---

## Middleware

### `JobHockMiddleware`

An `IExecutionMiddleware` that runs after every job execution. It signals all registered `JobCompletionHockTrigger` instances with the completed job's ID and final status, enabling job-completion chaining (job A triggers job B on success).

---

## Combining Triggers and Filters

Multiple triggers and filters can be chained fluently on a single job:

```csharp
jobs.Add("complex-job", worker.Run)
    .Enabled()
    .Trigger()
        .WithInterval(TimeSpan.FromMinutes(15))
            .WithTimeWindow(new TimeOnly(6, 0), new TimeOnly(22, 0))
            .WithAllowedDays(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                             DayOfWeek.Thursday, DayOfWeek.Friday)
        .WithJobCompletedHock("upstream-job")
    .And()
    .TaskLimit(2);