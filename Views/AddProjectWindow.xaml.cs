using System.Windows;
using System.Windows.Controls;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class AddProjectWindow : Window
{
    private AddProjectViewModel ViewModel => (AddProjectViewModel)DataContext;

    public AddProjectWindow()
    {
        InitializeComponent();

        // Subscribe to save-succeeded event so we can close the window
        Loaded += (_, _) => ViewModel.SaveSucceeded += OnSaveSucceeded;
    }

    // ── Year popup ───────────────────────────────────────────────────────────────
    private void YearButton_Click(object sender, RoutedEventArgs e)
    {
        YearPopup.IsOpen = true;
    }

    private void YearListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        YearPopup.IsOpen = false;   // close popup after selection
    }

    private void ClearYear_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedYear = null;
        YearPopup.IsOpen = false;
    }

    // ── Buttons ──────────────────────────────────────────────────────────────────
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnSaveSucceeded(object? sender, System.EventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
