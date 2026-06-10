using System.Collections.Generic;
using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.ViewModels;

public class SimpleDashboardViewModel : ViewModelBase
{
    public decimal TotalBudget { get; set; } = 1000;
    public decimal TotalIncome { get; set; } = 800;
    public decimal TotalExpense { get; set; } = 400;
    public int ActiveUsersCount { get; set; } = 5;
    
    public double IncomeHeight => 200;
    public double ExpenseHeight => 100;
    public List<DeptAllocationData> DeptAllocations { get; set; } = new List<DeptAllocationData> 
    { 
        new DeptAllocationData { Name = "Test", Amount = 100 } 
    };
}

