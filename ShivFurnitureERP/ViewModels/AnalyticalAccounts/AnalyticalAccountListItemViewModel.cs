namespace ShivFurnitureERP.ViewModels.AnalyticalAccounts;

public class AnalyticalAccountListItemViewModel
{
    public int AnalyticalAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedOn { get; set; }
}
