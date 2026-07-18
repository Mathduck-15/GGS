using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GoodGovernanceApp.Data;

public class CloudDbContext : DbContext
{
    // ── Core ──────────────────────────────────────────────────────────────────
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ValidateUser> ValidateUsers { get; set; } = null!;
    public DbSet<AuditTrail> AuditTrails { get; set; } = null!;
    public DbSet<Office> Offices { get; set; } = null!;

    // ── Budget ────────────────────────────────────────────────────────────────
    public DbSet<MasterBudget> MasterBudgets { get; set; } = null!;
    public DbSet<BudgetAllocation> BudgetAllocations { get; set; } = null!;
    public DbSet<ProgramProvision> ProgramProvisions { get; set; } = null!;

    // ── Transactions ──────────────────────────────────────────────────────────
    public DbSet<ConsolidatedTransactions> ConsolidatedTransactions { get; set; } = null!;
    public DbSet<TblTransaction> TblTransactions { get; set; } = null!;

    // ── People & Organization ─────────────────────────────────────────────────
    public DbSet<DepartmentRole> DepartmentRoles { get; set; } = null!;

    // ── Services & Requests ───────────────────────────────────────────────────
    public DbSet<TblService> TblServices { get; set; } = null!;

    // ── Files & Evaluations ──────────────────────────────────────────────────
    public DbSet<UploadedFile> UploadedFiles { get; set; } = null!;
    public DbSet<Evaluation> Evaluations { get; set; } = null!;

    // ── Legacy / Retained ────────────────────────────────────────────────────
    public DbSet<Parameter> Parameters { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;

    public DbSet<SystemLog> SystemLogs { get; set; } = null!;
    public DbSet<ProjectDetail> ProjectDetails { get; set; } = null!;

    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map models exactly as AppDbContext does, because the schemas are identical.
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
        });



        modelBuilder.Entity<ValidateUser>(entity =>
        {
            entity.ToTable("validate_users");
            entity.HasKey(v => v.Id);
            entity.HasOne(v => v.Category).WithMany().HasForeignKey(v => v.CategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(v => v.AppUser).WithOne(u => u.ValidationInfo).HasForeignKey<ValidateUser>(v => v.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditTrail>().ToTable("audit_trails").HasKey(a => a.Id);
        modelBuilder.Entity<Office>().ToTable("tbl_offices").HasKey(o => o.Id);
        modelBuilder.Entity<MasterBudget>().ToTable("master_budget").HasKey(m => m.Id);
        modelBuilder.Entity<BudgetAllocation>().ToTable("budget_allocations").HasKey(b => b.Id);
        modelBuilder.Entity<ProgramProvision>().ToTable("tbl_program_provision").HasKey(p => p.Id);
        modelBuilder.Entity<TblTransaction>().ToTable("tbl_transaction").HasKey(t => t.Id);
        modelBuilder.Entity<TblService>().ToTable("tbl_services").HasKey(s => s.ServicesId);
        modelBuilder.Entity<ProjectDetail>().ToTable("project_details").HasKey(p => p.Id);
        modelBuilder.Entity<Parameter>().ToTable("parameters");
        modelBuilder.Entity<Category>().ToTable("categories");
    }
}
