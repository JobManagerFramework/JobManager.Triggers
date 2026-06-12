# ToolWheel JobManager

Modular infrastructure for defining, scheduling, executing and safeguarding background jobs in .NET 8 and .NET 10 applications.

## What can you do with it?

- **Register jobs** – via delegate, lambda expression or a startup class
- **Execute jobs manually** – via `IJobService.ExecuteAsync` or through the REST API
- **Trigger jobs automatically** – interval, cron, date, file-system events, application status changes or after another job completes
- **Guard executions** – task-limit per job, inter-job dependencies, rate limiting, time windows, day-of-week, month/quarter boundaries
- **Job groups** – assign jobs to named groups with a configurable concurrency limit per group (default: mutual exclusion)
- **Resilience** – circuit breaker, linear/exponential retry and per-operation/total timeouts
- **Extend freely** – custom middleware, execution conditions and self-registering feature packages via `IAutoFeatureConfigurator`
- **Persist state** – optional database storage for jobs, tasks and journal entries (PostgreSQL, SQL Server, Oracle, SQLite)
- **Dynamic job loading** – load and unload job assemblies at runtime without restarting the host
- **REST API** – optional ASP.NET Core controllers and a typed HTTP client for remote monitoring and control
- **Studio dashboard** – embedded Razor Pages UI for monitoring jobs, browsing tasks and manually triggering executions
- **Standalone host** – console application with a plugin system for dynamically loading and unloading job assemblies at runtime

## Packages

