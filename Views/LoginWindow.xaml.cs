using System.Windows;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = ((System.Windows.Controls.PasswordBox)sender).Password;
        }
    }
}
