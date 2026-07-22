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

    // Recent-items collections — swapped into RecentItemsListBox per mode
    private List<string> _recentProjectCodes = new();
    private List<string> _recentOfficeCodes  = new();
    private List<string> _recentBarangays    = new();
    private List<string> _recentBeneficiaryIds = new();
    private List<string> _recentFullNames = new();
    private List<string> _recentHouseholdNos = new();

    // Debounce timer — fires ApplyFilter() 300 ms after the user stops typing
    private readonly System.Windows.Threading.DispatcherTimer _debounceTimer;
    private bool _suppressTextChanged = false;

    public ConsolidatedSearchWindow()
    {
        InitializeComponent();

        // Set up debounce timer (300 ms) — stops and restarts on every keystroke
        _debounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        // Stop timer when window closes to prevent any orphaned Tick callbacks
        this.Closing += (s, e) => _debounceTimer.Stop();

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
                        .Select(t => t.ProjectCode + " - " + t.ProjectName)
                        .Distinct()
                        .ToList()!;
                        
                    _allOfficeCodes = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.OfficeId))
                        .Select(t => t.OfficeId + " - " + t.OfficeName)
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
                        .Select(t => t.ProjectCode + " - " + t.ProjectName)
                        .Distinct()
                        .Take(10)
                        .ToList()!;

                    var recentOffices = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.OfficeId))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.OfficeId + " - " + t.OfficeName)
                        .Distinct()
                        .Take(10)
                        .ToList()!;

                    var recentBarangays = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.Barangay))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.Barangay)
                        .Distinct()
                        .Take(10)
                        .ToList()!;

                    var recentBeneficiaryIds = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.BeneficiaryId))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.BeneficiaryId)
                        .Distinct()
                        .Take(10)
                        .ToList()!;

                    var recentFullNames = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.FullName))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.FullName)
                        .Distinct()
                        .Take(10)
                        .ToList()!;

                    var recentHouseholdNos = dbContext.ConsolidatedTransactions
                        .Where(t => !string.IsNullOrEmpty(t.HouseholdNo))
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.HouseholdNo)
                        .Distinct()
                        .Take(10)
                        .ToList()!;

                    Dispatcher.Invoke(() =>
                    {
                        _recentProjectCodes = recentProjects;
                        _recentOfficeCodes  = recentOffices;
                        _recentBarangays    = recentBarangays;
                        _recentBeneficiaryIds = recentBeneficiaryIds;
                        _recentFullNames = recentFullNames;
                        _recentHouseholdNos = recentHouseholdNos;

                        // If the active mode already has a recent-items panel visible,
                        // re-run SearchMode_Changed so the list reflects the just-loaded data.
                        SearchMode_Changed(this, new RoutedEventArgs());
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
        if (RadioBeneficiaryId == null || RadioFullName == null ||
            RadioBarangay == null || RadioHouseholdNo == null ||
            SearchInputPanel == null)
            return;

        // ── Helper: set focus asynchronously after layout ──────────────────
        void FocusSearchBox()
        {
            SearchTextBox.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new System.Action(() =>
                {
                    SearchTextBox.IsEnabled = true;
                    SearchTextBox.Focus();
                    Keyboard.Focus(SearchTextBox);
                }));
        }

        // ── Helper: show/configure the recent-items right panel ────────────
        void ShowRecentPanel(string header, List<string> items)
        {
            RecentItemsHeader.Text      = header;
            RecentItemsListBox.ItemsSource = items;
            RecentItemsListBox.SelectedItem = null;  // clear prior selection
            RecentItemsCard.Visibility  = items.Count > 0
                                          ? Visibility.Visible
                                          : Visibility.Collapsed;
        }

        void HideRecentPanel()
        {
            RecentItemsCard.Visibility = Visibility.Collapsed;
            RecentItemsListBox.ItemsSource = null;
        }

        // ── Per-mode logic ─────────────────────────────────────────────────
        SearchTextBox.Text  = string.Empty;
        SearchTextBox.IsEnabled = true;

        if (RadioBeneficiaryId.IsChecked == true)
        {
            SearchLabel.Text = "Enter Beneficiary ID:";
            SearchInputPanel.Visibility = Visibility.Visible;
            ShowRecentPanel("Recent Beneficiary IDs", _recentBeneficiaryIds);
            FocusSearchBox();
        }
        else if (RadioFullName.IsChecked == true)
        {
            SearchLabel.Text = "Enter Full Name:";
            SearchInputPanel.Visibility = Visibility.Visible;
            ShowRecentPanel("Recent Names", _recentFullNames);
            FocusSearchBox();
        }

        else if (RadioBarangay.IsChecked == true)
        {
            SearchLabel.Text = "Enter Barangay:";
            SearchInputPanel.Visibility = Visibility.Visible;
            ShowRecentPanel("Recent Barangays", _recentBarangays);
            FocusSearchBox();
        }
        else if (RadioHouseholdNo.IsChecked == true)
        {
            SearchLabel.Text = "Enter Household No:";
            SearchInputPanel.Visibility = Visibility.Visible;
            ShowRecentPanel("Recent Household Nos", _recentHouseholdNos);
            FocusSearchBox();
        }
        else if (RadioOffice.IsChecked == true)
        {
            SearchLabel.Text = "Enter Office:";
            SearchInputPanel.Visibility = Visibility.Visible;
            ShowRecentPanel("Recent Offices", _recentOfficeCodes);
            FocusSearchBox();
        }
        else // View All
        {
            SearchInputPanel.Visibility = Visibility.Collapsed;
            HideRecentPanel();
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
        else if (RadioOffice.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Please enter an Office.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SearchMode = "OfficeCode";
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
        // If we're setting text programmatically (e.g. from a suggestion), skip debounce
        if (_suppressTextChanged) return;

        // Reset and restart the debounce timer on each keystroke
        _debounceTimer.Stop();

        string query = SearchTextBox.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        _debounceTimer.Start();
    }

    /// <summary>
    /// Fired by the debounce timer ~300 ms after the user stops typing.
    /// Runs the filter and updates the suggestion popup.
    /// </summary>
    private void DebounceTimer_Tick(object sender, System.EventArgs e)
    {
        _debounceTimer.Stop();
        ApplyFilter();
    }

    /// <summary>
    /// Filters the appropriate list against the current TextBox text and
    /// shows/hides the suggestion popup (including a "No matches found" state).
    /// </summary>
    private void ApplyFilter()
    {
        // Guard: only run when a searchable radio is checked
        if (RadioFullName == null || RadioBeneficiaryId == null ||
            RadioBarangay == null || RadioHouseholdNo == null || RadioOffice == null)
            return;

        bool anySearchMode = RadioFullName.IsChecked == true
                          || RadioBeneficiaryId.IsChecked == true
                          || RadioBarangay.IsChecked == true
                          || RadioHouseholdNo.IsChecked == true
                          || RadioOffice.IsChecked == true;

        if (!anySearchMode)
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
            matches = _allNames
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();
        else if (RadioBeneficiaryId.IsChecked == true)
            matches = _allBeneficiaryIds
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();

        else if (RadioBarangay.IsChecked == true)
            matches = _allBarangays
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();
        else if (RadioOffice.IsChecked == true)
            matches = _allOfficeCodes
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();
        else // RadioHouseholdNo
            matches = _allHouseholdNos
                .Where(n => n.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();

        if (matches.Count > 0)
        {
            SuggestionsListBox.ItemsSource = matches;
            SuggestionsListBox.Visibility = Visibility.Visible;
            NoMatchesText.Visibility = Visibility.Collapsed;
        }
        else
        {
            SuggestionsListBox.ItemsSource = null;
            SuggestionsListBox.Visibility = Visibility.Collapsed;
            NoMatchesText.Visibility = Visibility.Visible;
        }

        SuggestionsPopup.IsOpen = true;
    }

    private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuggestionsListBox.SelectedItem is string selected)
        {
            // Populate the TextBox without re-triggering the debounce timer
            _suppressTextChanged = true;
            SearchTextBox.Text = selected;
            SearchTextBox.CaretIndex = selected.Length;
            _suppressTextChanged = false;

            SuggestionsPopup.IsOpen = false;
            _debounceTimer.Stop();

            // Auto-submit: run the same logic as clicking Proceed
            ProceedButton_Click(this, new RoutedEventArgs());
        }
    }

    /// <summary>
    /// Allows pressing Enter in the search TextBox to trigger the search,
    /// and Escape to dismiss the suggestion popup.
    /// </summary>
    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Close popup and stop any pending debounce, then run search
            SuggestionsPopup.IsOpen = false;
            _debounceTimer.Stop();
            ProceedButton_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SuggestionsPopup.IsOpen = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Down && SuggestionsPopup.IsOpen
                 && SuggestionsListBox.Items.Count > 0)
        {
            // Allow keyboard navigation into the suggestion list
            SuggestionsListBox.Focus();
            SuggestionsListBox.SelectedIndex = 0;
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Unified handler for the single RecentItemsListBox.
    /// Fills SearchTextBox with the clicked value and immediately triggers
    /// the search (same as pressing Enter / Proceed).
    /// </summary>
    private void RecentItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentItemsListBox.SelectedItem is not string selected)
            return;

        // Suppress the TextChanged debounce so the popup doesn't flicker open
        _suppressTextChanged = true;
        SearchTextBox.Text = selected;
        SearchTextBox.CaretIndex = selected.Length;
        _suppressTextChanged = false;

        SuggestionsPopup.IsOpen = false;
        _debounceTimer.Stop();

        // Auto-submit — same logic as clicking Proceed
        ProceedButton_Click(this, new RoutedEventArgs());
    }
}
