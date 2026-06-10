using System.Windows;
using System.Windows.Input;

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
        if (RadioBeneficiaryId == null || RadioFullName == null || SearchInputPanel == null)
            return;

        if (RadioBeneficiaryId.IsChecked == true)
        {
            SearchLabel.Text = "Enter Beneficiary ID:";
            SearchTextBox.Text = string.Empty;
            SearchTextBox.IsEnabled = true;
            SearchInputPanel.Visibility = Visibility.Visible;
            // Give keyboard focus after layout updates
            SearchTextBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                new System.Action(() =>
                {
                    SearchTextBox.IsEnabled = true;
                    SearchTextBox.Focus();
                    Keyboard.Focus(SearchTextBox);
                }));
        }
        else if (RadioFullName.IsChecked == true)
        {
            SearchLabel.Text = "Enter Full Name:";
            SearchTextBox.Text = string.Empty;
            SearchTextBox.IsEnabled = true;
            SearchInputPanel.Visibility = Visibility.Visible;
            SearchTextBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                new System.Action(() =>
                {
                    SearchTextBox.IsEnabled = true;
                    SearchTextBox.Focus();
                    Keyboard.Focus(SearchTextBox);
                }));
        }
        else // View All
        {
            SearchInputPanel.Visibility = Visibility.Collapsed;
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
