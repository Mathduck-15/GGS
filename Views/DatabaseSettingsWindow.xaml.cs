using System.Windows;
using System.Windows.Media;
using GoodGovernanceApp.Data;
using Microsoft.Extensions.Configuration;

namespace GoodGovernanceApp.Views;

public partial class DatabaseSettingsWindow : Window
{
    public DatabaseSettingsWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        // Get the current connection string from the ViewModel binding
        var vm = DataContext as GoodGovernanceApp.ViewModels.SettingsViewModel;
        if (vm == null) return;

        string connStr = vm.RemoteConnectionString ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("YOUR_HOSTINGER_IP"))
        {
            ShowStatus(false, "⚠️ Connection string still contains placeholder values. Please fill in your real Hostinger credentials first.");
            return;
        }

        // Show spinner while testing
        TestProgress.Visibility = Visibility.Visible;
        TestConnectionBtn.IsEnabled = false;
        StatusPanel.Visibility = Visibility.Collapsed;

        try
        {
            // Build a temporary DatabaseHelper just for the test
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RemoteConnection"] = connStr,
                    ["AppSettings:UseRemoteDatabase"] = "true"
                })
                .Build();

            var helper = new DatabaseHelper(config);
            var (isSuccess, message) = await helper.TestConnectionAsync(connStr);

            ShowStatus(isSuccess, message);
        }
        catch (Exception ex)
        {
            ShowStatus(false, $"Unexpected error: {ex.Message}");
        }
        finally
        {
            TestProgress.Visibility = Visibility.Collapsed;
            TestConnectionBtn.IsEnabled = true;
        }
    }

    private void ShowStatus(bool success, string message)
    {
        StatusPanel.Visibility = Visibility.Visible;

        if (success)
        {
            StatusPanel.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // light green
            StatusPanel.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            StatusPanel.BorderThickness = new Thickness(1);
            StatusIcon.Text = "✅";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
        }
        else
        {
            StatusPanel.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // light red
            StatusPanel.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            StatusPanel.BorderThickness = new Thickness(1);
            StatusIcon.Text = "❌";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
        }

        StatusText.Text = message;
    }
}
