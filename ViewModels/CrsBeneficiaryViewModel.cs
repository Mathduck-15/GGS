using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;
using GoodGovernanceApp.ViewModels;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class CrsBeneficiaryViewModel : ViewModelBase
{
    // ── Backing Fields ─────────────────────────────────────────────────────────
    private string _statusMessage = "Press Load to fetch beneficiaries.";
    private bool _isLoading;
    private string _searchText = string.Empty;

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
                ? $"✅ Showing all {Beneficiaries.Count:N0} beneficiaries."
                : $"🔍 Filtering by \"{value}\" — {BeneficiariesView.Cast<Beneficiary>().Count():N0} result(s) found.";
        }
    }

    // ── Commands ───────────────────────────────────────────────────────────────
    public ICommand LoadCommand { get; }
    public ICommand ClearCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────────
    public CrsBeneficiaryViewModel()
    {
        BeneficiariesView = CollectionViewSource.GetDefaultView(Beneficiaries);
        BeneficiariesView.Filter = FilterBeneficiary;

        LoadCommand = new RelayCommand(async _ => await LoadBeneficiariesAsync());
        ClearCommand = new RelayCommand(_ => ClearSearch());
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
        SearchText = string.Empty;
    }

    // ── Data Loading ───────────────────────────────────────────────────────────
    private async Task LoadBeneficiariesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading beneficiaries...";

        try
        {
            Beneficiaries.Clear();

            using var conn = new MySqlConnection(ConfigHelper.BuildConnectionString("CrsConfig.txt"));
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    id, residents_id, beneficiary_id, user_id, civilregistry_id,
                    last_name, first_name, middle_name, full_name,
                    sex, date_of_birth, age, marital_status, address,
                    is_pwd, pwd_id_no, is_senior, senior_id_no,
                    disability_type, cause_of_disability,
                    created_at, updated_at
                FROM val_beneficiaries
                ORDER BY last_name, first_name";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Beneficiaries.Add(new Beneficiary
                {
                    Id = reader.GetInt64("id"),
                    ResidentsId = reader.GetInt64("residents_id"),
                    BeneficiaryId = reader["beneficiary_id"]?.ToString() ?? "",

                    UserId = reader.IsDBNull(reader.GetOrdinal("user_id"))
        ? null : reader.GetInt32("user_id"),

                    CivilRegistryId = reader["civilregistry_id"]?.ToString(),

                    LastName = reader["last_name"]?.ToString(),
                    FirstName = reader["first_name"]?.ToString(),
                    MiddleName = reader["middle_name"]?.ToString(),
                    FullName = reader["full_name"]?.ToString(),

                    Sex = reader["sex"]?.ToString(),

                    // ✅ FIXED
                    DateOfBirthRaw = reader["date_of_birth"]?.ToString(),
                    AgeRaw = reader["age"]?.ToString(),

                    MaritalStatus = reader["marital_status"]?.ToString(),
                    Address = reader["address"]?.ToString(),

                    IsPwd = reader.GetInt32("is_pwd") == 1,
                    PwdIdNo = reader["pwd_id_no"]?.ToString(),

                    IsSenior = reader.GetInt32("is_senior") == 1,
                    SeniorIdNo = reader["senior_id_no"]?.ToString(),

                    DisabilityType = reader["disability_type"]?.ToString(),
                    CauseOfDisability = reader["cause_of_disability"]?.ToString(),

                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))
        ? null : reader.GetDateTime("created_at"),

                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
        ? null : reader.GetDateTime("updated_at"),
                });
            }

            StatusMessage = $"✅ Loaded {Beneficiaries.Count:N0} beneficiaries.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to load beneficiaries: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
