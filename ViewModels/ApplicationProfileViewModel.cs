using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MySqlConnector;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace GoodGovernanceApp.ViewModels;

public class ApplicationProfileViewModel : ViewModelBase
{
    private readonly LocalDbContext _dbContext;
    private ApplicationProfileModel _profile = new();
    private BitmapImage? _logoPreview;

    public string GoveName
    {
        get => _profile.GoveName;
        set { _profile.GoveName = value; OnPropertyChanged(); }
    }

    public string Address
    {
        get => _profile.Address;
        set { _profile.Address = value; OnPropertyChanged(); }
    }

    public string LogoAddress
    {
        get => _profile.LogoAddress;
        set { _profile.LogoAddress = value; OnPropertyChanged(); }
    }

    public BitmapImage? LogoPreview
    {
        get => _logoPreview;
        set { _logoPreview = value; OnPropertyChanged(); }
    }

    public ICommand BrowseCommand { get; }
    public ICommand SaveCommand { get; }

    public ApplicationProfileViewModel()
    {
        var scope = App.AppHost!.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        BrowseCommand = new RelayCommand(_ => ExecuteBrowse());
        SaveCommand = new RelayCommand(async _ => await ExecuteSaveAsync());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var profile = await _dbContext.GoveProfiles.FirstOrDefaultAsync();

            if (profile != null)
            {
                GoveName = profile.GoveName ?? "";
                Address = profile.Address ?? "";
                LogoAddress = profile.LogoAddress ?? "";

                LoadLogoImage(LogoAddress);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading application profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteBrowse()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Logo",
            Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                string selectedPath = dialog.FileName;

                // ✅ Use AppData\Roaming instead of Program Files
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GoodGovernanceApp",
                    "Logos");

                if (!Directory.Exists(appDataFolder))
                    Directory.CreateDirectory(appDataFolder);

                string fileName = Path.GetFileName(selectedPath);
                string newPath = Path.Combine(appDataFolder, fileName);

                // If file already exists, append guid to avoid overwrite
                if (File.Exists(newPath))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);
                    newPath = Path.Combine(appDataFolder, $"{name}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}");
                }

                File.Copy(selectedPath, newPath);

                // ✅ Save absolute path — no more relative path issues
                LogoAddress = newPath; // or PhotoAddress for SystemsApplicationProfile
                LoadLogoImage(newPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadLogoImage(string path)
    {
        // Resolve relative path if needed
        if (!Path.IsPathRooted(path))
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.EndInit();
                LogoPreview = bi;
            }
            catch
            {
                LogoPreview = null;
            }
        }
        else
        {
            LogoPreview = null;
        }
    }

    private async Task ExecuteSaveAsync()
    {
        try
        {
            var profile = await _dbContext.GoveProfiles.FirstOrDefaultAsync();

            if (profile != null)
            {
                profile.GoveName = GoveName;
                profile.Address = Address;
                profile.LogoAddress = LogoAddress;
            }
            else
            {
                profile = new GoveProfile
                {
                    GoveName = GoveName,
                    Address = Address,
                    LogoAddress = LogoAddress
                };
                _dbContext.GoveProfiles.Add(profile);
            }

            await _dbContext.SaveChangesAsync();

            MessageBox.Show("Application profile saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Re-throw Event or Close dialog (Usually dialog bounds can be closed, but we just save for now)
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving application profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

