using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Budget_Accounting_System.Models;

public class Budget
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Budget name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }
    
    [Required]
    public BudgetState State { get; set; } = BudgetState.Draft;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Revision tracking
    public int? RevisedFromId { get; set; }
    
    [ForeignKey(nameof(RevisedFromId))]
    public Budget? RevisedFrom { get; set; }
    
    public int? RevisedWithId { get; set; }
    
    [ForeignKey(nameof(RevisedWithId))]
    public Budget? RevisedWith { get; set; }
    
    // Audit fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? RevisedDate { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    
    [StringLength(100)]
    public string? CreatedBy { get; set; }
    
    [StringLength(100)]
    public string? ModifiedBy { get; set; }
    
    [StringLength(100)]
    public string? ConfirmedBy { get; set; }
    
    [StringLength(100)]
    public string? RevisedBy { get; set; }
    
    [StringLength(100)]
    public string? ArchivedBy { get; set; }
    
    [StringLength(100)]
    public string? CancelledBy { get; set; }

    // Navigation
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
    
    // Computed properties
    [NotMapped]
    public bool CanEdit => State == BudgetState.Draft;
    
    [NotMapped]
    public bool CanConfirm => State == BudgetState.Draft && Lines.Any();
    
    [NotMapped]
    public bool CanRevise => State == BudgetState.Confirmed;
    
    [NotMapped]
    public bool CanArchive => State == BudgetState.Confirmed || State == BudgetState.Revised;
    
    [NotMapped]
    public bool CanCancel => State == BudgetState.Draft;
    
    [NotMapped]
    public bool IsRevision => RevisedFromId.HasValue;
    
    [NotMapped]
    public bool HasRevision => RevisedWithId.HasValue;
    
    [NotMapped]
    public string DisplayName => IsRevision 
        ? $"{Name} (Rev {RevisedDate?.ToString("dd MMM yyyy") ?? ""})"
        : Name;
    
    [NotMapped]
    public decimal TotalBudgetedIncome => Lines
        .Where(l => l.Type == BudgetLineType.Income)
        .Sum(l => l.BudgetedAmount);
    
    [NotMapped]
    public decimal TotalBudgetedExpense => Lines
        .Where(l => l.Type == BudgetLineType.Expense)
        .Sum(l => l.BudgetedAmount);
    
    [NotMapped]
    public decimal TotalBudgetedNet => TotalBudgetedIncome - TotalBudgetedExpense;
    
    [NotMapped]
    public decimal TotalAchievedIncome => Lines
        .Where(l => l.Type == BudgetLineType.Income)
        .Sum(l => l.AchievedAmount);
    
    [NotMapped]
    public decimal TotalAchievedExpense => Lines
        .Where(l => l.Type == BudgetLineType.Expense)
        .Sum(l => l.AchievedAmount);
    
    [NotMapped]
    public decimal TotalAchievedNet => TotalAchievedIncome - TotalAchievedExpense;
    
    [NotMapped]
    public int DaysRemaining
    {
        get
        {
            var days = (EndDate - DateTime.Today).Days;
            return days > 0 ? days : 0;
        }
    }
    
    [NotMapped]
    public int TotalDays => (EndDate - StartDate).Days + 1;
    
    [NotMapped]
    public int DaysElapsed
    {
        get
        {
            var days = (DateTime.Today - StartDate).Days;
            return days > 0 ? Math.Min(days, TotalDays) : 0;
        }
    }
    
    [NotMapped]
    public decimal ProgressPercentage => TotalDays > 0 
        ? Math.Round((decimal)DaysElapsed / TotalDays * 100, 2) 
        : 0;
    
    // Helper methods
    public string GetStateBadgeClass()
    {
        return State switch
        {
            BudgetState.Draft => "bg-secondary",
            BudgetState.Confirmed => "bg-success",
            BudgetState.Revised => "bg-warning text-dark",
            BudgetState.Archived => "bg-dark",
            BudgetState.Cancelled => "bg-danger",
            _ => "bg-secondary"
        };
    }
    
    public string GetStateIcon()
    {
        return State switch
        {
            BudgetState.Draft => "bi-file-earmark",
            BudgetState.Confirmed => "bi-check-circle-fill",
            BudgetState.Revised => "bi-arrow-repeat",
            BudgetState.Archived => "bi-archive-fill",
            BudgetState.Cancelled => "bi-x-circle-fill",
            _ => "bi-file-earmark"
        };
    }
    
    public bool IsActive()
    {
        return State == BudgetState.Confirmed && 
               DateTime.Today >= StartDate && 
               DateTime.Today <= EndDate;
    }
    
    public bool IsInPeriod(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }
}

