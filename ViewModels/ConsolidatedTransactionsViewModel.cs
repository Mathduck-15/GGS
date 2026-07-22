using GoodGovernanceApp.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoodGovernanceApp.ViewModels
{
    public class ConsolidatedTransactionsViewModel
    {
        // 1. Properties stay here
        public int Id { get; set; }
        public string BeneficiaryId { get; set; } = string.Empty;
        public string CivilRegistryId { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectDetailsId { get; set; } = string.Empty;  // blank when not set
        public string ProjectName { get; set; } = string.Empty;
        public string OfficeId { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public string HouseholdNo { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Source { get; set; } = "Consolidated";

        // 2. Query goes inside a method
        public static async Task<List<ConsolidatedTransactionsViewModel>> GetTransactionsAsync(AppDbContext context)
        {
            var transactions = await context.ConsolidatedTransactions
                .Where(t => !string.IsNullOrEmpty(t.BeneficiaryId))
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new ConsolidatedTransactionsViewModel
                {
                    Id = t.Id,
                    BeneficiaryId = t.BeneficiaryId ?? string.Empty,
                    CivilRegistryId = t.CivilRegistryId ?? string.Empty,
                    ProjectCode = t.ProjectCode ?? string.Empty,
                    ProjectDetailsId = t.ProjectDetailsId ?? string.Empty,
                    ProjectName = t.ProjectName ?? string.Empty,
                    OfficeId = t.OfficeId ?? string.Empty,
                    OfficeName = t.OfficeName ?? string.Empty,
                    FullName = t.FullName ?? string.Empty,
                    FirstName = t.FirstName ?? string.Empty,
                    MiddleName = t.MiddleName ?? string.Empty,
                    LastName = t.LastName ?? string.Empty,
                    Barangay = t.Barangay ?? string.Empty,
                    HouseholdNo = t.HouseholdNo ?? string.Empty,
                    TransactionType = t.TransactionType ?? string.Empty,
                    Amount = t.Amount ?? 0,
                    TransactionDate = t.TransactionDate,
                    Status = t.Status ?? string.Empty,
                    CreatedAt = t.CreatedAt ?? DateTime.MinValue,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return transactions;
        }
    }
}