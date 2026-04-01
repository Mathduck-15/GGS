using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;

namespace GoodGovernanceApp.Services;

/// <summary>
/// Singleton background service that checks every minute whether a scheduled
/// backup is due and triggers it automatically.
/// 
/// Scheduling logic:
///   Once      – runs one time at NextRunTime, then disables itself.
///   Daily     – advances NextRunTime by 1 day after each run.
///   Weekly    – advances NextRunTime by 7 days after each run.
///   Monthly   – advances NextRunTime by 1 calendar month after each run,
///               keeping the configured ScheduleDay as the day of the month.
/// </summary>
public class BackupSchedulerService : IDisposable
{
    private Timer? _timer;
    private BackupSettings _settings = new();
    private string _activeConnectionString = string.Empty;
    private readonly BackupService _backupService = new();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Start (or restart) the scheduler with the given settings.</summary>
    public void Start(BackupSettings settings, string connectionString)
    {
        _settings = settings;
        _activeConnectionString = connectionString;
        _backupService.MySqlDumpPath = settings.MySqlDumpPath;

        // Stop any running timer before restarting.
        _timer?.Dispose();

        if (!settings.IsEnabled) return;

        // Tick every 60 seconds; first tick after 5 s so startup delay is minimal.
        _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
    }

    /// <summary>Stop the scheduler without changing saved settings.</summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    // ── Timer Callback ────────────────────────────────────────────────────────

    private void OnTimerTick(object? state)
    {
        if (!_settings.IsEnabled) return;
        if (DateTime.Now < _settings.NextRunTime) return;

        // Run backup asynchronously; fire-and-forget with proper logging inside.
        _ = Task.Run(async () =>
        {
            string folder = string.IsNullOrWhiteSpace(_settings.BackupFolder)
                ? Path.Combine(AppContext.BaseDirectory, "Backups")
                : _settings.BackupFolder;

            bool success = _settings.BackupType switch
            {
                "Differential" => await _backupService.CreateDifferentialBackupAsync(_activeConnectionString, folder),
                "Incremental"  => await _backupService.CreateIncrementalBackupAsync(_activeConnectionString, folder),
                _              => await _backupService.CreateFullBackupAsync(_activeConnectionString, folder)
            };

            string logLine = success
                ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✅ Scheduled {_settings.BackupType} backup completed."
                : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Scheduled {_settings.BackupType} backup FAILED.";

            AppendToLog(folder, logLine);

            // Advance NextRunTime and persist.
            AdvanceSchedule();
            BackupConfigHelper.Save(_settings);
        });
    }

    // ── Schedule Advancement ─────────────────────────────────────────────────

    private void AdvanceSchedule()
    {
        switch (_settings.ScheduleType)
        {
            case "Once":
                // Only runs once — disable after execution.
                _settings.IsEnabled = false;
                Stop();
                break;

            case "Daily":
                _settings.NextRunTime = _settings.NextRunTime.AddDays(1);
                break;

            case "Weekly":
                _settings.NextRunTime = _settings.NextRunTime.AddDays(7);
                break;

            case "Monthly":
                // Keep the same time-of-day, advance to the next calendar month.
                var next = _settings.NextRunTime.AddMonths(1);
                int maxDay = DateTime.DaysInMonth(next.Year, next.Month);
                int targetDay = Math.Min(_settings.ScheduleDay, maxDay);
                _settings.NextRunTime = new DateTime(next.Year, next.Month, targetDay,
                    _settings.NextRunTime.Hour, _settings.NextRunTime.Minute, 0);
                break;
        }
    }

    // ── Log Helper ────────────────────────────────────────────────────────────

    private static void AppendToLog(string folder, string message)
    {
        try
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string logPath = Path.Combine(folder, "backup_schedule.log");
            File.AppendAllText(logPath, message + Environment.NewLine);
        }
        catch { /* If logging itself fails we must not crash the timer thread. */ }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
