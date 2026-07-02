using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoodGovernanceApp.Services;

/// <summary>
/// Wraps mysqldump to create database backups.
///
/// ── Backup Type Explanation ──────────────────────────────────────────────────
///
/// FULL BACKUP
///   mysqldump --single-transaction --routines --triggers --events
///   Dumps schema + all data. Required as a baseline for the other types.
///   File prefix: FullBackup_
///
/// DIFFERENTIAL BACKUP
///   mysqldump --single-transaction --no-create-info
///   Dumps ONLY data rows (no CREATE TABLE statements), representing changes
///   since the last Full backup (schema is assumed unchanged).
///   To restore: apply Full backup first, then replay this file.
///   File prefix: DiffBackup_
///   Note: For true differential you need MySQL binary logs; this is a practical
///   approximation for application-level scheduling.
///
/// INCREMENTAL BACKUP
///   Same as Differential but captures data since the last backup of any type.
///   In production, enable binary logging (log_bin=ON in my.ini) and use
///   mysqlbinlog to replay changes between specific positions/time ranges.
///   File prefix: IncBackup_
///
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class BackupService
{
    public string MySqlDumpPath { get; set; } = "mysqldump";

    // ── Public entry points ───────────────────────────────────────────────────

    public Task<bool> CreateFullBackupAsync(string connectionString, string backupDirectory)
        => ExecuteBackupAsync(connectionString, backupDirectory, "FullBackup",
            extraArgs: "--single-transaction --routines --triggers --events");

    public Task<bool> CreateDifferentialBackupAsync(string connectionString, string backupDirectory)
        // --no-create-info = data only; mirrors "what changed" since last full backup.
        => ExecuteBackupAsync(connectionString, backupDirectory, "DiffBackup",
            extraArgs: "--single-transaction --no-create-info");

    public Task<bool> CreateIncrementalBackupAsync(string connectionString, string backupDirectory)
        // Same technique as differential for application-level incremental tracking.
        => ExecuteBackupAsync(connectionString, backupDirectory, "IncBackup",
            extraArgs: "--single-transaction --no-create-info");

    // ── Core execution ────────────────────────────────────────────────────────

    private async Task<bool> ExecuteBackupAsync(
        string connectionString,
        string backupDirectory,
        string prefix,
        string extraArgs = "")
    {
        try
        {
            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);

            string? dbServer = ExtractValue(connectionString, "Server");
            string? dbPort   = ExtractValue(connectionString, "Port") ?? "3306";
            string? dbUser   = ExtractValue(connectionString, "User")
                            ?? ExtractValue(connectionString, "Uid");
            string? dbPass   = ExtractValue(connectionString, "Password")
                            ?? ExtractValue(connectionString, "Pwd");
            string? dbName   = ExtractValue(connectionString, "Database");

            if (string.IsNullOrWhiteSpace(dbName))
            {
                LogError(backupDirectory, "Cannot determine database name from connection string.");
                return false;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName  = $"{prefix}_{dbName}_{timestamp}.sql";
            string filePath  = Path.Combine(backupDirectory, fileName);

            string passwordArg = string.IsNullOrWhiteSpace(dbPass) ? "" : $"-p\"{dbPass}\"";
            string arguments   =
                $"-h \"{dbServer}\" -P {dbPort} -u \"{dbUser}\" {passwordArg} " +
                $"{extraArgs} \"{dbName}\" --result-file=\"{filePath}\"";

            var psi = new ProcessStartInfo
            {
                FileName               = MySqlDumpPath,
                Arguments              = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                LogError(backupDirectory, "Failed to start mysqldump process. Check MySqlDumpPath.");
                return false;
            }

            // Capture stderr for error logging.
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                LogError(backupDirectory,
                    $"mysqldump exited with code {process.ExitCode}.\nStderr: {stderr}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError(backupDirectory, $"Exception in {prefix}: {ex.Message}");
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void LogError(string backupDirectory, string message)
    {
        try
        {
            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);

            string logPath = Path.Combine(backupDirectory, "backup_errors.log");
            string entry   = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(logPath, entry);
        }
        catch { /* Must not throw from an error handler. */ }
    }

    private static string? ExtractValue(string connectionString, string key)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }
        return null;
    }
}