| Package | Description |
|---|---|
| [`ToolWheel.Extensions.JobManager.Abstractions`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Abstractions/src/) | Public API – interfaces, DTOs and configuration contracts. No implementations. |
| [`ToolWheel.Extensions.JobManager`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager/src/) | Core runtime – `JobService`, `JobTaskService`, middleware pipeline, DI registration via `AddJobManager()`. |
| [`ToolWheel.Extensions.JobManager.ExecutionConditions`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.ExecutionConditions/src/) | Task-limit, job-dependency and rate-limit execution conditions. Auto-configured on assembly load. |
| [`ToolWheel.Extensions.JobManager.JobGroups`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.JobGroups/src/) | Named job groups with configurable concurrency limit per group (default: mutual exclusion). |
| [`ToolWheel.Extensions.JobManager.Resilience`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Resilience/src/) | Circuit breaker, retry and timeout support. Auto-configured on assembly load. |
| [`ToolWheel.Extensions.JobManager.Triggers.Abstractions`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Triggers.Abstractions/src/) | Trigger and filter API, `TriggerBackgroundService`. |
| [`ToolWheel.Extensions.JobManager.Triggers`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Triggers/src/) | Built-in triggers (interval, date, file watcher, app status, job-completion hook) and filters (time window, day-of-week, month/quarter boundary). |
| [`ToolWheel.Extensions.JobManager.Triggers.Cron`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Triggers.Cron/src/) | Cron trigger (5- and 6-field expressions) powered by [Cronos](https://github.com/HangfireIO/Cronos). |
| [`ToolWheel.Extensions.JobManager.Database.Abstractions`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Database.Abstractions/src/) | Shared `DbContext`, versioned schema migration system and DI helpers for database providers. |
| [`ToolWheel.Extensions.JobManager.Database.PostgreSql`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Database.PostgreSql/src/) | PostgreSQL storage backend via `AddPostgreSql()`. |
| [`ToolWheel.Extensions.JobManager.Database.SqlServer`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Database.SqlServer/src/) | SQL Server storage backend via `AddSqlServer()`. |
| [`ToolWheel.Extensions.JobManager.Database.Oracle`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Database.Oracle/src/) | Oracle storage backend via `AddOracle()`. |
| [`ToolWheel.Extensions.JobManager.Database.SqlLite`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Database.SqlLite/src/) | SQLite storage backend via `AddSqlLite()`. |
| [`ToolWheel.Extensions.JobManager.Dynamic.Abstractions`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Dynamic.Abstractions/src/) | Contracts for dynamically loading and unloading job assemblies at runtime. |
| [`ToolWheel.Extensions.JobManager.Dynamic`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Dynamic/src/) | Default implementation of `IDynamicJobService` with isolated, collectible `AssemblyLoadContext` per assembly. |
| [`ToolWheel.Extensions.JobManager.API`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager/api/) | ASP.NET Core controllers and typed HTTP client for the REST API. |
| [`ToolWheel.Extensions.JobManager.UI`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager/ui/) | Embedded Job Manager Studio dashboard (Razor Pages + Studio REST API). |
| [`ToolWheel.Extensions.JobManager.Abstractions.API`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Abstractions/api/) | Kestrel host extension and `JobManagerClient` base for the REST API. |
| [`ToolWheel.Extensions.JobManager.ExecutionConditions.API`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.ExecutionConditions/api/) | REST endpoints and typed HTTP client for managing task limits and rate limits. |
| [`ToolWheel.Applications.JobManager`](Source/Applications/ToolWheel.Applications.JobManager/src/) | Standalone console host with a plugin system for dynamic job assembly loading. |

## Quick Start

### Register and run a job

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddJobManager(configure =>
{
    configure.ConfigureJobs(jobs =>
    {
        jobs.Add("my-job", new MyWorker().DoWork)
            .Name("My Job")
            .Enabled();
    });
});

await builder.Build().RunAsync();
```

### Scheduled trigger with filters

```csharp
jobs.Add("report", worker.Generate)
    .Enabled()
    .Trigger()
        .WithInterval(TimeSpan.FromHours(1))
            .WithTimeWindow(new TimeOnly(8, 0), new TimeOnly(18, 0))
            .WithAllowedDays(DayOfWeek.Monday, DayOfWeek.Friday)
    .And();
```

### Cron trigger

```csharp
jobs.Add("nightly", worker.Run)
    .Enabled()
    .Trigger()
        .WithCron("0 2 * * *")   // every day at 02:00 UTC
    .And();
```

### Resilience

```csharp
jobs.Add("import", worker.Run)
    .Enabled()
    .Resilience()
        .WithCircuitBreaker(failureThreshold: 3, openDuration: TimeSpan.FromMinutes(2))
        .WithExponentialRetry(maxAttempts: 3, initialDelay: TimeSpan.FromSeconds(2))
        .WithTimeout(t => t
            .WithOperationTimeout(TimeSpan.FromSeconds(30))
            .WithTotalTimeout(TimeSpan.FromMinutes(2)))
    .JobBuilder;
```

### Job groups (mutual exclusion)

```csharp
configure.ConfigureGroups(groups => groups.Add("etl-group", "ETL Pipeline"));

jobs.Add("extract", worker.Extract).Enabled().WithGroup("etl-group");
jobs.Add("load",    worker.Load).Enabled().WithGroup("etl-group");
```

### REST API (server-side)

```csharp
Host.CreateDefaultBuilder(args)
    .UseJobManagerRestApi(listenPort: 8080)
    .Build()
    .Run();
```

### REST API (client-side)

```csharp
var client = new JobManagerClient("http://localhost:8080");
var jobs = await client.ReadJobsAsync();
await client.ExecuteAsync("my-job");
```

### Studio dashboard

```csharp
Host.CreateDefaultBuilder(args)
    .UseJobManagerWebService(8080, ws => ws.UseStudioUI())
    .ConfigureServices(services =>
    {
        services.AddJobManager(configure =>
        {
            configure.ConfigureJobs(jobs =>
            {
                jobs.Add("my-job", new MyWorker().DoWork)
                    .Name("My Job")
                    .Enabled();
            });
        });
    })
    .Build()
    .Run();
```

Open `http://localhost:8080` in a browser to access the dashboard.

---

## Detailed Documentation

Each package has its own `README.md` in its project directory with full API reference, configuration examples and internals description.

## License

[MIT](LICENSE)