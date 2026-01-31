using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class AutoAnalyticalModel
{
    public int ModelId { get; set; }

    public int? PartnerId { get; set; }
    public Contact? Partner { get; set; }

    public int? PartnerTagId { get; set; }
    public Tag? PartnerTag { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? ProductCategoryId { get; set; }
    public ProductCategory? ProductCategory { get; set; }

    [Required]
    public int AnalyticalAccountId { get; set; }
    public AnalyticalAccount? AnalyticalAccount { get; set; }

    [Required]
    public AnalyticalModelStatus Status { get; set; } = AnalyticalModelStatus.Draft;

    public bool IsArchived { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
