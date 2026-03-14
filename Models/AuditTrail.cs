using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("audit_trails")]
public class AuditTrail
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("action")]
    public string? Action { get; set; }

    [Column("model_type")]
    public string? ModelType { get; set; }

    [Column("model_id")]
    public long? ModelId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("office_id")]
    public long? OfficeId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
