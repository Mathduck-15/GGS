using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace GoodGovernanceApp.Services;

/// <summary>
/// Performs bidirectional sync between local SQLite (LocalDbContext) and Hostinger MySQL (AppDbContext).
/// Pull = MySQL → SQLite (last-write-wins on UpdatedAt).
/// Push = SQLite → MySQL (last-write-wins on UpdatedAt).
/// Also refreshes the CRS beneficiary cache from CRS Hostinger.
///
/// NEVER called directly from ViewModels — only called by ConnectivityService.
/// </summary>
public class SyncService
{
    private readonly IDbContextFactory<LocalDbContext> _localFactory;
    private readonly IDbContextFactory<AppDbContext>   _remoteFactory;

    public SyncService(
        IDbContextFactory<LocalDbContext> localFactory,
        IDbContextFactory<AppDbContext>   remoteFactory)
    {
        _localFactory  = localFactory;
        _remoteFactory = remoteFactory;
    }

    public async Task SyncAsync(bool syncCrsCache)
    {
        using var local  = await _localFactory.CreateDbContextAsync();
        using var remote = await _remoteFactory.CreateDbContextAsync();

        // Auto-add SyncId / UpdatedAt columns to Hostinger MySQL if they don't exist yet.
        // Safe to call every time — IF NOT EXISTS means it's a no-op after the first run.
        await ApplyRemoteMigrationsAsync(remote);

        await PullAsync(local, remote);
        await PushAsync(local, remote);

        if (syncCrsCache)
            await RefreshCrsCacheAsync(local);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AUTO-MIGRATION — adds SyncId + UpdatedAt to Hostinger MySQL tables
    // Runs every sync but is a no-op after the first successful run.
    // ══════════════════════════════════════════════════════════════════════════
    private static async Task ApplyRemoteMigrationsAsync(AppDbContext remote)
    {
        // Table name → approximate row count limit for the UUID backfill
        var tables = new[]
        {
            "users",
            "validate_users",
            "audit_trails",
            "tbl_offices",
            "master_budget",
            "budget_allocations",
            "tbl_program_provision",
            "tbl_transaction",
            "tbl_services",
            "project_details",
            "parameters",
            "categories",
            "yearlybudgets",
            "officeallocations",
            "uploaded_files",
            "evaluations",
        };

        var conn = remote.Database.GetDbConnection() as MySqlConnector.MySqlConnection;
        if (conn == null) return;

        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        foreach (var table in tables)
        {
            try
            {
                // 1. Add SyncId column if missing
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        ALTER TABLE `{table}`
                        ADD COLUMN IF NOT EXISTS `SyncId` CHAR(36) NULL;";
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Backfill any NULLs with a UUID
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        UPDATE `{table}` SET `SyncId` = UUID()
                        WHERE `SyncId` IS NULL OR `SyncId` = '';";
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Add UpdatedAt column if missing
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        ALTER TABLE `{table}`
                        ADD COLUMN IF NOT EXISTS `UpdatedAt` DATETIME NULL;";
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Backfill UpdatedAt with current timestamp where NULL
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        UPDATE `{table}` SET `UpdatedAt` = NOW()
                        WHERE `UpdatedAt` IS NULL;";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                // Log but never crash the sync — a single table failure
                // shouldn't block the rest.
                System.Diagnostics.Debug.WriteLine(
                    $"[SyncService] Migration skipped for '{table}': {ex.Message}");
            }
        }
    }


    // ══════════════════════════════════════════════════════════════════════════
    // PULL — MySQL → SQLite
    // ══════════════════════════════════════════════════════════════════════════
    private static async Task PullAsync(LocalDbContext local, AppDbContext remote)
    {
        // Parents first to satisfy FK constraints
        await PullSetAsync(remote.Offices,           local.Offices,           local);
        await PullSetAsync(remote.Categories,        local.Categories,        local);
        await PullSetAsync(remote.Parameters,        local.Parameters,        local);
        await PullSetAsync(remote.YearlyBudgets,     local.YearlyBudgets,     local);
        await PullSetAsync(remote.TblServices,       local.TblServices,       local);
        await PullSetAsync(remote.Users,             local.Users,             local);
        await PullSetAsync(remote.ValidateUsers,     local.ValidateUsers,     local);
        await PullSetAsync(remote.AuditTrails,       local.AuditTrails,       local);
        await PullSetAsync(remote.MasterBudgets,     local.MasterBudgets,     local);
        await PullSetAsync(remote.BudgetAllocations, local.BudgetAllocations, local);
        await PullSetAsync(remote.ProgramProvisions, local.ProgramProvisions, local);
        await PullSetAsync(remote.TblTransactions,   local.TblTransactions,   local);
        await PullSetAsync(remote.ProjectDetails,    local.ProjectDetails,    local);
        await PullSetAsync(remote.UploadedFiles,     local.UploadedFiles,     local);
        await PullSetAsync(remote.Evaluations,       local.Evaluations,       local);
        await PullSetAsync(remote.OfficeAllocations, local.OfficeAllocations, local);

        // Bulk-replace tables that don't have SyncId (reference/legacy tables)
        await BulkReplaceAsync(local, remote);
    }

    private static async Task PullSetAsync<T>(
        DbSet<T> source, DbSet<T> dest, LocalDbContext destCtx)
        where T : class, ISyncable
    {
        var remoteRows = await source.AsNoTracking().ToListAsync();
        var localMap   = await dest.ToDictionaryAsync(r => r.SyncId);

        foreach (var row in remoteRows)
        {
            if (localMap.TryGetValue(row.SyncId, out var existing))
            {
                // Last-write-wins
                if ((row.UpdatedAt ?? DateTime.MinValue) >= (existing.UpdatedAt ?? DateTime.MinValue))
                    destCtx.Entry(existing).CurrentValues.SetValues(row);
            }
            else
            {
                dest.Add(row);
            }
        }
        await destCtx.SaveChangesAsync();
    }

    private static async Task BulkReplaceAsync(LocalDbContext local, AppDbContext remote)
    {
        // SystemLog
        local.SystemLogs.RemoveRange(local.SystemLogs);
        local.SystemLogs.AddRange(await remote.SystemLogs.AsNoTracking().ToListAsync());

        // DepartmentRoles
        local.DepartmentRoles.RemoveRange(local.DepartmentRoles);
        local.DepartmentRoles.AddRange(await remote.DepartmentRoles.AsNoTracking().ToListAsync());

        // ConsolidatedTransactions
        local.ConsolidatedTransactions.RemoveRange(local.ConsolidatedTransactions);
        local.ConsolidatedTransactions.AddRange(await remote.ConsolidatedTransactions.AsNoTracking().ToListAsync());

        // Budget (legacy)
        local.Budgets.RemoveRange(local.Budgets);
        local.Budgets.AddRange(await remote.Budgets.AsNoTracking().ToListAsync());

        // Transaction (legacy)
        local.Transactions.RemoveRange(local.Transactions);
        local.Transactions.AddRange(await remote.Transactions.AsNoTracking().ToListAsync());

        await local.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUSH — SQLite → MySQL
    // ══════════════════════════════════════════════════════════════════════════
    private static async Task PushAsync(LocalDbContext local, AppDbContext remote)
    {
        await PushSetAsync(local.Offices,           remote.Offices,           remote);
        await PushSetAsync(local.Categories,        remote.Categories,        remote);
        await PushSetAsync(local.Parameters,        remote.Parameters,        remote);
        await PushSetAsync(local.YearlyBudgets,     remote.YearlyBudgets,     remote);
        await PushSetAsync(local.TblServices,       remote.TblServices,       remote);
        await PushSetAsync(local.Users,             remote.Users,             remote);
        await PushSetAsync(local.ValidateUsers,     remote.ValidateUsers,     remote);
        await PushSetAsync(local.AuditTrails,       remote.AuditTrails,       remote);
        await PushSetAsync(local.MasterBudgets,     remote.MasterBudgets,     remote);
        await PushSetAsync(local.BudgetAllocations, remote.BudgetAllocations, remote);
        await PushSetAsync(local.ProgramProvisions, remote.ProgramProvisions, remote);
        await PushSetAsync(local.TblTransactions,   remote.TblTransactions,   remote);
        await PushSetAsync(local.ProjectDetails,    remote.ProjectDetails,    remote);
        await PushSetAsync(local.UploadedFiles,     remote.UploadedFiles,     remote);
        await PushSetAsync(local.Evaluations,       remote.Evaluations,       remote);
        await PushSetAsync(local.OfficeAllocations, remote.OfficeAllocations, remote);
    }

    private static async Task PushSetAsync<T>(
        DbSet<T> source, DbSet<T> dest, AppDbContext destCtx)
        where T : class, ISyncable
    {
        var localRows   = await source.AsNoTracking().ToListAsync();
        var remoteMap   = await dest.ToDictionaryAsync(r => r.SyncId);

        foreach (var row in localRows)
        {
            if (remoteMap.TryGetValue(row.SyncId, out var existing))
            {
                if ((row.UpdatedAt ?? DateTime.MinValue) >= (existing.UpdatedAt ?? DateTime.MinValue))
                {
                    var entry = destCtx.Entry(existing);
                    entry.CurrentValues.SetValues(row);
                    // Never overwrite the remote PK with the local PK value
                    foreach (var pk in entry.Metadata.FindPrimaryKey()!.Properties)
                        entry.Property(pk.Name).IsModified = false;
                }
            }
            else
            {
                dest.Add(row);
            }
        }
        await destCtx.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CRS Cache Refresh
    // ══════════════════════════════════════════════════════════════════════════
    private static async Task RefreshCrsCacheAsync(LocalDbContext local)
    {
        try
        {
            var rows = new List<CrsBeneficiaryCache>();

            using var conn = new MySqlConnection(DatabaseConfig.CrsConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT residents_id, beneficiary_id,
                       last_name, first_name, middle_name, full_name,
                       sex, date_of_birth, age, marital_status, address,
                       is_pwd, pwd_id_no, is_senior, senior_id_no,
                       disability_type, cause_of_disability
                FROM val_beneficiaries
                ORDER BY last_name, first_name
                LIMIT 2000;";

            using var cmd    = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new CrsBeneficiaryCache
                {
                    ResidentsId       = reader.GetInt64("residents_id"),
                    BeneficiaryId     = reader["beneficiary_id"]?.ToString(),
                    LastName          = reader["last_name"]?.ToString(),
                    FirstName         = reader["first_name"]?.ToString(),
                    MiddleName        = reader["middle_name"]?.ToString(),
                    FullName          = reader["full_name"]?.ToString(),
                    Sex               = reader["sex"]?.ToString(),
                    DateOfBirthRaw    = reader["date_of_birth"]?.ToString(),
                    AgeRaw            = reader["age"]?.ToString(),
                    MaritalStatus     = reader["marital_status"]?.ToString(),
                    Address           = reader["address"]?.ToString(),
                    IsPwd             = reader.GetInt32("is_pwd") == 1,
                    PwdIdNo           = reader["pwd_id_no"]?.ToString(),
                    IsSenior          = reader.GetInt32("is_senior") == 1,
                    SeniorIdNo        = reader["senior_id_no"]?.ToString(),
                    DisabilityType    = reader["disability_type"]?.ToString(),
                    CauseOfDisability = reader["cause_of_disability"]?.ToString(),
                    CachedAt          = DateTime.UtcNow
                });
            }

            local.CrsBeneficiaryCache.RemoveRange(local.CrsBeneficiaryCache);
            await local.SaveChangesAsync();
            local.CrsBeneficiaryCache.AddRange(rows);
            await local.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncService] CRS cache refresh failed: {ex.Message}");
        }
    }
}
