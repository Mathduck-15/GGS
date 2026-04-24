using System.Windows;
using System.Windows.Controls;
using GoodGovernanceApp.ViewModels;

namespace GoodGovernanceApp.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();

        // Clear the password box whenever editing mode ends (save or cancel)
        DataContextChanged += (_, _) => HookViewModel();
    }

    private UserManagementViewModel? _vm;

    private void HookViewModel()
    {
        if (_vm != null)
            _vm.PropertyChanged -= Vm_PropertyChanged;

        _vm = DataContext as UserManagementViewModel;

        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When editing ends (IsEditing → false), clear the password box
        if (e.PropertyName == nameof(UserManagementViewModel.IsEditing) &&
            _vm?.IsEditing == false)
        {
            UserPasswordBox.Clear();
        }
    }

    private void UserPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.NewPasswordInput = ((PasswordBox)sender).Password;
    }
}
