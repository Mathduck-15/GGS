using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Services;
using GoodGovernanceApp.Utilities;
using GoodGovernanceApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class CrsBeneficiaryViewModel : ViewModelBase
{
    // ── Backing Fields ─────────────────────────────────────────────────────────
    private string _statusMessage        = "Press Load to fetch beneficiaries.";
    private bool   _isLoading;
    private string _searchText           = string.Empty;
    private string _beneficiaryIdFilter  = string.Empty;
    private bool   _isShowingCachedData;

    // ── Collections ────────────────────────────────────────────────────────────

    /// <summary>Master list — never filtered directly.</summary>
    public ObservableCollection<Beneficiary> Beneficiaries { get; } = new();

    /// <summary>Filtered view bound to the DataGrid.</summary>
    public ICollectionView BeneficiariesView { get; }

    // ── Properties ─────────────────────────────────────────────────────────────

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    /// <summary>True when data is coming from the local SQLite cache (offline).</summary>
    public bool IsShowingCachedData
    {
        get => _isShowingCachedData;
        set { _isShowingCachedData = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Filters by last name, first name, beneficiary ID, or address.
    /// Updates the view automatically as the user types.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            BeneficiariesView.Refresh();
            StatusMessage = string.IsNullOrWhiteSpace(value)
                ? $"✅ Showing all {Beneficiaries.Count:N0} beneficiaries{(IsShowingCachedData ? " (cached)" : "")}."
                : $"🔍 Filtering by \"{value}\" — {BeneficiariesView.Cast<Beneficiary>().Count():N0} result(s) found.";
        }
    }

    public string BeneficiaryIdFilter
    {
        get => _beneficiaryIdFilter;
        set { _beneficiaryIdFilter = value; OnPropertyChanged(); }
    }

    // ── Commands ───────────────────────────────────────────────────────────────
    public ICommand LoadCommand       { get; }
    public ICommand ClearCommand      { get; }
    public ICommand SearchByIdCommand { get; }
    public ICommand OpenAnalyticsCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────────
    public CrsBeneficiaryViewModel()
    {
        BeneficiariesView = CollectionViewSource.GetDefaultView(Beneficiaries);
        BeneficiariesView.Filter = FilterBeneficiary;

        LoadCommand       = new RelayCommand(async _ => await LoadBeneficiariesAsync());
        ClearCommand      = new RelayCommand(_ => ClearSearch());
        SearchByIdCommand = new RelayCommand(
            async _ => await SearchByBeneficiaryIdAsync(),
            _        => !string.IsNullOrWhiteSpace(BeneficiaryIdFilter));
        OpenAnalyticsCommand = new RelayCommand(ExecuteOpenAnalytics);
    }

    // ── Filter Logic ───────────────────────────────────────────────────────────
    private bool FilterBeneficiary(object obj)
    {
        if (obj is not Beneficiary b) return false;
        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var keyword = _searchText.Trim().ToLower();

        return (b.LastName?.ToLower().Contains(keyword) ?? false)
            || (b.FirstName?.ToLower().Contains(keyword) ?? false)
            || (b.MiddleName?.ToLower().Contains(keyword) ?? false)
            || (b.FullName?.ToLower().Contains(keyword) ?? false)
            || (b.BeneficiaryId?.ToLower().Contains(keyword) ?? false)
            || (b.Address?.ToLower().Contains(keyword) ?? false)
            || (b.Sex?.ToLower().Contains(keyword) ?? false)
            || (b.MaritalStatus?.ToLower().Contains(keyword) ?? false)
            || (b.DisabilityType?.ToLower().Contains(keyword) ?? false);
    }

    private void ClearSearch()
    {
        SearchText          = string.Empty;
        BeneficiaryIdFilter = string.Empty;
    }

    // ── Analytics ──────────────────────────────────────────────────────────────
    private void ExecuteOpenAnalytics(object? parameter)
    {
        if (parameter is Beneficiary b && !string.IsNullOrWhiteSpace(b.BeneficiaryId))
        {
            var fullName = b.DisplayName;
            var dbContext = App.AppHost!.Services.GetRequiredService<LocalDbContext>();
            var vm = new BeneficiaryAnalyticsViewModel(dbContext, b.BeneficiaryId, fullName);
            var window = new GoodGovernanceApp.Views.BeneficiaryAnalyticsWindow(vm);
            window.Show();
        }
    }

    // ── Data Loading ───────────────────────────────────────────────────────────
    private async Task LoadBeneficiariesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading beneficiaries...";

        try
        {
            Beneficiaries.Clear();

            var connectivity = App.AppHost!.Services.GetRequiredService<ConnectivityService>();

            if (connectivity.IsCrsOnline)
            {
                // ── Online: load live from CRS Hostinger ──────────────────────
                await LoadFromCrsLiveAsync(null);
                IsShowingCachedData = false;
            }
            else
            {
                // ── Offline: load from local SQLite cache ─────────────────────
                await LoadFromLocalCacheAsync(null);
                IsShowingCachedData = true;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to load: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Search by Beneficiary ID ────────────────────────────────────────────────
    private async Task SearchByBeneficiaryIdAsync()
    {
        string id = BeneficiaryIdFilter.Trim();
        if (string.IsNullOrWhiteSpace(id)) return;

        IsLoading     = true;
        StatusMessage = $"Searching for Beneficiary ID: {id}…";
        Beneficiaries.Clear();

        try
        {
            var connectivity = App.AppHost!.Services.GetRequiredService<ConnectivityService>();

            if (connectivity.IsCrsOnline)
            {
                await LoadFromCrsLiveAsync(id);
                IsShowingCachedData = false;
            }
            else
            {
                await LoadFromLocalCacheAsync(id);
                IsShowingCachedData = true;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Search failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Live CRS fetch ─────────────────────────────────────────────────────────
    private async Task LoadFromCrsLiveAsync(string? idFilter)
    {
        string sql = idFilter == null
            ? @"SELECT id, residents_id, beneficiary_id, user_id, civilregistry_id,
                       last_name, first_name, middle_name, full_name,
                       sex, date_of_birth, age, marital_status, address,
                       is_pwd, pwd_id_no, is_senior, senior_id_no,
                       disability_type, cause_of_disability,
                       created_at, updated_at
                FROM val_beneficiaries
                ORDER BY last_name, first_name
                LIMIT 50;"
            : @"SELECT id, residents_id, beneficiary_id, user_id, civilregistry_id,
                       last_name, first_name, middle_name, full_name,
                       sex, date_of_birth, age, marital_status, address,
                       is_pwd, pwd_id_no, is_senior, senior_id_no,
                       disability_type, cause_of_disability,
                       created_at, updated_at
                FROM val_beneficiaries
                WHERE beneficiary_id LIKE @id
                ORDER BY last_name, first_name
                LIMIT 50;";

        using var conn = new MySqlConnection(GoodGovernanceApp.Data.DatabaseConfig.CrsConnectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        if (idFilter != null) cmd.Parameters.AddWithValue("@id", $"%{idFilter}%");
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Beneficiaries.Add(MapBeneficiary(reader));
        }

        StatusMessage = idFilter == null
            ? $"✅ Loaded {Beneficiaries.Count:N0} beneficiaries (showing first 50)."
            : Beneficiaries.Count > 0
                ? $"✅ Found {Beneficiaries.Count:N0} result(s) for ID '{idFilter}'."
                : $"⚠️ No beneficiary found with ID '{idFilter}'.";
    }

    // ── Local cache fetch ──────────────────────────────────────────────────────
    private async Task LoadFromLocalCacheAsync(string? idFilter)
    {
        using var scope = App.AppHost!.Services.CreateScope();
        var localDb = scope.ServiceProvider.GetRequiredService<LocalDbContext>();

        var query = localDb.CrsBeneficiaryCache.AsQueryable();
        if (idFilter != null)
            query = query.Where(c => c.BeneficiaryId != null && c.BeneficiaryId.Contains(idFilter));

        var cached = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Take(50)
            .ToListAsync();

        foreach (var row in cached)
        {
            Beneficiaries.Add(new Beneficiary
            {
                BeneficiaryId     = row.BeneficiaryId ?? "",
                ResidentsId       = row.ResidentsId,
                LastName          = row.LastName,
                FirstName         = row.FirstName,
                MiddleName        = row.MiddleName,
                FullName          = row.FullName,
                Sex               = row.Sex,
                DateOfBirthRaw    = row.DateOfBirthRaw,
                AgeRaw            = row.AgeRaw,
                MaritalStatus     = row.MaritalStatus,
                Address           = row.Address,
                IsPwd             = row.IsPwd,
                PwdIdNo           = row.PwdIdNo,
                IsSenior          = row.IsSenior,
                SeniorIdNo        = row.SeniorIdNo,
                DisabilityType    = row.DisabilityType,
                CauseOfDisability = row.CauseOfDisability,
            });
        }

        if (!cached.Any())
        {
            StatusMessage = idFilter == null
                ? "⚠️ No cached beneficiaries available. Connect to internet and press Load."
                : $"⚠️ No cached beneficiary found with ID '{idFilter}'.";
        }
        else
        {
            DateTime? cachedAt = (await localDb.CrsBeneficiaryCache.OrderByDescending(c => c.CachedAt).FirstOrDefaultAsync())?.CachedAt;
            string cacheTime = cachedAt.HasValue ? cachedAt.Value.ToLocalTime().ToString("MMM dd HH:mm") : "unknown";
            StatusMessage = $"⚠️ Showing cached CRS data (cached {cacheTime}) — {Beneficiaries.Count:N0} record(s).";
        }
    }

    // ── Row mapper (live CRS reader) ───────────────────────────────────────────
    private static Beneficiary MapBeneficiary(MySqlDataReader reader) => new()
    {
        Id              = reader.GetInt64("id"),
        ResidentsId     = reader.GetInt64("residents_id"),
        BeneficiaryId   = reader["beneficiary_id"]?.ToString() ?? "",
        UserId          = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt32("user_id"),
        CivilRegistryId = reader["civilregistry_id"]?.ToString(),
        LastName        = reader["last_name"]?.ToString(),
        FirstName       = reader["first_name"]?.ToString(),
        MiddleName      = reader["middle_name"]?.ToString(),
        FullName        = reader["full_name"]?.ToString(),
        Sex             = reader["sex"]?.ToString(),
        DateOfBirthRaw  = reader["date_of_birth"]?.ToString(),
        AgeRaw          = reader["age"]?.ToString(),
        MaritalStatus   = reader["marital_status"]?.ToString(),
        Address         = reader["address"]?.ToString(),
        IsPwd           = reader.GetInt32("is_pwd") == 1,
        PwdIdNo         = reader["pwd_id_no"]?.ToString(),
        IsSenior        = reader.GetInt32("is_senior") == 1,
        SeniorIdNo      = reader["senior_id_no"]?.ToString(),
        DisabilityType  = reader["disability_type"]?.ToString(),
        CauseOfDisability = reader["cause_of_disability"]?.ToString(),
        CreatedAt       = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime("created_at"),
        UpdatedAt       = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime("updated_at"),
    };
}
