using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.AutoAnalyticalModels;

public class AutoAnalyticalModelFormViewModel
{
    public int? ModelId { get; set; }

    [Display(Name = "Partner Tag")]
    public int? PartnerTagId { get; set; }

    [Display(Name = "Partner")]
    public int? PartnerId { get; set; }

    [Display(Name = "Product Category")]
    public int? ProductCategoryId { get; set; }

    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    [Display(Name = "Analytical Account")]
    [Required]
    public int? AnalyticalAccountId { get; set; }

    [Display(Name = "Status")]
    public AnalyticalModelStatus Status { get; set; } = AnalyticalModelStatus.Draft;

    public bool IsArchived { get; set; }

    public DateTime? CreatedOn { get; set; }

    public IEnumerable<SelectListItem> PartnerTags { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Partners { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ProductCategories { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Products { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> AnalyticalAccounts { get; set; } = Enumerable.Empty<SelectListItem>();
}
