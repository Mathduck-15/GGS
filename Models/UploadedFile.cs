using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class UploadedFile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string StoragePath { get; set; } = string.Empty;

    [StringLength(50)]
    public string? FileType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.Now;

    // Relations — now use Office instead of Department
    public long OfficeId { get; set; }

    [ForeignKey("OfficeId")]
    public virtual Office? Office { get; set; }

    public int ParameterId { get; set; }

    [ForeignKey("ParameterId")]
    public virtual Parameter? Parameter { get; set; }
}
