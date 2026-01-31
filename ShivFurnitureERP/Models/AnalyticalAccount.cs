using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class AnalyticalAccount
{
    public int AnalyticalAccountId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
