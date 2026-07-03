using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using Microsoft.Extensions.DependencyInjection;

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

    private List<string> _allNames = new();
    private List<string> _allBeneficiaryIds = new();
    private List<string> _allProjectCodes = new();
    private List<string> _allOfficeCodes = new();
    private List<string> _allBarangays = new();
    private List<string> _allHouseholdNos = new();

    public ConsolidatedSearchWindow()
    {
        InitializeComponent();
        this.Loaded += ConsolidatedSearchWindow_Loaded;
    }

    private async void ConsolidatedSearchWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.AppHost != null)
        {
            try
            {
                var dbContext = App.AppHost.Services.GetRequiredService<AppDbContext>();
                await Task.Run(() =>
                {
                    _allNames = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.FullName))
                        .Select(t => t.FullName)
                        .Distinct()
                        .ToList()!;

                    _allBeneficiaryIds = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.BeneficiaryId))
                        .Select(t => t.BeneficiaryId)
                        .Distinct()
                        .ToList()!;
                        
                    _allProjectCodes = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.ProjectCode))
                        .Select(t => t.ProjectCode)
                        .Distinct()
                        .ToList()!;
                        
                    _allOfficeCodes = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.OfficeId))
                        .Select(t => t.OfficeId)
                        .Distinct()
                        .ToList()!;

                    _allBarangays = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.Barangay))
                        .Select(t => t.Barangay)
                        .Distinct()
                        .ToList()!;

                    _allHouseholdNos = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.HouseholdNo))
                        .Select(t => t.HouseholdNo)
                        .Distinct()
                        .ToList()!;
                        
                    var recentProjects = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.ProjectCode))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.ProjectCode)
                        .Distinct()
                        .Take(10)
                        .ToList()!;
                        
                    var recentOffices = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.OfficeId))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.OfficeId)
                        .Distinct()
                        .Take(10)
                        .ToList()!;
                        
                    Dispatcher.Invoke(() => 
                    {
                        RecentProjectsListBox.ItemsSource = recentProjects;
                        RecentOfficeCodesListBox.ItemsSource = recentOffices;
                    });
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading names: {ex.Message}");
            }
        }
        
        // Ensure UI state matches the default radio button selection after everything is initialized
        SearchMode_Changed(this, new RoutedEventArgs());
    }

    private void SearchMode_Changed(object sender, RoutedEventArgs e)
    {
        if (RadioBeneficiaryId == null || RadioFullName == null || RadioProjectCode == null || RadioOfficeCode == null || RadioBarangay == null || RadioHouseholdNo == null || SearchInputPanel == null)
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
        else if (RadioProjectCode.IsChecked == true)
        {
            SearchLabel.Text = "Enter Project Code:";
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
        else if (RadioOfficeCode.IsChecked == true)
        {
            SearchLabel.Text = "Enter Office Code:";
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
        else if (RadioBarangay.IsChecked == true)
        {
            SearchLabel.Text = "Enter Barangay:";
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
        else if (RadioHouseholdNo.IsChecked == true)
        {
            SearchLabel.Text = "Enter Household No:";
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
        else if (RadioProjectCode.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter a Project Code.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "ProjectCode";
            SearchValue = SearchTextBox.Text.Trim();
        }
        else if (RadioOfficeCode.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter an Office Code.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "OfficeCode";
            SearchValue = SearchTextBox.Text.Trim();
        }
        else if (RadioBarangay.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter a Barangay.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "Barangay";
            SearchValue = SearchTextBox.Text.Trim();
        }
        else if (RadioHouseholdNo.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter a Household No.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "HouseholdNo";
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

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (RadioFullName.IsChecked != true && RadioBeneficiaryId.IsChecked != true && RadioProjectCode.IsChecked != true && RadioOfficeCode.IsChecked != true && RadioBarangay.IsChecked != true && RadioHouseholdNo.IsChecked != true)
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        string query = SearchTextBox.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        List<string> matches;
        
        if (RadioFullName.IsChecked == true)
        {
            matches = _allNames
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
        else if (RadioBeneficiaryId.IsChecked == true)
        {
            matches = _allBeneficiaryIds
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
        else if (RadioProjectCode.IsChecked == true)
        {
            matches = _allProjectCodes
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
        else if (RadioOfficeCode.IsChecked == true)
        {
            matches = _allOfficeCodes
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
        else if (RadioBarangay.IsChecked == true)
        {
            matches = _allBarangays
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
        else // RadioHouseholdNo is checked
        {
            matches = _allHouseholdNos
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        if (matches.Any())
        {
            SuggestionsListBox.ItemsSource = matches;
            SuggestionsPopup.IsOpen = true;
        }
        else
        {
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuggestionsListBox.SelectedItem is string selectedName)
        {
            // Set text without triggering TextChanged event over and over if possible,
            // but it's fine since query will match exactly and then we close popup
            SearchTextBox.Text = selectedName;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            SuggestionsPopup.IsOpen = false;
            
            // Move focus back to text box
            SearchTextBox.Focus();
        }
    }
    
    private void RecentProjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentProjectsListBox.SelectedItem is string selectedProject)
        {
            RadioProjectCode.IsChecked = true;
            SearchTextBox.Text = selectedProject;
            SearchTextBox.Focus();
        }
    }
    
    private void RecentOfficeCodesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentOfficeCodesListBox.SelectedItem is string selectedOffice)
        {
            RadioOfficeCode.IsChecked = true;
            SearchTextBox.Text = selectedOffice;
            SearchTextBox.Focus();
        }
    }
}
