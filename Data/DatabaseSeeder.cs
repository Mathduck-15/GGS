using System;
using System.Linq;
using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.Data;

public static class DatabaseSeeder
{
    public static void SeedData(AppDbContext context)
    {
        // 0. Seed Departments
        var standardDepts = new[]
        {
            new Department { Name = "Engineering", Description = "Technical and construction works" },
            new Department { Name = "Health Services", Description = "Medical and clinical operations" },
            new Department { Name = "Finance", Description = "Accounting and budget management" }
        };

        foreach (var d in standardDepts)
        {
            if (!context.Departments.Any(dbD => dbD.Name == d.Name))
                context.Departments.Add(d);
        }
        context.SaveChanges();

        var engineering = context.Departments.FirstOrDefault(d => d.Name == "Engineering");
        var health = context.Departments.FirstOrDefault(d => d.Name == "Health Services");

        // 0.1 Seed DepartmentRoles (only for departments we just ensured exist)
        if (engineering != null)
        {
            var roles = new[] { "Department Head", "Secretary", "Worker" };
            foreach (var r in roles)
            {
                if (!context.DepartmentRoles.Any(dbR => dbR.DepartmentId == engineering.Id && dbR.Name == r))
                    context.DepartmentRoles.Add(new DepartmentRole { Name = r, DepartmentId = engineering.Id });
            }
        }
        if (health != null)
        {
            var roles = new[] { "Department Head", "Secretary" };
            foreach (var r in roles)
            {
                if (!context.DepartmentRoles.Any(dbR => dbR.DepartmentId == health.Id && dbR.Name == r))
                    context.DepartmentRoles.Add(new DepartmentRole { Name = r, DepartmentId = health.Id });
            }
        }
        context.SaveChanges();

        // 1. Seed Users (Check individually)
        var engineeringDept = context.Departments.FirstOrDefault(d => d.Name == "Engineering");
        var engHeadRole = context.DepartmentRoles.FirstOrDefault(r => r.DepartmentId == engineeringDept!.Id && r.Name == "Department Head");
        var engSecRole = context.DepartmentRoles.FirstOrDefault(r => r.DepartmentId == engineeringDept!.Id && r.Name == "Secretary");
        var engWorkerRole = context.DepartmentRoles.FirstOrDefault(r => r.DepartmentId == engineeringDept!.Id && r.Name == "Worker");

        var usersToSeed = new[]
        {
            new User { Username = "superadmin", PasswordHash = "admin", Role = "SuperAdmin", IsActive = true },
            new User { Username = "admin", PasswordHash = "admin", Role = "Admin", IsActive = true },
            new User { Username = "evaluator", PasswordHash = "password", Role = "Evaluator", IsActive = true },
            new User { Username = "eng_head", PasswordHash = "password", Role = "User", IsActive = true, DepartmentId = engineeringDept?.Id, DepartmentRoleId = engHeadRole?.Id },
            new User { Username = "eng_sec", PasswordHash = "password", Role = "User", IsActive = true, DepartmentId = engineeringDept?.Id, DepartmentRoleId = engSecRole?.Id },
            new User { Username = "eng_worker1", PasswordHash = "password", Role = "User", IsActive = true, DepartmentId = engineeringDept?.Id, DepartmentRoleId = engWorkerRole?.Id }
        };

        foreach (var u in usersToSeed)
        {
            if (!context.Users.Any(dbU => dbU.Username == u.Username))
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
                new Parameter { Name = "Currency", Value = "USD", Description = "System currency" },
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

        // 5. Seed Transactions (Only if users and categories exist)
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
                    UserId = users[random.Next(users.Count)].Id,
                    Amount = random.Next(1000, 50000),
                    Date = DateTime.Now.AddDays(-random.Next(1, 365)),
                    TransactionType = types[random.Next(types.Length)],
                    Description = $"{descriptions[random.Next(descriptions.Length)]} #{i}"
                });
            }
            context.SaveChanges();
        }
        context.SaveChanges();

        // 6. Seed System Logs
        var adminUser = context.Users.FirstOrDefault(u => u.Role == "SuperAdmin" || u.Role == "Admin");
        if (adminUser != null)
        {
            context.SystemLogs.Add(new SystemLog
            {
                UserId = adminUser.Id,
                Timestamp = DateTime.Now,
                Action = "System Initialization",
                Details = "Database seeded with initial transactions and required parameters."
            });
            context.SaveChanges();
        }
    }
}
