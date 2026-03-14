using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Views;
using System.Globalization;
using System.Threading;

namespace GoodGovernanceApp;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        // Set global culture to Philippines (en-PH) for currency (₱)
        var culture = new CultureInfo("en-PH");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Ensure WPF UI elements respect this culture
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Retrieve Configuration
                IConfiguration config = context.Configuration;
                bool useRemote = config.GetValue<bool>("AppSettings:UseRemoteDatabase");

                // Configure DbContext based on selection
                services.AddDbContext<AppDbContext>(options =>
                {
                    string connStr = useRemote 
                        ? (config.GetConnectionString("RemoteConnection") ?? "")
                        : (config.GetConnectionString("LocalConnection") ?? "");

                    if (string.IsNullOrEmpty(connStr) || connStr.Contains("YOUR_HOSTINGER_IP"))
                    {
                        // Use a default version if it's a placeholder to avoid pre-startup crash
                        options.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 31)),
                            mysqlOptions => mysqlOptions.EnableRetryOnFailure());
                    }
                    else
                    {
                        try
                        {
                            options.UseMySql(connStr, ServerVersion.AutoDetect(connStr),
                                mysqlOptions => mysqlOptions.EnableRetryOnFailure());
                        }
                        catch
                        {
                            // Fallback to a common version if AutoDetect fails due to network/firewall
                            options.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 31)),
                                mysqlOptions => mysqlOptions.EnableRetryOnFailure());
                        }
                    }
                });

                // Register Services
                services.AddSingleton<DatabaseHelper>();
                services.AddSingleton<GoodGovernanceApp.Services.ValidationService>();
                services.AddSingleton<GoodGovernanceApp.Services.SessionService>();
                services.AddSingleton<GoodGovernanceApp.Services.FileService>();
                services.AddSingleton<GoodGovernanceApp.Services.BackupService>();

                // Register Views
                services.AddTransient<MainWindow>();
                services.AddTransient<GoodGovernanceApp.Views.LoginWindow>();
                services.AddTransient<GoodGovernanceApp.ViewModels.LoginViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        // Connect to existing database and seed if needed
        using (var scope = AppHost.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();
                
                // Validate if connection works
                if (!dbContext.Database.CanConnect())
                {
                    throw new Exception("Could not establish a connection to the database.");
                }

                try { dbContext.Database.EnsureCreated(); } catch { /* tables may already exist */ }
                
                // Patch: Ensure the office_code column exists since EF migrations are out of sync with EnsureCreated
                try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_offices ADD COLUMN office_code VARCHAR(30) NULL AFTER name;"); } catch { }
                
                // Patch: Ensure the active column has a default value so inserts don't fail
                try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_offices ALTER COLUMN active SET DEFAULT 1;"); } catch { }

                // Patch: Ensure officeallocations table exists and has the correct schema (no SpentAmount)
                try {
                    // Force refresh by disabling FK checks during drop/create
                    dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 0;");
                    dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS officeallocations;");
                    dbContext.Database.ExecuteSqlRaw(@"
                        CREATE TABLE officeallocations (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            YearlyBudgetId INT NOT NULL,
                            office_code VARCHAR(30) NOT NULL,
                            AllocatedAmount DECIMAL(65,30) NOT NULL,
                            CONSTRAINT FK_officeallocations_YearlyBudgets FOREIGN KEY (YearlyBudgetId) REFERENCES YearlyBudgets (Id) ON DELETE CASCADE,
                            CONSTRAINT FK_officeallocations_tbl_offices FOREIGN KEY (office_code) REFERENCES tbl_offices (office_code) ON DELETE CASCADE
                        ) CHARACTER SET=utf8mb4;
                    ");
                    dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;");
                } catch { 
                    try { dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;"); } catch {}
                }
                
                // Seed data if empty
                try { DatabaseSeeder.SeedData(dbContext); } catch { }
            }
            catch (Exception ex)
            {
                var config = services.GetRequiredService<IConfiguration>();
                bool isRemote = config.GetValue<bool>("AppSettings:UseRemoteDatabase");

                string errorMsg = $"Database Connection Failed!\n\nDetails: {ex.Message}";
                if (isRemote)
                {
                    errorMsg += "\n\nTroubleshooting for Hostinger:\n" +
                                "1. Check appsettings.json for correct IP, User, and Password.\n" +
                                "2. ADD YOUR CURRENT IP to 'Remote MySQL' in Hostinger hPanel.\n" +
                                "3. Ensure you aren't using placeholders (YOUR_HOSTINGER_IP).";
                }

                MessageBox.Show(errorMsg, "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Optional: Decide if app should continue or close. 
                // For now, we allow it to reach Login so the user can see the error.
            }
        }

        var loginWindow = AppHost.Services.GetRequiredService<GoodGovernanceApp.Views.LoginWindow>();
        loginWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        AppHost.Dispose();

        base.OnExit(e);
    }
}

