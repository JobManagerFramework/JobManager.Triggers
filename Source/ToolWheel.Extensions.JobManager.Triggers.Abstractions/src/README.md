# ToolWheel.Extensions.JobManager.Triggers.Abstractions

Public API, contracts and default service implementations for the ToolWheel Job Manager trigger and filter system. This package defines how triggers and filters work but ships **no concrete trigger implementations** – those are provided by `ToolWheel.Extensions.JobManager.Triggers` and `ToolWheel.Extensions.JobManager.Triggers.Cron`.

## Package Info

| Property | Value |
|---|---|
| Target Framework | `net8.0; net10.0` |
| NuGet Package ID | `ToolWheel.Extensions.JobManager.Triggers.Abstractions` |
| Project Reference | `ToolWheel.Extensions.JobManager.Abstractions` |
| Package Dependencies | `Microsoft.Extensions.Hosting.Abstractions` |

---

## Core Interfaces

### `ITrigger`

Represents a trigger that determines whether an associated job should be executed. Implementations decide autonomously if the trigger should fire.

```csharp
public interface ITrigger
{
    string Id { get; }
    bool Enabled { get; set; }
    Task<bool> ShouldFireAsync(TriggerContext context, CancellationToken cancellationToken);
}
```

### `ISchedulableTrigger`

Extended trigger interface for triggers that can predict their next fire time. The background service uses adaptive polling for schedulable triggers instead of a fixed evaluation interval.

```csharp
public interface ISchedulableTrigger : ITrigger
{
    DateTimeOffset? GetNextFireTimeUtc();
}
```

### `TriggerContext`

Provides contextual information to a trigger during evaluation.

```csharp
public sealed record TriggerContext(IJob Job, DateTimeOffset SignalTimestamp);
```

---

## Filter API

### `ITriggerFilter`

Evaluated before a trigger initiates job execution. Returns a `TriggerFilterStatus` to indicate whether the trigger should proceed or be blocked. Filters support both synchronous and asynchronous evaluation.

```csharp
public interface ITriggerFilter
{
    void Evaluate(ITriggerFilterContext context, ref TriggerFilterStatus status);
    ValueTask<TriggerFilterStatus> EvaluateAsync(ITriggerFilterContext context, TriggerFilterStatus status, CancellationToken cancellationToken);
    bool IsAsync { get; }
    int Order { get; }
}
```

### `TriggerFilterStatus`

```csharp
public sealed record TriggerFilterStatus(bool ShouldExecute, string Message);

// Predefined instances
TriggerFilterStatus.Allow   // ShouldExecute = true
TriggerFilterStatus.Block   // ShouldExecute = false
TriggerFilterStatus.Blocked("Custom reason")
```

---

## Services

### `IJobTriggerService`

Manages the association between jobs and their triggers at runtime.

```csharp
public interface IJobTriggerService
{
    void Register(IJob job, ITrigger trigger);
    bool Remove(IJob job, ITrigger trigger);
    IReadOnlyList<ITrigger> GetTriggers(IJob job);
    IEnumerable<KeyValuePair<IJob, IReadOnlyList<ITrigger>>> GetAll();
}
```

### `IJobTriggerFilterService`

Manages the association between triggers and their filters.

```csharp
public interface IJobTriggerFilterService
{
    void Register(ITrigger trigger, ITriggerFilter filter);
    bool Remove(ITrigger trigger, ITriggerFilter filter);
    void Clear(ITrigger trigger);
    IReadOnlyList<ITriggerFilter> GetFilters(ITrigger trigger);
}
```

### `JobTriggerBackgroundService`

A `BackgroundService` that periodically evaluates all registered triggers and executes associated jobs when a trigger fires. Uses adaptive polling when triggers implement `ISchedulableTrigger`. During shutdown, it requests cancellation for all running job tasks and waits for them to complete.

---

## Fluent Configuration

### Entry point

```csharp
jobs.Add("my-job", worker.Run)
    .Enabled()
    .Trigger()                          // opens the trigger builder
        .WithTrigger(myTrigger)         // add any ITrigger instance
            .WithFilter(myFilter)       // optionally attach filters
        .WithTrigger(anotherTrigger)    // add more triggers
    .And();                             // return to the job builder
```

### `IJobDescriptionTriggerBuilder`

Builder interface for adding triggers to a job description.

### `IJobDescriptionTriggerFilterBuilder`

Builder interface for attaching filters to a specific trigger.

---

## Auto Configuration

This package ships `JobManagerTriggerConfigurator` implementing `IAutoFeatureConfigurator`. When `AddJobManager()` scans loaded assemblies it automatically:

1. Registers `IJobTriggerService` and `IJobTriggerFilterService` as singletons.
2. Registers `JobTriggerBackgroundService` as a hosted service.

**No manual registration is required** – referencing the package is sufficient.