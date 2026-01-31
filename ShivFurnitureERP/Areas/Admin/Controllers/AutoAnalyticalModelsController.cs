using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels.AutoAnalyticalModels;
using System;
using System.Linq;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AutoAnalyticalModelsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public AutoAnalyticalModelsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AnalyticalModelStatus? status = AnalyticalModelStatus.Confirmed, bool showArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AutoAnalyticalModels
            .AsNoTracking()
            .Include(m => m.Partner)
            .Include(m => m.PartnerTag)
            .Include(m => m.Product)
            .Include(m => m.ProductCategory)
            .Include(m => m.AnalyticalAccount)
            .OrderByDescending(m => m.CreatedOn)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        query = showArchived
            ? query.Where(m => m.IsArchived)
            : query.Where(m => !m.IsArchived);

        var models = await query.ToListAsync(cancellationToken);
        var list = models.Select(MapToListItem).ToList();

        ViewData["StatusFilter"] = status;
        ViewData["ShowArchived"] = showArchived;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new AutoAnalyticalModelFormViewModel
        {
            Status = AnalyticalModelStatus.Draft
        };
        await PopulateLookupsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AutoAnalyticalModelFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateLookupsAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = MapToEntity(model);
        _dbContext.AutoAnalyticalModels.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["StatusMessage"] = "Auto analytical model created.";
        return RedirectToAction(nameof(Index), new { status = model.Status });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.AutoAnalyticalModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var model = MapToFormModel(entity);
        await PopulateLookupsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AutoAnalyticalModelFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.ModelId)
        {
            return BadRequest();
        }

        await PopulateLookupsAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = await _dbContext.AutoAnalyticalModels.FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.PartnerId = model.PartnerId;
        entity.PartnerTagId = model.PartnerTagId;
        entity.ProductId = model.ProductId;
        entity.ProductCategoryId = model.ProductCategoryId;
        entity.AnalyticalAccountId = model.AnalyticalAccountId ?? throw new InvalidOperationException("Analytical account is required.");
        entity.Status = model.Status;
        if (entity.Status != AnalyticalModelStatus.Archived)
        {
            entity.IsArchived = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Auto analytical model updated.";
        return RedirectToAction(nameof(Index), new { status = model.Status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, AnalyticalModelStatus status, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.AutoAnalyticalModels.FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Status = status;
        if (status == AnalyticalModelStatus.Archived)
        {
            entity.IsArchived = true;
        }
        else if (entity.IsArchived)
        {
            entity.IsArchived = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"Model marked as {status}.";
        var showArchived = status == AnalyticalModelStatus.Archived;
        return RedirectToAction(nameof(Index), new { status, showArchived });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleArchive(int id, bool archive, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.AutoAnalyticalModels.FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.IsArchived = archive;
        if (archive)
        {
            entity.Status = AnalyticalModelStatus.Archived;
        }
        else if (entity.Status == AnalyticalModelStatus.Archived)
        {
            entity.Status = AnalyticalModelStatus.Draft;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = archive ? "Model archived." : "Model restored.";
        return RedirectToAction(nameof(Index), new { showArchived = archive, status = archive ? AnalyticalModelStatus.Archived : entity.Status });
    }

    private static AutoAnalyticalModelListItemViewModel MapToListItem(AutoAnalyticalModel model)
    {
        return new AutoAnalyticalModelListItemViewModel
        {
            ModelId = model.ModelId,
            PartnerName = model.Partner?.Name ?? "Any partner",
            PartnerTagName = model.PartnerTag?.Name ?? "Any tag",
            ProductName = model.Product?.Name ?? "Any product",
            ProductCategoryName = model.ProductCategory?.Name ?? "Any category",
            AnalyticalAccountName = model.AnalyticalAccount?.Name ?? "-",
            Status = model.Status,
            IsArchived = model.IsArchived,
            CreatedOn = model.CreatedOn
        };
    }

    private static AutoAnalyticalModelFormViewModel MapToFormModel(AutoAnalyticalModel entity)
    {
        return new AutoAnalyticalModelFormViewModel
        {
            ModelId = entity.ModelId,
            PartnerTagId = entity.PartnerTagId,
            PartnerId = entity.PartnerId,
            ProductCategoryId = entity.ProductCategoryId,
            ProductId = entity.ProductId,
            AnalyticalAccountId = entity.AnalyticalAccountId,
            Status = entity.Status,
            IsArchived = entity.IsArchived,
            CreatedOn = entity.CreatedOn
        };
    }

    private static AutoAnalyticalModel MapToEntity(AutoAnalyticalModelFormViewModel model)
    {
        var analyticalAccountId = model.AnalyticalAccountId ?? throw new InvalidOperationException("Analytical account is required.");

        return new AutoAnalyticalModel
        {
            PartnerTagId = model.PartnerTagId,
            PartnerId = model.PartnerId,
            ProductCategoryId = model.ProductCategoryId,
            ProductId = model.ProductId,
            AnalyticalAccountId = analyticalAccountId,
            Status = model.Status
        };
    }

    private async Task PopulateLookupsAsync(AutoAnalyticalModelFormViewModel model, CancellationToken cancellationToken)
    {
        var tags = await _dbContext.Tags
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Text = t.Name, Value = t.TagId.ToString() })
            .ToListAsync(cancellationToken);
        tags.Insert(0, new SelectListItem { Text = "Any partner tag", Value = string.Empty });

        var partners = await _dbContext.Contacts
            .Where(c => !c.IsArchived)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Text = c.Name, Value = c.ContactId.ToString() })
            .ToListAsync(cancellationToken);
        partners.Insert(0, new SelectListItem { Text = "Any partner", Value = string.Empty });

        var categories = await _dbContext.ProductCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Text = c.Name, Value = c.ProductCategoryId.ToString() })
            .ToListAsync(cancellationToken);
        categories.Insert(0, new SelectListItem { Text = "Any category", Value = string.Empty });

        var products = await _dbContext.Products
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Text = p.Name, Value = p.ProductId.ToString() })
            .ToListAsync(cancellationToken);
        products.Insert(0, new SelectListItem { Text = "Any product", Value = string.Empty });

        var analyticalAccounts = await _dbContext.AnalyticalAccounts
            .OrderBy(a => a.Name)
            .Select(a => new SelectListItem { Text = a.Name, Value = a.AnalyticalAccountId.ToString() })
            .ToListAsync(cancellationToken);

        model.PartnerTags = tags;
        model.Partners = partners;
        model.ProductCategories = categories;
        model.Products = products;
        model.AnalyticalAccounts = analyticalAccounts;
    }
}
