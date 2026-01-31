using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.AnalyticalBudgets;

public class AnalyticalBudgetFormViewModel
{
    public int? AnalyticalBudgetId { get; set; }

    [Display(Name = "Budget Name")]
    [Required]
    [MaxLength(200)]
    public string BudgetName { get; set; } = string.Empty;

    [Display(Name = "Analytical Account")]
    [Required]
    public int AnalyticalAccountId { get; set; }

    [Display(Name = "Budget Type")]
    public BudgetType BudgetType { get; set; } = BudgetType.Income;

    [Display(Name = "Period Start")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime PeriodStart { get; set; }

    [Display(Name = "Period End")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime PeriodEnd { get; set; }

    [Display(Name = "Budget Limit")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Limit must be greater than zero.")]
    public decimal LimitAmount { get; set; }

    public bool IsRevision { get; set; }
    public int? OriginalBudgetId { get; set; }
    public string AnalyticalAccountName { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Accounts { get; set; } = Array.Empty<SelectListItem>();
}
