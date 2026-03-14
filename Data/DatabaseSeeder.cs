using System;
using System.Linq;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;

namespace GoodGovernanceApp.Data;

public static class DatabaseSeeder
{
    public static void SeedData(AppDbContext context)
    {
        // 1. Seed Users
        var usersToSeed = new[]
        {
            new User { Name = "superadmin", Email = "superadmin@ggs.local", Password = PasswordHasher.HashPassword("admin"), Role = "SuperAdmin", Status = "active" },
            new User { Name = "admin", Email = "admin@ggs.local", Password = PasswordHasher.HashPassword("admin"), Role = "Admin", Status = "active" },
            new User { Name = "evaluator", Email = "evaluator@ggs.local", Password = PasswordHasher.HashPassword("password"), Role = "Evaluator", Status = "active" },
            new User { Name = "eng_head", Email = "eng_head@ggs.local", Password = PasswordHasher.HashPassword("password"), Role = "User", Status = "active" },
            new User { Name = "eng_sec", Email = "eng_sec@ggs.local", Password = PasswordHasher.HashPassword("password"), Role = "User", Status = "active" },
            new User { Name = "eng_worker1", Email = "eng_worker@ggs.local", Password = PasswordHasher.HashPassword("password"), Role = "User", Status = "active" }
        };

        foreach (var u in usersToSeed)
        {
            if (!context.Users.Any(dbU => dbU.Name == u.Name))
                context.Users.Add(u);
        }
        context.SaveChanges();

        // 2. Seed Parameters if empty
        if (!context.Parameters.Any())
        {
            var parameters = new[]
            {
                new Parameter { Name = "AppName", Value = "Good Governance System", Description = "System Name" },
                new Parameter { Name = "Version", Value = "1.0", Description = "Current Release" },
                new Parameter { Name = "MaxLoginAttempts", Value = "5", Description = "Maximum failed logins" },
                new Parameter { Name = "Currency", Value = "PHP", Description = "System currency" },
                new Parameter { Name = "FiscalYearStart", Value = "Jan", Description = "Start of fiscal year" }
            };
            context.Parameters.AddRange(parameters);
            context.SaveChanges();
        }

        // 3. Seed Categories if empty
        if (!context.Categories.Any())
        {
            var categories = new[]
            {
                new Category { Name = "Infrastructure", Description = "Roads, Bridges, Buildings" },
                new Category { Name = "Education", Description = "Schools, Supplies" },
                new Category { Name = "Health", Description = "Hospitals, Clinics, Medicine" },
                new Category { Name = "Agriculture", Description = "Farming, Seeds, Tractors" },
                new Category { Name = "Social Services", Description = "Welfare, Support Programs" }
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();
        }

        // 4. Seed Budgets if empty
        if (!context.Budgets.Any())
        {
            var categories = context.Categories.ToList();
            var budgets = new[]
            {
                new Budget { CategoryId = categories[0].Id, Year = DateTime.Now.Year, Amount = 5000000m },
                new Budget { CategoryId = categories[1].Id, Year = DateTime.Now.Year, Amount = 3000000m },
                new Budget { CategoryId = categories[2].Id, Year = DateTime.Now.Year, Amount = 4000000m },
                new Budget { CategoryId = categories[3].Id, Year = DateTime.Now.Year, Amount = 2000000m },
                new Budget { CategoryId = categories[4].Id, Year = DateTime.Now.Year, Amount = 1500000m }
            };
            context.Budgets.AddRange(budgets);
            context.SaveChanges();
        }

        // 5. Seed Transactions
        if (context.Transactions.Count() < 10)
        {
            var users = context.Users.ToList();
            var categories = context.Categories.ToList();
            var random = new Random();
            string[] types = { "Expense", "Income" };
            string[] descriptions = 
            { 
                "Road Repair", "New Desks for School", "Hospital Supplies", "Tractor Fuel", "Food Aid Distribution",
                "Clinic Renovation", "Bridge Construction", "Teacher Training", "Agricultural Grant", "Community Center Build"
            };

            for (int i = 1; i <= 30; i++)
            {
                context.Transactions.Add(new Transaction
                {
                    CategoryId = categories[random.Next(categories.Count)].Id,
                    UserId = (int)users[random.Next(users.Count)].Id,
                    Amount = random.Next(1000, 50000),
                    Date = DateTime.Now.AddDays(-random.Next(1, 365)),
                    TransactionType = types[random.Next(types.Length)],
                    Description = $"{descriptions[random.Next(descriptions.Length)]} #{i}"
                });
            }
            context.SaveChanges();
        }

        // 6. Seed System Logs
        var adminUser = context.Users.FirstOrDefault(u => u.Role == "SuperAdmin" || u.Role == "Admin");
        if (adminUser != null)
        {
            context.SystemLogs.Add(new SystemLog
            {
                UserId = (int)adminUser.Id,
                Timestamp = DateTime.Now,
                Action = "System Initialization",
                Details = "Database seeded with initial transactions and required parameters."
            });
            context.SaveChanges();
        }
    }
}
