using System.Windows;

namespace GoodGovernanceApp.Views;

public partial class DatabaseSettingsWindow : Window
{
    public DatabaseSettingsWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // The ViewModel handles the saving, we just provide a way to close the window
        // We could also check if save was successful before closing, but for now we close.
        DialogResult = true;
        Close();
    }
}
