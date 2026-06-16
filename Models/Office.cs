using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("tbl_offices")]
public class Office
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Auto-generated unique office code, e.g. OFF-2024-0001</summary>
    [Column("office_code")]
    public string? OfficeCode { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<DepartmentRole> Roles { get; set; } = new List<DepartmentRole>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

}
