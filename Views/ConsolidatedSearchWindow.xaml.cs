using System.Windows;
using MaterialDesignThemes.Wpf;

namespace GoodGovernanceApp.Views;

public partial class ConsolidatedSearchWindow : Window
{
    /// <summary>
    /// The search mode chosen by the user.
    /// "BeneficiaryId" | "FullName" | "ViewAll"
    /// </summary>
    public string SearchMode { get; private set; } = "ViewAll";

    /// <summary>
    /// The search value entered (empty when ViewAll is selected).
    /// </summary>
    public string SearchValue { get; private set; } = string.Empty;

    public ConsolidatedSearchWindow()
    {
        InitializeComponent();
    }

    private void SearchMode_Changed(object sender, RoutedEventArgs e)
    {
        if (RadioBeneficiaryId == null || RadioFullName == null || SearchInputCard == null)
            return;

        if (RadioBeneficiaryId.IsChecked == true)
        {
            SearchInputCard.Visibility = Visibility.Visible;
            SearchLabel.Text = "Enter Beneficiary ID:";
            SearchTextBox.Text = string.Empty;
            HintAssist.SetHint(SearchTextBox, "e.g. BEN-00123");
        }
        else if (RadioFullName.IsChecked == true)
        {
            SearchInputCard.Visibility = Visibility.Visible;
            SearchLabel.Text = "Enter Full Name:";
            SearchTextBox.Text = string.Empty;
            HintAssist.SetHint(SearchTextBox, "e.g. Juan Dela Cruz");
        }
        else // View All
        {
            SearchInputCard.Visibility = Visibility.Collapsed;
            SearchTextBox.Text = string.Empty;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ProceedButton_Click(object sender, RoutedEventArgs e)
    {
        if (RadioBeneficiaryId.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter a Beneficiary ID.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "BeneficiaryId";
            SearchValue = SearchTextBox.Text.Trim();
        }
        else if (RadioFullName.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter a Full Name.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "FullName";
            SearchValue = SearchTextBox.Text.Trim();
        }
        else
        {
            SearchMode = "ViewAll";
            SearchValue = string.Empty;
        }

        DialogResult = true;
        Close();
    }
}
