using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MySqlConnector;
using GoodGovernanceApp.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.ViewModels;

public class SystemsApplicationProfileViewModel : ViewModelBase
{
    private readonly LocalDbContext _dbContext;
    private string _photoAddress = string.Empty;
    private BitmapImage? _photoPreview;

    public string PhotoAddress
    {
        get => _photoAddress;
        set { _photoAddress = value; OnPropertyChanged(); }
    }

    public BitmapImage? PhotoPreview
    {
        get => _photoPreview;
        set { _photoPreview = value; OnPropertyChanged(); }
    }

    public ICommand BrowseCommand { get; }
    public ICommand SaveCommand { get; }

    public SystemsApplicationProfileViewModel()
    {
        var scope = App.AppHost!.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        BrowseCommand = new RelayCommand(_ => ExecuteBrowse());
        SaveCommand = new RelayCommand(async _ => await ExecuteSaveAsync());

        // Fix: ensure async init runs on UI thread properly
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await InitializeAsync();
        });
    }

    private async Task InitializeAsync()
    {
        try
        {
            var profile = await _dbContext.SystemsProfiles.FirstOrDefaultAsync();

            if (profile != null)
            {
                PhotoAddress = profile.PhotoAddress ?? "";
                LoadPhoto(PhotoAddress);
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"Profile load error:\n\n" +
                    $"Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n\n" +
                    $"Inner: {ex.InnerException?.Message ?? "none"}\n\n" +
                    $"Stack: {ex.StackTrace}",
                    "Debug Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
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
                PhotoAddress = newPath;
                LoadPhoto(newPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadPhoto(string path)
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
                PhotoPreview = bi;
            }
            catch
            {
                PhotoPreview = null;
            }
        }
        else
        {
            PhotoPreview = null;
        }
    }

    private async Task ExecuteSaveAsync()
    {
        try
        {
            var profile = await _dbContext.SystemsProfiles.FirstOrDefaultAsync();

            if (profile != null)
            {
                profile.PhotoAddress = PhotoAddress;
            }
            else
            {
                profile = new SystemsProfile
                {
                    PhotoAddress = PhotoAddress
                };
                _dbContext.SystemsProfiles.Add(profile);
            }

            await _dbContext.SaveChangesAsync();

            MessageBox.Show("Systems Application Profile saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving systems application profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

