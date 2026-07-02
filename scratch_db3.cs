using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql("Server=127.0.0.1;Port=3306;Database=governance;User=root;Password=root;SslMode=None;AllowPublicKeyRetrieval=True;", new MySqlServerVersion(new Version(8, 0, 31)));
        
        using var dbContext = new AppDbContext(optionsBuilder.Options);

        var projects = await dbContext.ProjectDetails.OrderByDescending(p => p.Id).Take(5).ToListAsync();
        Console.WriteLine($"Found {projects.Count} projects:");
        foreach (var p in projects)
        {
            Console.WriteLine($" - ID: {p.Id}, PID: {p.ProjectDetailsID}, ContactPerson: '{p.ContactPerson}'");
        }

        var txns = await dbContext.Transactions.OrderByDescending(t => t.Id).Take(5).ToListAsync();
        Console.WriteLine($"Found {txns.Count} transactions:");
        foreach (var t in txns)
        {
            Console.WriteLine($" - ID: {t.Id}, PCode: {t.ProjectCode}, Amount: {t.Amount}, Date: {t.Date}");
        }
    }
}
