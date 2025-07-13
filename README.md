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
  "Jobs": [
    {
      "JobName": "PingJob",
      "Interval": "00:00:10",
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
- [ ] ⭐ **GitHub Star Tracker** – Poll a GitHub repo for stars, track and alert on increases
- [ ] 🧠 Memory monitor or disk usage job
- [ ] 📨 Email/Slack/webhook alerting system
- [ ] 📦 Plugin-based job discovery (load jobs from external assemblies)
- [ ] 📊 CSV/JSON file processor job
- [ ] 🕸️ Broken link checker (scan websites for 404s)

---

## 📄 License
MIT — do whatever you want. Just give credit if you share it.
