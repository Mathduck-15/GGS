using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;
using GoodGovernanceApp.ViewModels;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class CrsBeneficiaryViewModel : ViewModelBase
{
    private string _statusMessage = "CRS database not yet connected. Showing placeholder data.";
    private bool _isLoading;
    private bool _isCrsConnected;

    public ObservableCollection<CrsBeneficiary> Beneficiaries { get; } = new();

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

    public bool IsCrsConnected
    {
        get => _isCrsConnected;
        set { _isCrsConnected = value; OnPropertyChanged(); }
    }

    public ICommand LoadCommand { get; }

    public CrsBeneficiaryViewModel()
    {
        LoadCommand = new RelayCommand(async _ => await LoadBeneficiariesAsync());
        LoadPlaceholderData();
    }

    private void LoadPlaceholderData()
    {
        Beneficiaries.Clear();
        Beneficiaries.Add(new CrsBeneficiary { BeneficiaryId = "—", FirstName = "CRS", LastName = "Database", MiddleName = "Not Yet Shared", Address = "(address not yet available)" });
        StatusMessage = "CRS database not yet connected. These are placeholder rows.";
        IsCrsConnected = false;
    }

    private async Task LoadBeneficiariesAsync()
    {
        IsLoading = true;
        StatusMessage = "Connecting to CRS database...";

        try
        {
            string crsConnStr = ConfigHelper.BuildConnectionString("CrsConfig.txt");

            if (string.IsNullOrEmpty(crsConnStr) || crsConnStr.Contains("Server=;"))
            {
                StatusMessage = "❌ CRS connection not configured. Go to Settings to set CRS credentials.";
                IsLoading = false;
                return;
            }

            Beneficiaries.Clear();
            int batchSize = 500;
            int offset = 0;
            int totalLoaded = 0;

            while (true)
            {
                using var conn = new MySqlConnection(crsConnStr);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(
                    $"SELECT beneficiary_id, first_name, last_name, middle_name, address " +
                    $"FROM val_beneficiaries LIMIT {batchSize} OFFSET {offset}", conn);

                using var reader = await cmd.ExecuteReaderAsync();
                int batchCount = 0;

                while (await reader.ReadAsync())
                {
                    batchCount++;
                    var b = new CrsBeneficiary
                    {
                        BeneficiaryId = reader["beneficiary_id"]?.ToString() ?? "",
                        FirstName     = reader["first_name"]?.ToString()     ?? "",
                        LastName      = reader["last_name"]?.ToString()      ?? "",
                        MiddleName    = reader["middle_name"]?.ToString()    ?? "",
                        Address       = string.IsNullOrWhiteSpace(reader["address"]?.ToString())
                                            ? "(address not yet available)"
                                            : reader["address"]!.ToString()!
                    };
                    Beneficiaries.Add(b);
                    totalLoaded++;
                }

                if (batchCount < batchSize) break;
                offset += batchSize;

                StatusMessage = $"Loading CRS records... {totalLoaded:N0} loaded";
                await Task.Delay(10); // Allow UI refresh between batches
            }

            IsCrsConnected = true;
            StatusMessage = $"✅ CRS loaded — {totalLoaded:N0} beneficiaries (read-only)";
        }
        catch (Exception ex)
        {
            IsCrsConnected = false;
            StatusMessage = $"❌ CRS connection failed: {ex.Message}";
            LoadPlaceholderData();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
