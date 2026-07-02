using System;
using System.IO;
using System.Text.Json;
using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.Utilities;

/// <summary>
/// Reads and writes the backup schedule configuration to/from BackupConfig.json
/// placed alongside the application executable.
/// </summary>
public static class BackupConfigHelper
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "BackupConfig.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public static BackupSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new BackupSettings();

            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<BackupSettings>(json, JsonOpts)
                   ?? new BackupSettings();
        }
        catch
        {
            return new BackupSettings();
        }
    }

    public static void Save(BackupSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOpts);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Silently ignore — SettingsViewModel surfaces save errors via StatusMessage.
        }
    }
}
