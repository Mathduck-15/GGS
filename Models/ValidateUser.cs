using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("validate_users")]
public class ValidateUser : ISyncable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("civil_registryid")]
    public string? CivilRegistryId { get; set; }

    [Column("lastname")]
    public string? Lastname { get; set; }

    [Column("firstname")]
    public string? Firstname { get; set; }

    [Column("middlename")]
    public string? Middlename { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    /// <summary>Status: pending, accepted, rejected</summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("validated_by")]
    public long? ValidatedBy { get; set; }

    [Column("validated_at")]
    public DateTime? ValidatedAt { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("photo")]
    public string? Photo { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Stable cross-PC identity used by SyncService.</summary>
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    [ForeignKey("UserId")]
    public virtual User? AppUser { get; set; }

    // Computed helpers (not mapped)
    [NotMapped]
    public string FullName => $"{Firstname} {Middlename} {Lastname}".Trim();

    [NotMapped]
    public bool IsPending => Status == "pending";

    [NotMapped]
    public bool IsAccepted => Status == "accepted";

    [NotMapped]
    public bool IsRejected => Status == "rejected";
}
