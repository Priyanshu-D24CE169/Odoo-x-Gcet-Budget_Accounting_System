namespace Budget_Accounting_System.Models;

public class Budget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AnalyticalAccountId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PlannedAmount { get; set; }
    public BudgetType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public AnalyticalAccount AnalyticalAccount { get; set; } = null!;
    public ICollection<BudgetRevision> Revisions { get; set; } = new List<BudgetRevision>();
}

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
