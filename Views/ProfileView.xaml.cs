using System.Windows;
using System.Windows.Controls;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();

        // Auto-clear all password boxes after a successful save
        DataContextChanged += (_, _) => HookViewModel();
    }

    private ProfileViewModel? _vm;

    private void HookViewModel()
    {
        if (_vm != null)
            _vm.PropertyChanged -= Vm_PropertyChanged;

        _vm = DataContext as ProfileViewModel;

        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When OldPassword is cleared (after save/reset), clear all boxes
        if (e.PropertyName == nameof(ProfileViewModel.OldPassword) &&
            string.IsNullOrEmpty(_vm?.OldPassword))
        {
            OldPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }

    private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileViewModel vm)
            vm.OldPassword = ((PasswordBox)sender).Password;
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileViewModel vm)
            vm.NewPassword = ((PasswordBox)sender).Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileViewModel vm)
            vm.ConfirmPassword = ((PasswordBox)sender).Password;
    }
}
