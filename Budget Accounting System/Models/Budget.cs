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
    
    // Revision tracking
    public int? RevisedFromId { get; set; }
    
    [ForeignKey(nameof(RevisedFromId))]
    public Budget? RevisedFrom { get; set; }
    
    public int? RevisedWithId { get; set; }
    
    [ForeignKey(nameof(RevisedWithId))]
    public Budget? RevisedWith { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? RevisedDate { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ConfirmedBy { get; set; }
    public string? RevisedBy { get; set; }
    public string? ArchivedBy { get; set; }

    // Navigation
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
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
    
    // Computed fields (read-only)
    [NotMapped]
    public decimal AchievedAmount { get; set; }
    
    [NotMapped]
    public decimal AchievedPercentage => BudgetedAmount > 0 
        ? Math.Round((AchievedAmount / BudgetedAmount) * 100, 2) 
        : 0;
    
    [NotMapped]
    public decimal AmountToAchieve => BudgetedAmount - AchievedAmount;
    
    // Navigation
    public Budget Budget { get; set; } = null!;
    public AnalyticalAccount AnalyticalAccount { get; set; } = null!;
}

public enum BudgetState
{
    Draft,
    Confirmed,
    Revised,
    Archived,
    Cancelled
}

public enum BudgetLineType
{
    Income,
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


