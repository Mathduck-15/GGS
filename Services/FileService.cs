using System;
using System.IO;
using System.Threading.Tasks;

namespace GoodGovernanceApp.Services;

public class FileService
{
    private readonly string _uploadDirectory;

    public FileService()
    {
        // Store files in an 'Uploads' folder in the application execution directory
        _uploadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
        
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<string> SaveFileAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Source file not found", sourceFilePath);

        string fileName = Path.GetFileName(sourceFilePath);
        string fileExtension = Path.GetExtension(sourceFilePath);
        string uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{fileExtension}";
        string destinationPath = Path.Combine(_uploadDirectory, uniqueFileName);

        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
        using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        return destinationPath;
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
