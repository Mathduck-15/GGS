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

        string beneficiaryId = "BEN-2026-700016595-1";
        
        try
        {
            var projectDetailsQuery = dbContext.ProjectDetails
                .Where(pd => pd.ContactPerson == beneficiaryId && pd.ProjectDetailsID != null)
                .Select(pd => pd.ProjectDetailsID);
            
            var projectCodes = await projectDetailsQuery.ToListAsync();
            Console.WriteLine($"Found {projectCodes.Count} project codes for {beneficiaryId}: " + string.Join(", ", projectCodes));

            var departmentTransactionsQuery = dbContext.Transactions
                .Where(t => projectCodes.Contains(t.ProjectCode));
            
            var departmentTransactions = await departmentTransactionsQuery.ToListAsync();
            Console.WriteLine($"Found {departmentTransactions.Count} department transactions!");
            foreach(var t in departmentTransactions)
            {
                Console.WriteLine($"Transaction: {t.ProjectCode}, Amount: {t.Amount}, Type: {t.TransactionType}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.ToString());
        }
    }
}
