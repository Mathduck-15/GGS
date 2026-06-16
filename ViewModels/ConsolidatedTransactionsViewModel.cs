using GoodGovernanceApp.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoodGovernanceApp.ViewModels
{
    public class ConsolidatedTransactionsViewModel
    {
        // 1. Properties stay here
        public int Id { get; set; }
        public string BeneficiaryId { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string CivilRegistryId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string OfficeId { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = "Consolidated";

        // 2. Query goes inside a method
        public static async Task<List<ConsolidatedTransactionsViewModel>> GetTransactionsAsync(AppDbContext context)
        {
            var transactions = await context.ConsolidatedTransactions
                .Select(t => new ConsolidatedTransactionsViewModel
                {
                    Id = t.Id,
                    BeneficiaryId = t.BeneficiaryId ?? string.Empty,
                    ProjectCode = t.ProjectCode ?? string.Empty,
                    CivilRegistryId = t.CivilRegistryId ?? string.Empty,
                    FullName = t.FullName ?? string.Empty,
                    FirstName = t.FirstName ?? string.Empty,
                    MiddleName = t.MiddleName ?? string.Empty,
                    LastName = t.LastName ?? string.Empty,
                    OfficeId = t.OfficeId ?? string.Empty,
                    OfficeName = t.OfficeName ?? string.Empty,
                    TransactionType = t.TransactionType ?? string.Empty,
                    Amount = t.Amount ?? 0,
                    TransactionDate = t.TransactionDate,
                    Status = t.Status ?? string.Empty,
                    CreatedAt = t.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            return transactions;
        }
    }

}
