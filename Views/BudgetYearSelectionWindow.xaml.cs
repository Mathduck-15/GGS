using System.Windows;

namespace GoodGovernanceApp.Views;

public partial class BudgetYearSelectionWindow : Window
{
    public BudgetYearSelectionWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ProceedButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.BudgetYearSelectionViewModel vm && vm.SelectedYearlyBudget != null)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please select a budget year from the list.", "No Budget Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
