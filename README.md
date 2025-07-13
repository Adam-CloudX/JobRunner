# 🧠 JobRunner

**JobRunner** is a lightweight, extensible background job runner built with .NET 8. It provides a clean, modular architecture for running recurring or on-demand tasks with zero UI and zero friction.

Perfect for:
- ⏱️ Scheduled background jobs via `IHostedService`
- 🧩 Plug-and-play job modules (just implement `IJobTask`)
- 📂 Config-driven scheduling (`appsettings.json`)
- 🔍 Console logging with clean output
- 🛠️ Great for automation

---

## 🚀 Getting Started

### ✅ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Git

### 🛠️ Running the App

```bash
git clone https://github.com/YOUR_USERNAME/JobRunner.git
cd JobRunner
dotnet run --project JobRunner.App
```

---

## 🧠 Architecture Overview

```
JobRunner/
├── JobRunner.App/        → Console host (entry point)
├── JobRunner.Core/       → Shared contracts, scheduler, models
├── JobRunner.Jobs/       → Concrete job implementations (e.g. PingJob)
└── appsettings.json      → Configuration for job schedules
```

### 🧪 Example: PingJob

The included PingJob sends an HTTP GET request to a configured URL every 10 seconds.

Config Example (appsettings.json):
```
{
  // 🔁 Global switch: if true, all jobs will run in preview mode regardless of individual job settings.
  // This is useful for testing or dry-runs across all jobs.
  // If false, each job's "IsPreview" flag determines preview behavior.
  "RunAllJobsInPreview": false,

  "Jobs": [
    {
      "JobName": "PingJob",
      // ⏱️ How often the job should run
      "Interval": "00:00:10",

      // ✅ Controls whether this job is enabled and will be scheduled
      "Enabled": true,

      // 📝 If true, this specific job will run in preview (dry-run) mode.
      // If global RunAllJobsInPreview is true, this is ignored.
      "IsPreview": true,

      // ⚙️ Job-specific parameters
      "Parameters": {
        "Url": "https://google.com"
      }
    }
  ]
}
```

### 🛠️ Adding a New Job

- Create a class implementing `IJobTask`:
```
public class MyNewJob : IJobTask
{
    public string Name => "MyNewJob";

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var logger = context.Logger;
        logger.LogInformation("Running MyNewJob...");
        // your logic here
        await Task.CompletedTask;
    }
}
```
- Register it in `Program.cs`
```
services.AddSingleton<IJobTask, MyNewJob>();
```

---

## 📌 Roadmap Ideas
- [x] ✅ `PingJob` – Periodically ping a URL and log the status code
- [x] 🧹 **Disk Cleanup Job** – Recursively delete files older than X days from a target folder
- [x] 📝 Preview mode support for all jobs (dry-run without executing logic)
- [ ] ⭐ **GitHub Star Tracker** – Poll a GitHub repo for stars, track and alert on increases
- [ ] 🧠 Memory monitor or disk usage job
- [ ] 📨 Email/Slack/webhook alerting system
- [ ] 📦 Plugin-based job discovery (load jobs from external assemblies)
- [ ] 📊 CSV/JSON file processor job
- [ ] 🕸️ Broken link checker (scan websites for 404s)

---

## 📄 License
MIT — do whatever you want. Just give credit if you share it.