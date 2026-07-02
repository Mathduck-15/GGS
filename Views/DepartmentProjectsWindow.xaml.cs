using System.Windows;

namespace GoodGovernanceApp.Views;

public partial class DepartmentProjectsWindow : Window
{
    public DepartmentProjectsWindow(object dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
