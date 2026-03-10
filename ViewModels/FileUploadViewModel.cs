using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace GoodGovernanceApp.ViewModels;

public class FileUploadViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly FileService _fileService;

    private Department? _selectedDepartment;
    private Parameter? _selectedParameter;
    private string _selectedFilePath = string.Empty;
    private string _fileName = string.Empty;

    public ObservableCollection<Department> Departments { get; } = new();
    public ObservableCollection<Parameter> Parameters { get; } = new();
    public ObservableCollection<UploadedFile> UploadedFiles { get; } = new();

    public Department? SelectedDepartment
    {
        get => _selectedDepartment;
        set { _selectedDepartment = value; OnPropertyChanged(); }
    }

    public Parameter? SelectedParameter
    {
        get => _selectedParameter;
        set { _selectedParameter = value; OnPropertyChanged(); }
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set 
        { 
            _selectedFilePath = value; 
            OnPropertyChanged(); 
            FileName = Path.GetFileName(value);
        }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public ICommand SelectFileCommand { get; }
    public ICommand UploadFileCommand { get; }
    public ICommand DeleteFileCommand { get; }

    public FileUploadViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        _fileService = new FileService(); // Simple instantiation for now

        SelectFileCommand = new RelayCommand(_ => ExecuteSelectFile());
        UploadFileCommand = new RelayCommand(async _ => await ExecuteUploadFile(), _ => CanUpload());
        DeleteFileCommand = new RelayCommand(async row => await ExecuteDeleteFile(row));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var depts = await _context.Departments.ToListAsync();
            foreach (var d in depts) Departments.Add(d);

            var @params = await _context.Parameters.ToListAsync();
            foreach (var p in @params) Parameters.Add(p);

            await LoadUploadedFilesAsync();
        }
        catch { }
    }

    private async Task LoadUploadedFilesAsync()
    {
        UploadedFiles.Clear();
        var files = await _context.UploadedFiles
            .Include(f => f.Department)
            .Include(f => f.Parameter)
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();
            
        foreach (var f in files) UploadedFiles.Add(f);
    }

    private void ExecuteSelectFile()
    {
        var dialog = new OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
        }
    }

    private bool CanUpload()
    {
        return !string.IsNullOrWhiteSpace(SelectedFilePath) && 
               SelectedDepartment != null && 
               SelectedParameter != null;
    }

    private async Task ExecuteUploadFile()
    {
        try
        {
            string storedPath = await _fileService.SaveFileAsync(SelectedFilePath);
            var fileInfo = new FileInfo(SelectedFilePath);

            var uploadedFile = new UploadedFile
            {
                FileName = FileName,
                StoragePath = storedPath,
                FileType = fileInfo.Extension,
                FileSize = fileInfo.Length,
                DepartmentId = SelectedDepartment!.Id,
                ParameterId = SelectedParameter!.Id,
                UploadDate = DateTime.Now
            };

            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            UploadedFiles.Insert(0, uploadedFile);
            
            // Clear selection
            SelectedFilePath = string.Empty;
            FileName = string.Empty;
            
            MessageBox.Show("File uploaded successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Upload failed: {ex.Message}");
        }
    }

    private async Task ExecuteDeleteFile(object? parameter)
    {
        if (parameter is UploadedFile file)
        {
            var result = MessageBox.Show($"Are you sure you want to delete {file.FileName}?", "Confirm Delete", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _fileService.DeleteFile(file.StoragePath);
                _context.UploadedFiles.Remove(file);
                await _context.SaveChangesAsync();
                UploadedFiles.Remove(file);
            }
        }
    }
}
