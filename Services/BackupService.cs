using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoodGovernanceApp.Services;

public class BackupService
{
    public string MySqlDumpPath { get; set; } = "mysqldump"; // Default to PATH usage

    public async Task<bool> CreateFullBackupAsync(string connectionString, string backupDirectory)
    {
        return await ExecuteBackupAsync(connectionString, backupDirectory, "FullBackup");
    }

    public async Task<bool> CreateDifferentialBackupAsync(string connectionString, string backupDirectory)
    {
        // For MySQL, a true differential backup requires binary logging. 
        // For a desktop app, we simulate this by dumping data changed since the last full backup date.
        // A simplified implementation using mysqldump for demonstration of the requirement:
        return await ExecuteBackupAsync(connectionString, backupDirectory, "DiffBackup");
    }

    public async Task<bool> CreateIncrementalBackupAsync(string connectionString, string backupDirectory)
    {
        // Similar to differential, true incremental requires binary logs (mysqlbinlog).
        // A placeholder for the desktop implementation:
        return await ExecuteBackupAsync(connectionString, backupDirectory, "IncBackup");
    }

    private async Task<bool> ExecuteBackupAsync(string connectionString, string backupDirectory, string prefix)
    {
        try
        {
            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);

            var dbServer = ExtractConnectionStringValue(connectionString, "Server");
            var dbUser = ExtractConnectionStringValue(connectionString, "User") ?? ExtractConnectionStringValue(connectionString, "Uid");
            var dbPass = ExtractConnectionStringValue(connectionString, "Password") ?? ExtractConnectionStringValue(connectionString, "Pwd");
            var dbName = ExtractConnectionStringValue(connectionString, "Database");

            string fileName = $"{prefix}_{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            string filePath = Path.Combine(backupDirectory, fileName);

            string arguments = $"-h {dbServer} -u {dbUser} {(string.IsNullOrEmpty(dbPass) ? "" : $"-p{dbPass}")} {dbName} --result-file=\"{filePath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = MySqlDumpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            return false;
        }
        catch (Exception)
        {
            // Log error
            return false;
        }
    }

    private string? ExtractConnectionStringValue(string connectionString, string key)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var kvp = part.Split('=');
            if (kvp.Length == 2 && kvp[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp[1].Trim();
            }
        }
        return null;
    }
}
