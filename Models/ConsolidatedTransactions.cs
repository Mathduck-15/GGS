using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models
{
    [Table("consolidated_transactions")]
    public class ConsolidatedTransactions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("beneficiary_id")]
        [StringLength(45)]
        public string? BeneficiaryId { get; set; }

        [Column("project_code")]
        [StringLength(45)]
        public string? ProjectCode { get; set; }

        [Column("project_details_id")]
        [StringLength(45)]
        public string? ProjectDetailsId { get; set; }

        [Column("project_name")]
        [StringLength(45)]
        public string? ProjectName { get; set; }

        [Column("civil_registry_id")]
        [StringLength(45)]
        public string? CivilRegistryId { get; set; }

        [Column("full_name")]
        [StringLength(45)]
        public string? FullName { get; set; }

        [Column("first_name")]
        [StringLength(45)]
        public string? FirstName { get; set; }

        [Column("middle_name")]
        [StringLength(45)]
        public string? MiddleName { get; set; }

        [Column("last_name")]
        [StringLength(45)]
        public string? LastName { get; set; }

        [Column("barangay")]
        [StringLength(45)]
        public string? Barangay { get; set; }

        [Column("household_no")]
        [StringLength(45)]
        public string? HouseholdNo { get; set; }

        [Column("office_id")]
        [StringLength(45)]
        public string? OfficeId { get; set; }

        [Column("office_name")]
        [StringLength(45)]
        public string? OfficeName { get; set; }

        [Column("transaction_type")]
        [StringLength(45)]
        public string? TransactionType { get; set; }

        [Column("amount", TypeName = "decimal(20,4)")]
        public decimal? Amount { get; set; }

        [Column("transaction_date")]
        public DateTime? TransactionDate { get; set; }

        [Column("status")]
        [StringLength(45)]
        public string? Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } 

        [Column("SyncId")]
        public Guid SyncId { get; set; } = Guid.NewGuid();

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
