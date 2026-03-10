using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GoodGovernanceApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Parameter> Parameters { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<SystemLog> SystemLogs { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<DepartmentRole> DepartmentRoles { get; set; } = null!;
    public DbSet<UploadedFile> UploadedFiles { get; set; } = null!;
    public DbSet<YearlyBudget> YearlyBudgets { get; set; } = null!;
    public DbSet<DepartmentAllocation> DepartmentAllocations { get; set; } = null!;
    public DbSet<Evaluation> Evaluations { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Define relations if necessary
        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Category)
            .WithMany(c => c.Budgets)
            .HasForeignKey(b => b.CategoryId);
            
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId);
            
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<SystemLog>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DepartmentRole>()
            .HasOne(dr => dr.Department)
            .WithMany(d => d.Roles)
            .HasForeignKey(dr => dr.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UploadedFile>()
            .HasOne(f => f.Department)
            .WithMany()
            .HasForeignKey(f => f.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UploadedFile>()
            .HasOne(f => f.Parameter)
            .WithMany()
            .HasForeignKey(f => f.ParameterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DepartmentAllocation>()
            .HasOne(da => da.YearlyBudget)
            .WithMany(yb => yb.Allocations)
            .HasForeignKey(da => da.YearlyBudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DepartmentAllocation>()
            .HasOne(da => da.Department)
            .WithMany()
            .HasForeignKey(da => da.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Evaluation>()
            .HasOne(e => e.UploadedFile)
            .WithMany()
            .HasForeignKey(e => e.UploadedFileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Evaluation>()
            .HasOne(e => e.Evaluator)
            .WithMany()
            .HasForeignKey(e => e.EvaluatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
