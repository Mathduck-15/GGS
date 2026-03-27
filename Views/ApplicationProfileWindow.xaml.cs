using System.Windows;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class ApplicationProfileWindow : Window
{
    public ApplicationProfileWindow()
    {
        InitializeComponent();
        DataContext = new ApplicationProfileViewModel();
    }
}
