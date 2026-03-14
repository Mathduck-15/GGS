using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Services;
using GoodGovernanceApp.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class ProfileViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly SessionService _sessionService;
    
    private string _username = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _role = string.Empty;
    private string _officeName = "None";

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string NewPassword
    {
        get => _newPassword;
        set { _newPassword = value; OnPropertyChanged(); }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { _confirmPassword = value; OnPropertyChanged(); }
    }

    public string Role
    {
        get => _role;
        set { _role = value; OnPropertyChanged(); }
    }

    public string OfficeName
    {
        get => _officeName;
        set { _officeName = value; OnPropertyChanged(); }
    }

    // Keep for XAML compatibility
    public string DepartmentName
    {
        get => _officeName;
        set { _officeName = value; OnPropertyChanged(); }
    }

    public ICommand SaveChangesCommand { get; }

    public ProfileViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        _sessionService = App.AppHost!.Services.GetRequiredService<SessionService>();

        SaveChangesCommand = new RelayCommand(async _ => await ExecuteSaveChanges(), _ => CanSave());

        LoadUserData();
    }

    private void LoadUserData()
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser != null)
        {
            Username = currentUser.Name;
            Role = currentUser.Role;
            OfficeName = currentUser.Office?.Name ?? "General / Unassigned";
        }
    }

    private bool CanSave()
    {
        if (string.IsNullOrWhiteSpace(Username)) return false;
        if (!string.IsNullOrEmpty(NewPassword) && NewPassword != ConfirmPassword) return false;
        return true;
    }

    private async Task ExecuteSaveChanges()
    {
        try
        {
            var sessionUser = _sessionService.CurrentUser;
            if (sessionUser == null) return;

            var user = await _context.Users.FindAsync(sessionUser.Id);
            if (user == null)
            {
                MessageBox.Show("User not found in database.");
                return;
            }

            user.Name = Username;
            if (!string.IsNullOrEmpty(NewPassword))
            {
                user.Password = PasswordHasher.HashPassword(NewPassword);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Update session
            sessionUser.Name = Username;
            
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            
            MessageBox.Show("Profile updated successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed: {ex.Message}");
        }
    }
}
