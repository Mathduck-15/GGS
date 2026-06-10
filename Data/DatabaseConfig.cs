using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GoodGovernanceApp.Data;

/// <summary>
/// Single source of truth for all database connection strings in the application.
/// Every class that needs a connection string MUST read it from here.
/// </summary>
public static class DatabaseConfig
{
    /// <summary>GGMS connection string based on the active DatabaseMode in appsettings.json.</summary>
    public static string ConnectionString
    {
        get
        {
            string dbMode = App.Config["AppSettings:DatabaseMode"] ?? "Local";
            string key = dbMode switch
            {
                "Remote" => "RemoteConnection",
                "LAN"    => "LanConnection",
                _        => "LocalConnection"
            };

            return App.Config.GetConnectionString(key)
                ?? throw new InvalidOperationException($"Connection string '{key}' not found in appsettings.json.");
        }
    }

    /// <summary>
    /// Always returns the Hostinger (cloud) connection string.
    /// Used by SyncService and ConnectivityService — never by ViewModels.
    /// </summary>
    public static string HostingerConnectionString =>
        App.Config.GetConnectionString("RemoteConnection")
            ?? throw new InvalidOperationException("RemoteConnection not found in appsettings.json.");

    /// <summary>
    /// SQLite connection string pointing to ggms.db in the app's base directory.
    /// Used by LocalDbContext — the primary context for all normal app reads/writes.
    /// </summary>
    public static string SqliteConnectionString =>
        $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ggms.db")}";

    /// <summary>CRS database connection string built from the CrsConnection section.</summary>
    public static string CrsConnectionString
    {
        get
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = App.Config["CrsConnection:Server"] ?? "localhost",
                Port = uint.TryParse(App.Config["CrsConnection:Port"], out var p) ? p : 3306,
                Database = App.Config["CrsConnection:Database"] ?? "crs_db",
                UserID = App.Config["CrsConnection:User"] ?? "root",
                Password = App.Config["CrsConnection:Password"] ?? "",
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true,
                ConnectionTimeout = 15,
                SslMode = MySqlSslMode.None
            };
            return builder.ConnectionString;
        }
    }

    /// <summary>
    /// Persists updated connection settings to appsettings.json.
    /// IConfiguration with reloadOnChange picks up changes automatically.
    /// </summary>
    public static void SaveToAppsettings(
        string mode, string ggmsConnStr,
        string crsServer, string crsPort, string crsDb, string crsUser, string crsPass)
    {
        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoodGovernanceApp");
        string path = Path.Combine(appDataFolder, "appsettings.json");
        string json = File.ReadAllText(path);
        var root = JsonNode.Parse(json)!.AsObject();

        // Update DatabaseMode
        root["AppSettings"]!["DatabaseMode"] = mode;

        // Update the active GGMS connection string
        string key = mode switch
        {
            "Remote" => "RemoteConnection",
            "LAN"    => "LanConnection",
            _        => "LocalConnection"
        };
        root["ConnectionStrings"]![key] = ggmsConnStr;

        // Update CRS connection fields
        root["CrsConnection"]!["Server"]   = crsServer;
        root["CrsConnection"]!["Port"]     = crsPort;
        root["CrsConnection"]!["Database"] = crsDb;
        root["CrsConnection"]!["User"]     = crsUser;
        root["CrsConnection"]!["Password"] = crsPass;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, root.ToJsonString(options));
    }
}
