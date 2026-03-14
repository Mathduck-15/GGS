using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("tbl_requests")]
public class ServiceRequest
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("constituent_id")]
    public long? ConstituentId { get; set; }

    [ForeignKey("ConstituentId")]
    public virtual Constituent? Constituent { get; set; }

    [Column("service_type")]
    public string? ServiceType { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