public class BudgetLine
{
    public int Id { get; set; }
    
    [Required]
    public int BudgetId { get; set; }
    
    [Required(ErrorMessage = "Analytical account is required")]
    [Display(Name = "Analytical Account")]
    public int AnalyticalAccountId { get; set; }
    
    [Required(ErrorMessage = "Type is required")]
    public BudgetLineType Type { get; set; }
    
    [Required(ErrorMessage = "Budgeted amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Budgeted amount must be greater than 0")]
    [Display(Name = "Budgeted Amount")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BudgetedAmount { get; set; }
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    // Computed fields (calculated at runtime)
    [NotMapped]
    public decimal AchievedAmount { get; set; }
    
    [NotMapped]
    public decimal AchievedPercentage => BudgetedAmount > 0 
        ? Math.Round((AchievedAmount / BudgetedAmount) * 100, 2) 
        : 0;
    
    [NotMapped]
    public decimal AmountToAchieve => BudgetedAmount - AchievedAmount;
    
    [NotMapped]
    public decimal VarianceAmount => AchievedAmount - BudgetedAmount;
    
    [NotMapped]
    public decimal VariancePercentage => BudgetedAmount > 0
        ? Math.Round((VarianceAmount / BudgetedAmount) * 100, 2)
        : 0;
    
    [NotMapped]
    public bool IsOverBudget => Type == BudgetLineType.Expense && AchievedAmount > BudgetedAmount;
    
    [NotMapped]
    public bool IsUnderBudget => Type == BudgetLineType.Income && AchievedAmount < BudgetedAmount;
    
    // Navigation
    public Budget Budget { get; set; } = null!;
    public AnalyticalAccount AnalyticalAccount { get; set; } = null!;
    
    // Helper methods
    public string GetVarianceBadgeClass()
    {
        if (Type == BudgetLineType.Expense)
        {
            if (AchievedAmount > BudgetedAmount) return "bg-danger";
            if (AchievedAmount > BudgetedAmount * 0.9m) return "bg-warning text-dark";
            return "bg-success";
        }
        else // Income
        {
            if (AchievedAmount >= BudgetedAmount) return "bg-success";
            if (AchievedAmount >= BudgetedAmount * 0.75m) return "bg-warning text-dark";
            return "bg-danger";
        }
    }
}

public enum BudgetState
{
    [Display(Name = "Draft")]
    Draft,
    
    [Display(Name = "Confirmed")]
    Confirmed,
    
    [Display(Name = "Revised")]
    Revised,
    
    [Display(Name = "Archived")]
    Archived,
    
    [Display(Name = "Cancelled")]
    Cancelled
}

public enum BudgetLineType
{
    [Display(Name = "Income")]
    Income,
    
    [Display(Name = "Expense")]
    Expense
}


// Keep for backward compatibility but deprecated
public enum BudgetType
{
    Income,
    Expense
}

public class BudgetRevision
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public decimal OldAmount { get; set; }
    public decimal NewAmount { get; set; }
    public string? Reason { get; set; }
    public DateTime RevisionDate { get; set; } = DateTime.UtcNow;
    public string? RevisedBy { get; set; }

    public Budget Budget { get; set; } = null!;
}


