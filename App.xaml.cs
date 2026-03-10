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
                config.SetBasePath(Directory.GetCurrentDirectory());
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
                    if (useRemote)
                    {
                        string connStr = config.GetConnectionString("RemoteConnection") ?? "";
                        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
                    }
                    else
                    { //  ?? "Server=____;Database=____;User=___;Password=___;"
                        string connStr = config.GetConnectionString("LocalConnection");
                        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
                    }
                });

                // Register Services
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

        // Ensure database is created and migrations are applied
        using (var scope = AppHost.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
            
            // Seed 30 Dummy records if empty
            DatabaseSeeder.SeedData(dbContext);
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

