namespace Budget_Accounting_System.Models;

public class AutoAnalyticalModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public ICollection<AutoAnalyticalRule> Rules { get; set; } = new List<AutoAnalyticalRule>();
}

public class AutoAnalyticalRule
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public RuleCondition Condition { get; set; }
    public string? ProductCategory { get; set; }
    public int? ProductId { get; set; }
    public int? ContactId { get; set; }
    public int AnalyticalAccountId { get; set; }
    public bool IsActive { get; set; } = true;

    public AutoAnalyticalModel Model { get; set; } = null!;
    public Product? Product { get; set; }
    public Contact? Contact { get; set; }
    public AnalyticalAccount AnalyticalAccount { get; set; } = null!;
}

public enum RuleCondition
{
    ProductCategory,
    SpecificProduct,
    Customer,
    Vendor
}
