using System.Windows;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Temporarily, we create MainViewModel directly. We will register it in DI.
        DataContext = new MainViewModel();
    }
}