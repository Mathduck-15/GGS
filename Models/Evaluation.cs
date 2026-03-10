using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class Evaluation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UploadedFileId { get; set; }

    [ForeignKey("UploadedFileId")]
    public virtual UploadedFile? UploadedFile { get; set; }

    [Required]
    public int EvaluatorId { get; set; }

    [ForeignKey("EvaluatorId")]
    public virtual User? Evaluator { get; set; }

    [Range(0, 100)]
    public int Score { get; set; }

    [StringLength(1000)]
    public string? Comments { get; set; }

    public DateTime EvaluationDate { get; set; } = DateTime.Now;
}
