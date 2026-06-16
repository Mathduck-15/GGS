using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.Services;

public class SyncService
{
    private static bool _isSyncing = false;
    public static event Action<bool>?   OnSyncStatusChanged;
    public static event Action<string>? OnSyncProgress;

    public static void StartAutoSync()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                if (ConnectivityService.IsOnline && !_isSyncing)
                    await SyncNowAsync();
            }
        });
    }

    public static async Task SyncNowAsync()
    {
        if (_isSyncing) return;

        _isSyncing = true;
        OnSyncStatusChanged?.Invoke(true);
        OnSyncProgress?.Invoke("Syncing with cloud...");

        try
        {
            using var scope = App.AppHost!.Services.CreateScope();
            var localDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cloudDb = scope.ServiceProvider.GetRequiredService<CloudDbContext>();

            // Verify cloud connection is live before attempting sync
            bool canReach = await cloudDb.Database.CanConnectAsync();
            if (!canReach)
            {
                OnSyncProgress?.Invoke("Cloud unreachable. Sync skipped.");
                return;
            }

            // ── Step 0: Ensure cloud tables have SyncId + UpdatedAt columns ──
            OnSyncProgress?.Invoke("Preparing cloud schema...");
            await MigrateCloudTablesAsync(cloudDb);

            // ── Align Superadmin SyncId to prevent duplicate PK errors ──
            var localAdmin = await localDb.Users.FirstOrDefaultAsync(u => u.Name == "superadmin");
            var cloudAdmin = await cloudDb.Users.FirstOrDefaultAsync(u => u.Name == "superadmin");
            if (localAdmin != null && cloudAdmin != null && localAdmin.SyncId != cloudAdmin.SyncId)
            {
                localAdmin.SyncId = cloudAdmin.SyncId;
                await localDb.SaveChangesAsync();
            }

            OnSyncProgress?.Invoke("Syncing users...");
            await SyncTableAsync(localDb, cloudDb, localDb.Users, cloudDb.Users);

            OnSyncProgress?.Invoke("Syncing offices...");
            await SyncTableAsync(localDb, cloudDb, localDb.Offices, cloudDb.Offices);

            OnSyncProgress?.Invoke("Syncing master budgets...");
            await SyncTableAsync(localDb, cloudDb, localDb.MasterBudgets, cloudDb.MasterBudgets);

            OnSyncProgress?.Invoke("Syncing program provisions...");
            await SyncTableAsync(localDb, cloudDb, localDb.ProgramProvisions, cloudDb.ProgramProvisions);

            OnSyncProgress?.Invoke("Syncing budget allocations...");
            await SyncTableAsync(localDb, cloudDb, localDb.BudgetAllocations, cloudDb.BudgetAllocations);

            OnSyncProgress?.Invoke("Syncing services...");
            await SyncTableAsync(localDb, cloudDb, localDb.TblServices, cloudDb.TblServices);

            OnSyncProgress?.Invoke("Syncing transactions...");
            await SyncTableAsync(localDb, cloudDb, localDb.Transactions, cloudDb.Transactions);

            OnSyncProgress?.Invoke("Syncing office transactions...");
            await SyncTableAsync(localDb, cloudDb, localDb.TblTransactions, cloudDb.TblTransactions);

            OnSyncProgress?.Invoke("Syncing project details...");
            await SyncTableAsync(localDb, cloudDb, localDb.ProjectDetails, cloudDb.ProjectDetails);

            OnSyncProgress?.Invoke("Syncing yearly budgets...");
            await SyncTableAsync(localDb, cloudDb, localDb.YearlyBudgets, cloudDb.YearlyBudgets);

            OnSyncProgress?.Invoke("Syncing office allocations...");
            await SyncTableAsync(localDb, cloudDb, localDb.OfficeAllocations, cloudDb.OfficeAllocations);

            OnSyncProgress?.Invoke("Syncing consolidated transactions...");
            await SyncTableAsync(localDb, cloudDb, localDb.ConsolidatedTransactions, cloudDb.ConsolidatedTransactions);


            OnSyncProgress?.Invoke("✔ Synced successfully");
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
            OnSyncProgress?.Invoke($"⚠ Sync Error: {ex.Message}\nDetails: {innerMsg}");
            System.Diagnostics.Debug.WriteLine($"[SyncService] Error: {ex}");
        }
        finally
        {
            _isSyncing = false;
            OnSyncStatusChanged?.Invoke(false);

            // Clear status message after 15 seconds so user has time to read it
            _ = Task.Delay(15000).ContinueWith(_ => OnSyncProgress?.Invoke("Idle"));
        }
    }

    /// <summary>
    /// Ensures the cloud (Hostinger) MySQL tables have the SyncId and UpdatedAt
    /// columns required for bidirectional sync. Safe to run repeatedly —
    /// MySQL ALTER TABLE ... ADD COLUMN errors are silently ignored.
    /// </summary>
    private static async Task MigrateCloudTablesAsync(CloudDbContext cloudDb)
    {
        // List of tables and the columns they need
        var migrations = new[]
        {
            ("users",                     "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("users",                     "updated_at","DATETIME NULL"),
            ("tbl_offices",               "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("tbl_offices",               "updated_at","DATETIME NULL"),
            ("master_budget",             "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("master_budget",             "updated_at","DATETIME NULL"),
            ("budget_allocations",        "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("budget_allocations",        "updated_at","DATETIME NULL"),
            ("tbl_program_provision",     "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("tbl_program_provision",     "updated_at","DATETIME NULL"),
            ("tbl_services",              "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("tbl_services",              "updated_at","DATETIME NULL"),
            ("transactions",              "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("transactions",              "updated_at","DATETIME NULL"),
            ("project_details",           "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("project_details",           "updated_at","DATETIME NULL"),
            ("tbl_transaction",           "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("tbl_transaction",           "updated_at","DATETIME NULL"),
            ("tbl_transaction",           "distributed_by_id", "BIGINT NULL"),
            ("tbl_transaction",           "transaction_date",  "DATETIME NULL"),
            ("consolidated_transactions", "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("consolidated_transactions", "updated_at","DATETIME NULL"),
            ("yearlybudgets",             "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("yearlybudgets",             "updated_at","DATETIME NULL"),
            ("officeallocations",         "SyncId",    "CHAR(36) NOT NULL DEFAULT (UUID())"),
            ("officeallocations",         "updated_at","DATETIME NULL"),
            ("officeallocations",         "office_id", "BIGINT NULL"),
        };

        var conn = cloudDb.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        foreach (var (table, column, definition) in migrations)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                // MySQL: ADD COLUMN only if it doesn't exist
                cmd.CommandText = $@"
                    ALTER TABLE `{table}`
                    ADD COLUMN `{column}` {definition};";
                await cmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"[SyncMigrate] Added {table}.{column}");
            }
            catch
            {
                // Column already exists — ignore. MySQL throws 1060 for duplicate column.
            }
        }
    }

    /// <summary>
    /// Bidirectional sync: push local → cloud, then pull cloud → local.
    /// Uses SyncId (GUID) as the shared key and UpdatedAt for conflict resolution (last-write-wins).
    /// </summary>
    private static async Task SyncTableAsync<T>(
        AppDbContext localDb,
        CloudDbContext cloudDb,
        DbSet<T> localSet,
        DbSet<T> cloudSet) where T : class
    {
        var syncIdProp  = typeof(T).GetProperty("SyncId");
        var updatedProp = typeof(T).GetProperty("UpdatedAt");

        // Skip tables that don't have SyncId — they are local-only (e.g. CrsBeneficiaryCache)
        if (syncIdProp == null || updatedProp == null) return;

        var localRecords = await localSet.AsNoTracking().ToListAsync();
        var cloudRecords = await cloudSet.AsNoTracking().ToListAsync();

        // Build lookup dictionaries by SyncId for fast matching
        var localById = localRecords.ToDictionary(r => (Guid)syncIdProp.GetValue(r)!);
        var cloudById = cloudRecords.ToDictionary(r => (Guid)syncIdProp.GetValue(r)!);

        // ── Push local → cloud ────────────────────────────────────────────────
        var cloudToAdd    = new List<T>();
        var cloudToUpdate = new List<(T existing, T incoming)>();

        foreach (var local in localRecords)
        {
            var syncId   = (Guid)syncIdProp.GetValue(local)!;
            var localAt  = (DateTime?)updatedProp.GetValue(local);

            if (!cloudById.TryGetValue(syncId, out var cloudMatch))
            {
                // Not in cloud → insert
                cloudToAdd.Add(local);
            }
            else
            {
                var cloudAt = (DateTime?)updatedProp.GetValue(cloudMatch);
                if (localAt > cloudAt)
                    cloudToUpdate.Add((cloudMatch, local));
            }
        }

        // Detach & add new records to cloud
        foreach (var item in cloudToAdd)
        {
            // Create a fresh entry so there's no tracking conflict
            var entry = cloudDb.Entry(item);
            if (entry.State != EntityState.Detached)
                entry.State = EntityState.Detached;
            cloudSet.Add(item);
        }

        // Update existing cloud records
        foreach (var (existing, incoming) in cloudToUpdate)
        {
            var tracked = cloudDb.Set<T>().Local.FirstOrDefault(e =>
                (Guid)syncIdProp.GetValue(e)! == (Guid)syncIdProp.GetValue(existing)!);
            if (tracked != null)
                cloudDb.Entry(tracked).CurrentValues.SetValues(incoming);
            else
            {
                cloudDb.Attach(existing);
                cloudDb.Entry(existing).CurrentValues.SetValues(incoming);
            }
        }

        await cloudDb.SaveChangesAsync();
        // Detach everything so cloud context is clean for pull phase
        foreach (var entry in cloudDb.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;

        // ── Pull cloud → local ────────────────────────────────────────────────
        var localToAdd    = new List<T>();
        var localToUpdate = new List<(T existing, T incoming)>();

        // Reload cloud records (may have been just inserted)
        cloudRecords = await cloudSet.AsNoTracking().ToListAsync();
        cloudById    = cloudRecords.ToDictionary(r => (Guid)syncIdProp.GetValue(r)!);

        foreach (var cloud in cloudRecords)
        {
            var syncId  = (Guid)syncIdProp.GetValue(cloud)!;
            var cloudAt = (DateTime?)updatedProp.GetValue(cloud);

            if (!localById.TryGetValue(syncId, out var localMatch))
            {
                localToAdd.Add(cloud);
            }
            else
            {
                var localAt = (DateTime?)updatedProp.GetValue(localMatch);
                if (cloudAt > localAt)
                    localToUpdate.Add((localMatch, cloud));
            }
        }

        foreach (var item in localToAdd)
        {
            var entry = localDb.Entry(item);
            if (entry.State != EntityState.Detached)
                entry.State = EntityState.Detached;
            localSet.Add(item);
        }

        foreach (var (existing, incoming) in localToUpdate)
        {
            var tracked = localDb.Set<T>().Local.FirstOrDefault(e =>
                (Guid)syncIdProp.GetValue(e)! == (Guid)syncIdProp.GetValue(existing)!);
            if (tracked != null)
                localDb.Entry(tracked).CurrentValues.SetValues(incoming);
            else
            {
                localDb.Attach(existing);
                localDb.Entry(existing).CurrentValues.SetValues(incoming);
            }
        }

        await localDb.SaveChangesAsync();
        foreach (var entry in localDb.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }
}
