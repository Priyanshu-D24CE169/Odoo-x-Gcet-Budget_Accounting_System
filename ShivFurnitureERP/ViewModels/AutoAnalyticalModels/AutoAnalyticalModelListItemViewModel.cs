using System;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.AutoAnalyticalModels;

public class AutoAnalyticalModelListItemViewModel
{
    public int ModelId { get; set; }
    public string PartnerName { get; set; } = "-";
    public string PartnerTagName { get; set; } = "-";
    public string ProductName { get; set; } = "-";
    public string ProductCategoryName { get; set; } = "-";
    public string AnalyticalAccountName { get; set; } = string.Empty;
    public AnalyticalModelStatus Status { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedOn { get; set; }
}
