using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShivFurnitureERP.Models;

public class AnalyticalBudget
{
    public int AnalyticalBudgetId { get; set; }

    public int AnalyticalAccountId { get; set; }
    public AnalyticalAccount? AnalyticalAccount { get; set; }

    [MaxLength(200)]
    public string BudgetName { get; set; } = string.Empty;

    public BudgetType BudgetType { get; set; } = BudgetType.Income;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LimitAmount { get; set; }

    public int? OriginalBudgetId { get; set; }
    public AnalyticalBudget? OriginalBudget { get; set; }
    public ICollection<AnalyticalBudget> Revisions { get; set; } = new List<AnalyticalBudget>();

    public bool IsReadOnly { get; set; }

    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
