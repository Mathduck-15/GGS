using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Views;
using GoodGovernanceApp.Services;
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

        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoodGovernanceApp");
        Directory.CreateDirectory(appDataFolder);
        string appDataSettingsPath = Path.Combine(appDataFolder, "appsettings.json");

        if (!File.Exists(appDataSettingsPath))
        {
            string baseSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(baseSettingsPath))
                File.Copy(baseSettingsPath, appDataSettingsPath);
        }

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.SetBasePath(appDataFolder);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // ── SQLite LocalDbContext — primary context, always available ──
                // All ViewModels use this. Works offline.
                services.AddDbContextFactory<LocalDbContext>(options =>
                {
                    options.UseSqlite(DatabaseConfig.SqliteConnectionString);
                }, ServiceLifetime.Scoped);

                // Transient LocalDbContext for direct injection in ViewModels
                services.AddDbContext<LocalDbContext>(options =>
                {
                    options.UseSqlite(DatabaseConfig.SqliteConnectionString);
                }, ServiceLifetime.Transient, ServiceLifetime.Transient);

                // ── MySQL AppDbContext — used ONLY by SyncService ──────────────
                // NO EnableRetryOnFailure — fails fast so offline fallback works.
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    options.UseMySql(
                        DatabaseConfig.HostingerConnectionString,
                        new MySqlServerVersion(new Version(8, 0, 0)));
                }, ServiceLifetime.Scoped);

                // Transient AppDbContext for design-time / legacy use
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseMySql(
                        DatabaseConfig.HostingerConnectionString,
                        new MySqlServerVersion(new Version(8, 0, 0)));
                }, ServiceLifetime.Transient, ServiceLifetime.Transient);

                // ── Sync Infrastructure ────────────────────────────────────────
                services.AddSingleton<ConnectivityService>();
                services.AddScoped<SyncService>();

                // ── Application Services ───────────────────────────────────────
                services.AddSingleton<DatabaseHelper>();
                services.AddSingleton<GoodGovernanceApp.Services.ValidationService>();
                services.AddSingleton<GoodGovernanceApp.Services.SessionService>();
                services.AddSingleton<GoodGovernanceApp.Services.FileService>();
                services.AddSingleton<GoodGovernanceApp.Services.BackupService>();

                // ── Views ──────────────────────────────────────────────────────
                services.AddTransient<MainWindow>();
                services.AddTransient<GoodGovernanceApp.Views.LoginWindow>();
                services.AddTransient<GoodGovernanceApp.ViewModels.LoginViewModel>();
            })
            .Build();

        Config = AppHost.Services.GetRequiredService<IConfiguration>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        // Ensure app data folders always exist
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GoodGovernanceApp");

        Directory.CreateDirectory(Path.Combine(appData, "Logos"));
        Directory.CreateDirectory(Path.Combine(appData, "ProfilePhotos"));

        // Show the login window immediately — no blocking on DB
        var loginWindow = AppHost.Services.GetRequiredService<GoodGovernanceApp.Views.LoginWindow>();
        loginWindow.Show();

        base.OnStartup(e);

        // Initialise SQLite on a background thread — never blocks the UI
        await Task.Run(async () =>
        {
            try
            {
                // EnsureCreated creates ggms.db + all tables if they don't exist.
                // This is instant on subsequent launches (file already exists).
                using var scope = AppHost.Services.CreateScope();
                var localDb = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                await localDb.Database.EnsureCreatedAsync();

                // Apply any non-breaking patches to SQLite schema
                await ApplySqlitePatchesAsync(localDb);

                // Seed a default admin if the database is completely empty (e.g. running offline on first launch)
                if (!localDb.Users.Any())
                {
                    localDb.Users.Add(new Models.User
                    {
                        Name = "Offline Admin",
                        Email = "admin@ggms.local",
                        Password = GoodGovernanceApp.Utilities.PasswordHasher.HashPassword("admin123"),
                        Role = "super_admin",
                        Status = "active"
                    });
                    await localDb.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App Startup] SQLite init error: {ex.Message}");
            }

            // Start connectivity check + background sync timer
            // This runs regardless of whether SQLite init succeeded
            try
            {
                var connectivity = AppHost.Services.GetRequiredService<ConnectivityService>();
                await connectivity.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App Startup] Connectivity start error: {ex.Message}");
            }
        });
    }

    private static async Task ApplySqlitePatchesAsync(LocalDbContext db)
    {
        // SQLite doesn't support ALTER TABLE … ADD COLUMN with constraints,
        // but EnsureCreated handles the full schema. These patches are for
        // safety only in case of legacy SQLite files from before SyncId was added.
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE project_details ADD COLUMN voucher_code VARCHAR(10) NULL;");
        }
        catch { }

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE tbl_transaction ADD COLUMN voucher_code VARCHAR(10) NULL;");
        }
        catch { }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Stop the background sync timer cleanly
        try
        {
            var connectivity = AppHost!.Services.GetService<ConnectivityService>();
            connectivity?.Stop();
        }
        catch { }

        await AppHost!.StopAsync();
        AppHost.Dispose();
        base.OnExit(e);
    }
}