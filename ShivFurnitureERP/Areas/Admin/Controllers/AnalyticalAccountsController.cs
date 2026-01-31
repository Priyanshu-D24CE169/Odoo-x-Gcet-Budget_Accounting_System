using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.AnalyticalAccounts;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AnalyticalAccountsController : Controller
{
    private readonly IAnalyticalAccountService _accountService;

    public AnalyticalAccountsController(IAnalyticalAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<IActionResult> Index(string? search, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var accounts = await _accountService.GetAccountsAsync(search, includeArchived, cancellationToken);
        var model = accounts.Select(a => new AnalyticalAccountListItemViewModel
        {
            AnalyticalAccountId = a.AnalyticalAccountId,
            Name = a.Name,
            Description = a.Description,
            IsArchived = a.IsArchived,
            CreatedOn = a.CreatedOn
        }).ToList();

        ViewData["Search"] = search;
        ViewData["IncludeArchived"] = includeArchived;
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AnalyticalAccountFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnalyticalAccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var account = MapToEntity(model);
        await _accountService.CreateAsync(account, cancellationToken);

        TempData["StatusMessage"] = "Analytical account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        var model = MapToViewModel(account);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnalyticalAccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.AnalyticalAccountId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var account = MapToEntity(model);
        await _accountService.UpdateAsync(account, cancellationToken);

        TempData["StatusMessage"] = "Analytical account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static AnalyticalAccount MapToEntity(AnalyticalAccountFormViewModel model)
    {
        return new AnalyticalAccount
        {
            AnalyticalAccountId = model.AnalyticalAccountId ?? 0,
            Name = model.Name,
            Description = model.Description,
            IsArchived = model.IsArchived
        };
    }

    private static AnalyticalAccountFormViewModel MapToViewModel(AnalyticalAccount account)
    {
        return new AnalyticalAccountFormViewModel
        {
            AnalyticalAccountId = account.AnalyticalAccountId,
            Name = account.Name,
            Description = account.Description,
            IsArchived = account.IsArchived,
            CreatedOn = account.CreatedOn
        };
    }
}
