using System;
using System.Linq;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;

namespace GoodGovernanceApp.Data;

public static class DatabaseSeeder
{
    public static void SeedData(AppDbContext context)
    {
        // Check if the superadmin user already exists
        if (!context.Users.Any(u => u.Name == "superadmin"))
        {
            var superAdmin = new User
            {
                Name = "superadmin",
                Email = "superadmin@ggms.local",
                Password = PasswordHasher.HashPassword("password"),
                Role = "super_admin",
                Status = "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Users.Add(superAdmin);
            context.SaveChanges();
        }
    }
}
