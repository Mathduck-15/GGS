using System;

namespace GoodGovernanceApp.Models;

/// <summary>
/// Persisted backup configuration. Serialized to/from BackupConfig.json.
/// </summary>
public class BackupSettings
{
    /// <summary>Full | Differential | Incremental</summary>
    public string BackupType { get; set; } = "Full";

    /// <summary>Once | Daily | Weekly | Monthly</summary>
    public string ScheduleType { get; set; } = "Daily";

    /// <summary>
    /// For Monthly schedule: the day of the month (1-28).
    /// For Weekly schedule: the DayOfWeek index (0=Sun … 6=Sat).
    /// Ignored for Once/Daily.
    /// </summary>
    public int ScheduleDay { get; set; } = 1;

    /// <summary>When the next automatic backup should run.</summary>
    public DateTime NextRunTime { get; set; } = DateTime.Now.AddDays(1);

    /// <summary>Absolute path to the folder where .sql files are saved.</summary>
    public string BackupFolder { get; set; } = string.Empty;

    /// <summary>Absolute path to mysqldump.exe (or just "mysqldump" if on PATH).</summary>
    public string MySqlDumpPath { get; set; } = "mysqldump";

    /// <summary>When false the scheduler skips execution even if NextRunTime has passed.</summary>
    public bool IsEnabled { get; set; } = false;
}
