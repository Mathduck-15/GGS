using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    // ── Beneficiary ID – press Enter to search ───────────────────────────────────
    private void BeneficiaryIdBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.LookupBeneficiaryCommand.CanExecute(null))
        {
            ViewModel.LookupBeneficiaryCommand.Execute(null);
            e.Handled = true;
        }
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
