using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GoodGovernanceApp.Data;

/// <summary>
/// SQLite EF Core context — ALWAYS used for normal app reads/writes.
/// Hostinger MySQL is ONLY used by SyncService for background sync.
/// </summary>
public class LocalDbContext : DbContext
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
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<SystemLog> SystemLogs { get; set; } = null!;
    public DbSet<YearlyBudget> YearlyBudgets { get; set; } = null!;
    public DbSet<OfficeAllocation> OfficeAllocations { get; set; } = null!;
    public DbSet<ProjectDetail> ProjectDetails { get; set; } = null!;
    public DbSet<GoveProfile> GoveProfiles { get; set; } = null!;
    public DbSet<SystemsProfile> SystemsProfiles { get; set; } = null!;

    // ── Cross-System Cache (read-only from other systems) ─────────────────────
    public DbSet<CrsBeneficiaryCache> CrsBeneficiaryCache { get; set; } = null!;

    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Users ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasDefaultValue("user");
            entity.Property(u => u.Status).HasDefaultValue("active");
            entity.HasIndex(u => u.SyncId).IsUnique();
        });

        // ── Transaction ───────────────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("Id");
            entity.Property(t => t.ProjectCode).HasColumnName("project_code").HasMaxLength(45);
            entity.Property(t => t.Amount).HasColumnName("Amount");
            entity.Property(t => t.VoucherCode).HasColumnName("voucher_code").HasMaxLength(10);
            entity.Property(t => t.TransactionType).HasColumnName("transaction_type").HasMaxLength(45);
            entity.Property(t => t.Date).HasColumnName("date");
        });

        // ── ValidateUsers ─────────────────────────────────────────────────────
        modelBuilder.Entity<ValidateUser>(entity =>
        {
            entity.ToTable("validate_users");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Status).HasDefaultValue("pending");
            entity.HasOne(v => v.Category).WithMany().HasForeignKey(v => v.CategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(v => v.AppUser).WithOne(u => u.ValidationInfo).HasForeignKey<ValidateUser>(v => v.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(v => v.SyncId).IsUnique();
        });

        // ── AuditTrails ───────────────────────────────────────────────────────
        modelBuilder.Entity<AuditTrail>(entity =>
        {
            entity.ToTable("audit_trails");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.SyncId).IsUnique();
        });

        // ── Offices ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Office>(entity =>
        {
            entity.ToTable("tbl_offices");
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OfficeCode).IsUnique();
            entity.HasIndex(o => o.SyncId).IsUnique();
        });

        // ── Master Budget ─────────────────────────────────────────────────────
        modelBuilder.Entity<MasterBudget>(entity =>
        {
            entity.ToTable("master_budget");
            entity.HasKey(m => m.Id);
            entity.HasOne(m => m.CreatedBy).WithMany().HasForeignKey(m => m.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(m => m.SyncId).IsUnique();
        });

        // ── Budget Allocations ────────────────────────────────────────────────
        modelBuilder.Entity<BudgetAllocation>(entity =>
        {
            entity.ToTable("budget_allocations");
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.MasterBudget).WithMany(m => m.Allocations).HasForeignKey(b => b.MasterBudgetId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(b => b.AllocatedBy).WithMany().HasForeignKey(b => b.AllocatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.Office).WithMany().HasForeignKey(b => b.OfficeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(b => b.SyncId).IsUnique();
        });

        // ── Program Provision ─────────────────────────────────────────────────
        modelBuilder.Entity<ProgramProvision>(entity =>
        {
            entity.ToTable("tbl_program_provision");
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Office).WithMany().HasForeignKey(p => p.OfficeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(p => p.SyncId).IsUnique();
        });

        // ── Tbl Transaction ───────────────────────────────────────────────────
        modelBuilder.Entity<TblTransaction>(entity =>
        {
            entity.ToTable("tbl_transaction");
            entity.HasKey(t => t.Id);
            entity.HasOne(t => t.Program).WithMany().HasForeignKey(t => t.ProgramId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.BudgetAllocation).WithMany().HasForeignKey(t => t.BudgetAllocationId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.DistributedBy).WithMany().HasForeignKey(t => t.DistributedById).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Office).WithMany().HasForeignKey(t => t.OfficeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Service).WithMany().HasForeignKey(t => t.ServicesId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(t => t.SyncId).IsUnique();
        });

        // ── Services & Requests ───────────────────────────────────────────────
        modelBuilder.Entity<TblService>(entity =>
        {
            entity.ToTable("tbl_services");
            entity.HasKey(s => s.ServicesId);
            entity.HasIndex(s => s.SyncId).IsUnique();
        });

        // ── Project Details ───────────────────────────────────────────────────
        modelBuilder.Entity<ProjectDetail>(entity =>
        {
            entity.ToTable("project_details");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.SyncId).IsUnique();
        });

        // ── Legacy Retained Relations ─────────────────────────────────────────
        modelBuilder.Entity<Parameter>().ToTable("parameters");
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<YearlyBudget>().ToTable("yearlybudgets");
        modelBuilder.Entity<Budget>().HasOne(b => b.Category).WithMany(c => c.Budgets).HasForeignKey(b => b.CategoryId);
        modelBuilder.Entity<Budget>().HasOne(b => b.Office).WithMany().HasForeignKey(b => b.OfficeId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SystemLog>().HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<User>().HasOne(u => u.Office).WithMany(o => o.Users).HasForeignKey(u => u.OfficeId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DepartmentRole>().HasOne(dr => dr.Office).WithMany(o => o.Roles).HasForeignKey(dr => dr.OfficeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UploadedFile>().HasOne(f => f.Office).WithMany().HasForeignKey(f => f.OfficeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UploadedFile>().HasOne(f => f.Parameter).WithMany().HasForeignKey(f => f.ParameterId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Parameter>().HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OfficeAllocation>().HasOne(da => da.YearlyBudget).WithMany(yb => yb.Allocations).HasForeignKey(da => da.YearlyBudgetId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OfficeAllocation>().HasOne(da => da.Office).WithMany().HasPrincipalKey(o => o.OfficeCode).HasForeignKey(da => da.OfficeCode).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Evaluation>().HasOne(e => e.UploadedFile).WithMany().HasForeignKey(e => e.UploadedFileId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Evaluation>().HasOne(e => e.Evaluator).WithMany().HasForeignKey(e => e.EvaluatorId).OnDelete(DeleteBehavior.Cascade);

        // ── Profiles ──────────────────────────────────────────────────────────
        modelBuilder.Entity<GoveProfile>(entity =>
        {
            entity.ToTable("goveprofile");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.GoveName).HasMaxLength(255);
            entity.Property(g => g.Address).HasMaxLength(255);
            entity.Property(g => g.LogoAddress).HasMaxLength(500);
        });

        modelBuilder.Entity<SystemsProfile>(entity =>
        {
            entity.ToTable("systemsprofile");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.PhotoAddress).HasMaxLength(500);
        });

        // ── CRS Beneficiary Cache ─────────────────────────────────────────────
        modelBuilder.Entity<CrsBeneficiaryCache>(entity =>
        {
            entity.ToTable("crs_beneficiary_cache");
            entity.HasKey(c => c.BeneficiaryCacheId);
        });
    }
}
