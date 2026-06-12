# ToolWheel.Extensions.JobManager.Triggers.Cron

Provides a **cron-based trigger** for the ToolWheel Job Manager, powered by the [Cronos](https://github.com/HangfireIO/Cronos) library.

## Package Info

| Property | Value |
|---|---|
| Target Framework | `net8.0; net10.0` |
| NuGet Package ID | `ToolWheel.Extensions.JobManager.Triggers.Cron` |
| Project Reference | `ToolWheel.Extensions.JobManager.Abstractions` |
| Package Dependency | `Cronos` |

---

## `CronTrigger`

A trigger that fires based on a cron expression. Implements `ISchedulableTrigger` so the `TriggerBackgroundService` uses adaptive polling – it sleeps until just before the next scheduled occurrence instead of polling at a fixed interval.

### Features

- Standard **5-field** expressions: `minute hour day-of-month month day-of-week`
- Optional **6-field** expressions with a leading seconds field (`includeSeconds: true`)
- Configurable **time zone** (defaults to UTC)
- Thread-safe evaluation

### Usage

```csharp
services.AddJobManager(configure =>
{
    configure.ConfigureJobs(jobs =>
    {
        var worker = new ReportWorker();

        // Every day at 02:00 UTC
        jobs.Add("nightly-report", worker.Generate)
            .Name("Nightly Report")
            .Enabled()
            .Trigger()
                .WithCron("0 2 * * *")
            .And();

        // Every 30 seconds (6-field expression)
        jobs.Add("fast-poll", worker.Poll)
            .Name("Fast Poll")
            .Enabled()
            .Trigger()
                .WithCron("*/30 * * * * *", includeSeconds: true)
            .And();

        // Every weekday at 08:00 in local time zone
        jobs.Add("morning-job", worker.MorningTask)
            .Name("Morning Task")
            .Enabled()
            .Trigger()
                .WithCron("0 8 * * 1-5", timeZone: TimeZoneInfo.Local)
            .And();
    });
});
```

### Combining with Filters

Cron triggers can be combined with trigger filters from `ToolWheel.Extensions.JobManager.Triggers`:

```csharp
jobs.Add("business-hours-job", worker.Process)
    .Enabled()
    .Trigger()
        .WithCron("*/15 * * * *")                                      // every 15 minutes
            .WithTimeWindow(new TimeOnly(8, 0), new TimeOnly(18, 0))   // only 08:00–18:00
            .WithAllowedDays(DayOfWeek.Monday, DayOfWeek.Tuesday,
                             DayOfWeek.Wednesday, DayOfWeek.Thursday,
                             DayOfWeek.Friday)                         // weekdays only
    .And();
```

### Direct Instantiation

```csharp
// Standard 5-field expression (UTC)
var trigger = new CronTrigger("0 */6 * * *");

// 6-field expression with seconds, specific time zone
var trigger = new CronTrigger("0 0 3 * * *",
    timeZone: TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"),
    includeSeconds: true);

// Inspect the next scheduled occurrence
DateTimeOffset? next     = trigger.NextOccurrence;
DateTimeOffset? nextFire = trigger.GetNextFireTimeUtc();
```

---

## Extension Method

`WithCron` is available on `IJobDescriptionTriggerBuilder` after referencing this package:

```csharp
public static IJobDescriptionTriggerFilterBuilder WithCron(
    this IJobDescriptionTriggerBuilder builder,
    string cronExpression,
    TimeZoneInfo? timeZone = null,
    bool includeSeconds = false);