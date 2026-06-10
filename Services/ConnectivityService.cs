using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Utilities;
using GoodGovernanceApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace GoodGovernanceApp.Services;

public enum SyncStatus { Offline, Online, Syncing }

/// <summary>
/// Singleton service that tracks internet / Hostinger reachability and
/// triggers SyncService on a 5-minute background timer.
/// </summary>
public class ConnectivityService
{
    private readonly IServiceProvider _services;
    private SyncStatus _status = SyncStatus.Offline;
    private bool _isCrsOnline;
    private DateTime? _lastSyncedAt;
    private CancellationTokenSource? _timerCts;

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action? ConnectivityChanged;

    // ── Properties ────────────────────────────────────────────────────────────
    public SyncStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            ConnectivityChanged?.Invoke();
        }
    }

    public bool IsOnline => _status != SyncStatus.Offline;

    public bool IsCrsOnline
    {
        get => _isCrsOnline;
        private set
        {
            if (_isCrsOnline == value) return;
            _isCrsOnline = value;
            ConnectivityChanged?.Invoke();
        }
    }

    public DateTime? LastSyncedAt
    {
        get => _lastSyncedAt;
        set
        {
            _lastSyncedAt = value;
            ConnectivityChanged?.Invoke();
        }
    }

    public string StatusText
    {
        get
        {
            return _status switch
            {
                SyncStatus.Syncing => "Syncing...",
                SyncStatus.Online when _lastSyncedAt.HasValue =>
                    $"Online — Synced ({_lastSyncedAt.Value.ToLocalTime():HH:mm})" +
                    (_isCrsOnline ? "" : " · CRS unavailable"),
                SyncStatus.Online => "Online — Not yet synced" + (_isCrsOnline ? "" : " · CRS unavailable"),
                _ => "Offline — Working from local data"
            };
        }
    }

    public string StatusDotColor =>
        _status switch
        {
            SyncStatus.Syncing => "#3B82F6",
            SyncStatus.Online  => "#22C55E",
            _                  => "#F97316"
        };

    public bool IsSyncing => _status == SyncStatus.Syncing;

    public ICommand SyncNowCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public ConnectivityService(IServiceProvider services)
    {
        _services = services;
        SyncNowCommand = new RelayCommand(async _ => await TriggerSyncAsync(), _ => !IsSyncing);
    }

    // ── Startup ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Called once at app startup. Runs first check, then starts 5-minute timer.
    /// </summary>
    public async Task StartAsync()
    {
        await CheckAndSyncAsync();
        StartBackgroundTimer();
    }

    // ── Manual sync ───────────────────────────────────────────────────────────
    public async Task TriggerSyncAsync()
    {
        await CheckAndSyncAsync();
    }

    // ── Core check + sync ─────────────────────────────────────────────────────
    private async Task CheckAndSyncAsync()
    {
        bool ggmsOnline = await CheckGgmsAsync();
        bool crsOnline  = ggmsOnline && await CheckCrsAsync();

        IsCrsOnline = crsOnline;

        if (!ggmsOnline)
        {
            Status = SyncStatus.Offline;
            return;
        }

        Status = SyncStatus.Syncing;
        try
        {
            using var scope = _services.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
            await syncService.SyncAsync(crsOnline);
            LastSyncedAt = DateTime.UtcNow;
            Status = SyncStatus.Online;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Sync error: {ex.Message}");
            Status = SyncStatus.Online; // still online even if sync partially failed
        }
    }

    // ── Reachability probes ───────────────────────────────────────────────────
    private async Task<bool> CheckGgmsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var connStr = DatabaseConfig.HostingerConnectionString;
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckCrsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var conn = new MySqlConnection(DatabaseConfig.CrsConnectionString);
            await conn.OpenAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Background timer ──────────────────────────────────────────────────────
    private void StartBackgroundTimer()
    {
        _timerCts?.Cancel();
        _timerCts = new CancellationTokenSource();
        var token = _timerCts.Token;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    await CheckAndSyncAsync();
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    public void Stop() => _timerCts?.Cancel();
}
