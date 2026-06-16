using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using GoodGovernanceApp.Data;
using MySqlConnector;

namespace GoodGovernanceApp.Services;

public class ConnectivityService
{
    private static bool _isOnline = false;
    private static bool _isCrsOnline = false;
    public static event Action<bool>? OnConnectionStatusChanged;

    public static bool IsOnline => _isOnline;
    public static bool IsCrsOnline => _isCrsOnline;

    public static void StartMonitoring()
    {
        _ = Task.Run(async () =>
        {
            // ── First check immediately at startup ───────────────────────────
            _isOnline    = await CheckHostingerAsync();
            _isCrsOnline = await CheckCrsAsync();
            OnConnectionStatusChanged?.Invoke(_isOnline);

            // ── Then poll every 30 seconds ────────────────────────────────────
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));

                _isOnline    = await CheckHostingerAsync();
                _isCrsOnline = await CheckCrsAsync();

                // Always fire — subscribers decide if they care about unchanged values
                OnConnectionStatusChanged?.Invoke(_isOnline);
            }
        });
    }

    /// <summary>Immediately push current connectivity state to all subscribers.</summary>
    public static void SyncCurrentStatus()
    {
        OnConnectionStatusChanged?.Invoke(_isOnline);
    }

    // ── Hostinger GGMS ────────────────────────────────────────────────────────
    private static async Task<bool> CheckHostingerAsync()
    {
        try
        {
            string connStr = DatabaseConfig.ConnectionString;

            // Quick TCP check first (fast fail if host is unreachable)
            if (!await TcpPingAsync("194.59.164.58", 3306))
                return false;

            // Then verify with a real MySQL connection
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── CRS Database ──────────────────────────────────────────────────────────
    private static async Task<bool> CheckCrsAsync()
    {
        try
        {
            string connStr = DatabaseConfig.CrsConnectionString;

            if (!await TcpPingAsync("194.59.164.58", 3306))
                return false;

            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── TCP Ping helper ───────────────────────────────────────────────────────
    private static async Task<bool> TcpPingAsync(string host, int port, int timeoutMs = 3000)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            return await Task.WhenAny(task, Task.Delay(timeoutMs)) == task && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
