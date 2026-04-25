using System;
using System.Collections.Generic;
using System.IO;

namespace GoodGovernanceApp.Utilities;

public static class ConfigHelper
{
    public static Dictionary<string, string> ReadConfig(string fileName)
    {
        var config = new Dictionary<string, string>();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        if (!File.Exists(path)) return config;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
                config[parts[0].Trim()] = parts[1].Trim();
        }
        return config;
    }

    public static string BuildConnectionString(string configFileName)
    {
        var config = ReadConfig(configFileName);
        
        if (!config.ContainsKey("Server")) return string.Empty;

        return $"Server={config["Server"]};" +
               $"Port={config.GetValueOrDefault("Port", "3306")};" +
               $"Database={config.GetValueOrDefault("Database", "")};" +
               $"User={config.GetValueOrDefault("User", "root")};" +
               $"Password={config.GetValueOrDefault("Password", "")};" +
               "AllowZeroDateTime=True;ConvertZeroDateTime=True;" +
               "Connection Timeout=15;SslMode=None;";
    }

    public static void WriteConfig(string fileName, string server, string port, string database, string user, string password)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        var lines = new[]
        {
            $"Server={server}",
            $"Port={port}",
            $"Database={database}",
            $"User={user}",
            $"Password={password}"
        };

        File.WriteAllLines(path, lines);
    }

    public static bool IsRemoteDatabase()
    {
        var config = ReadConfig("GgmsConfig.txt");
        if (config.TryGetValue("Server", out var server))
        {
            // Simple check: if it's the specific Hostinger IP or not localhost/LAN IP
            return server == "194.59.164.58" || (!server.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !server.StartsWith("192.168."));
        }
        return false;
    }
}
