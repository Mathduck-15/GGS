using System.Windows;
using System.Windows.Controls;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileViewModel viewModel)
        {
            viewModel.NewPassword = ((PasswordBox)sender).Password;
        }
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileViewModel viewModel)
        {
            viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
        }
    }
}
