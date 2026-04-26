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
    public static IConfiguration Config { get; set; } = null!;

    public App()
    {
        // Set global culture to Philippines (en-PH) for currency (₱)
        var culture = new CultureInfo("en-PH");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Clear default sources to avoid loading appsettings.json from wrong directory
                config.Sources.Clear();
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure DbContext — uses DatabaseConfig (single source of truth)
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseMySql(DatabaseConfig.ConnectionString, new MySqlServerVersion(new Version(8, 0, 31)),
                        mysqlOptions => mysqlOptions.EnableRetryOnFailure());
                }, ServiceLifetime.Transient, ServiceLifetime.Transient);

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

        // Set Config to the host's IConfiguration so every class uses the SAME instance
        Config = AppHost.Services.GetRequiredService<IConfiguration>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        // Show the login window immediately — never block the UI thread for DB work
        var loginWindow = AppHost.Services.GetRequiredService<GoodGovernanceApp.Views.LoginWindow>();
        loginWindow.Show();

        base.OnStartup(e);

        // Run all DB initialisation off the UI thread so a slow/unreachable
        // remote server can never freeze or prevent the window from appearing.
        await Task.Run(async () =>
        {
            using var scope = AppHost.Services.CreateScope();
            var services   = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();

                Exception? connError = null;
                bool canConn = await Task.Run(() =>
                {
                    try   { return dbContext.Database.CanConnect(); }
                    catch (Exception ex) { connError = ex; return false; }
                });

                if (!canConn)
                {
                    string dbMode = Config["AppSettings:DatabaseMode"] ?? "Local";
                    string errorMsg = "Database Connection Failed!\n\nThe application could not reach the database. " +
                                      "Check your connection settings (Settings → Database).";

                    if (connError != null)
                    {
                        string realErr = connError.InnerException?.Message ?? connError.Message;
                        errorMsg += $"\n\nError Details:\n{realErr}";
                        
                        try 
                        { 
                            System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_error_log.txt"), 
                                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Startup Connection Error:\n{connError}\n\n"); 
                        } catch { }
                    }

                    if (dbMode == "Remote")
                        errorMsg += "\n\nFor Hostinger remote:\n" +
                                    "1. Add your current IP to 'Remote MySQL' in Hostinger hPanel.\n" +
                                    "2. Verify the server/user/password in Settings.";
                    else if (dbMode == "LAN")
                        errorMsg += "\n\nFor LAN:\n" +
                                    "1. Ensure you are on the same network.\n" +
                                    "2. Verify the LAN server IP in Settings.";

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        System.Windows.MessageBox.Show(errorMsg, "Database Warning",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning));
                    return;
                }

                // Non-critical patches — ignore failures silently
                try { await Task.Run(() => dbContext.Database.EnsureCreated()); } catch { }

                try
                {
                    await Task.Run(() =>
                    {
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_offices ADD COLUMN office_code VARCHAR(30) NULL AFTER name;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_offices ALTER COLUMN active SET DEFAULT 1;"); } catch { }
                        
                        // Patch VoucherCode
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE project_details ADD COLUMN voucher_code VARCHAR(10) NULL;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE transactions ADD COLUMN voucher_code VARCHAR(10) NULL;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_transaction ADD COLUMN voucher_code VARCHAR(10) NULL;"); } catch { }

                        try
                        {
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
                        }
                        catch
                        {
                            try { dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;"); } catch { }
                        }
                    });
                }
                catch { }

                try { await Task.Run(() => DatabaseSeeder.SeedData(dbContext)); } catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App Startup] DB init error: {ex.Message}");
            }
        });
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        AppHost.Dispose();
        base.OnExit(e);
    }
}