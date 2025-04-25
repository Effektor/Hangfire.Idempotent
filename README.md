
# Hangfire.IdempotentJobs

**Hangfire.IdempotentJobs** is a lightweight extension for [Hangfire](https://www.hangfire.io/) that prevents duplicate jobs from being enqueued based on their method and arguments.

- ✅ Works for normal jobs, delayed jobs, and recurring jobs
- ✅ No custom job IDs or special scheduling needed
- ✅ Transparent integration through an `[IdempotentJob]` attribute
- ✅ Automatic deduplication during job creation

---

## Installation

```bash
dotnet add package Effektor.Hangfire.IdempotentJobs
```

---

## Usage

1. **Enable the extension in your Hangfire configuration:**

```csharp
GlobalConfiguration.Configuration
    .UseSqlServerStorage("your_connection_string")
    .UseIdempotent();
```

2. **Mark your job methods with `[IdempotentJob]`:**

```csharp
using Hangfire.Idempotent;

public class OrderService
{
    [IdempotentJob]
    public void ProcessOrder(int orderId)
    {
        // Your processing logic
    }
}
```

3. **Enqueue jobs normally:**

```csharp
BackgroundJob.Enqueue(() => orderService.ProcessOrder(42));
```

Or for recurring jobs:

```csharp
RecurringJob.AddOrUpdate(() => orderService.ProcessOrder(42), Cron.Hourly);
```

If a job with the same method and arguments already exists in the queue, scheduled jobs, or is currently being fetched for processing, the new job **will not be created**.

---

## How It Works

- Hooks into Hangfire's job creation pipeline using a custom `IClientFilter`.
- At job creation time, scans the job queues (`Enqueued`, `Scheduled`, `Fetched`) for existing jobs matching:
  - Same method
  - Same arguments
- If a matching job exists, the new job is canceled before it’s created.
- The `[IdempotentJob]` attribute is used to opt-in specific methods for deduplication.

---

## Notes

- **Performance:** By default, scans up to 500 jobs in each state. You can adjust the number depending on your load.
- **Race conditions:** There is a minimal risk of race conditions when a job switches state, e.g. goes from Enqueueued -> Scheduled and similar.
- **Limitations:** Failed jobs are not scanned; only active jobs (Enqueued, Scheduled, Fetched) are considered for deduplication.

## License

MIT License

