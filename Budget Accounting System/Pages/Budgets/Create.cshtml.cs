using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.Budgets;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public List<AnalyticalAccount> AnalyticalAccounts { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Budget name is required")]
        [StringLength(200)]
        [Display(Name = "Budget Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

        public List<BudgetLineInput> Lines { get; set; } = new();
    }

    public class BudgetLineInput
    {
        [Required]
        public int AnalyticalAccountId { get; set; }

        [Required]
        public BudgetLineType Type { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal BudgetedAmount { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Input = new InputModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Lines = new List<BudgetLineInput>
            {
                new BudgetLineInput() // Start with one empty line
            }
        };

        await LoadAnalyticalAccountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        _logger.LogInformation("Budget Create POST - Action: {Action}, Name: {Name}", action, Input?.Name);

        // Check if Input is null
        if (Input == null)
        {
            _logger.LogError("Input is null");
            ModelState.AddModelError("", "Form data is missing. Please try again.");
            await LoadAnalyticalAccountsAsync();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
            }
            await LoadAnalyticalAccountsAsync();
            return Page();
        }

        // Validate date range
        if (Input.EndDate <= Input.StartDate)
        {
            ModelState.AddModelError("Input.EndDate", "End date must be after start date.");
            await LoadAnalyticalAccountsAsync();
            return Page();
        }

        // Validate at least one line
        if (Input.Lines == null || !Input.Lines.Any())
        {
            ModelState.AddModelError("", "Please add at least one budget line.");
            await LoadAnalyticalAccountsAsync();
            return Page();
        }

        // Validate no duplicate analytical account + type combinations
        var duplicates = Input.Lines
            .GroupBy(l => new { l.AnalyticalAccountId, l.Type })
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            ModelState.AddModelError("", "Cannot have duplicate analytical account and type combinations.");
            await LoadAnalyticalAccountsAsync();
            return Page();
        }

        try
        {
            _logger.LogInformation("Creating budget with {LineCount} lines", Input.Lines.Count);

            var budget = new Budget
            {
                Name = Input.Name,
                StartDate = Input.StartDate,
                EndDate = Input.EndDate,
                State = action == "confirm" ? BudgetState.Confirmed : BudgetState.Draft,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            if (action == "confirm")
            {
                budget.ConfirmedDate = DateTime.UtcNow;
                budget.ConfirmedBy = User.Identity?.Name;
            }

            // Add budget lines
            foreach (var lineInput in Input.Lines)
            {
                _logger.LogInformation("Adding line - Account: {AccountId}, Type: {Type}, Amount: {Amount}", 
                    lineInput.AnalyticalAccountId, lineInput.Type, lineInput.BudgetedAmount);

                var budgetLine = new BudgetLine
                {
                    AnalyticalAccountId = lineInput.AnalyticalAccountId,
                    Type = lineInput.Type,
                    BudgetedAmount = lineInput.BudgetedAmount
                };

                budget.Lines.Add(budgetLine);
            }

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Budget {Name} created successfully with {LineCount} lines, State: {State}", 
                budget.Name, budget.Lines.Count, budget.State);

            TempData["SuccessMessage"] = $"Budget '{budget.Name}' has been {(action == "confirm" ? "created and confirmed" : "created as draft")}.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating budget: {Message}", ex.Message);
            
            // Check if it's a database schema issue
            if (ex.Message.Contains("Invalid column name") || ex.Message.Contains("State"))
            {
                ModelState.AddModelError("", "Database migration required. Please run: IMPLEMENT-NEW-BUDGET-MODULE.bat");
            }
            else
            {
                ModelState.AddModelError("", $"An error occurred while creating the budget: {ex.Message}");
            }
            
            await LoadAnalyticalAccountsAsync();
            return Page();
        }
    }

    private async Task LoadAnalyticalAccountsAsync()
    {
        AnalyticalAccounts = await _context.AnalyticalAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();
    }
}

