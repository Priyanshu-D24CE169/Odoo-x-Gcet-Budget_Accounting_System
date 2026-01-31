using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.Products;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(string? search, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetProductsAsync(search, includeArchived, cancellationToken);
        var model = products.Select(p => new ProductListItemViewModel
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Category = p.Category?.Name ?? "-",
            SalesPrice = p.SalesPrice,
            PurchasePrice = p.PurchasePrice,
            IsArchived = p.IsArchived,
            CreatedOn = p.CreatedOn
        }).ToList();

        ViewData["Search"] = search;
        ViewData["IncludeArchived"] = includeArchived;
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = MapToEntity(model);
        await _productService.CreateAsync(product, model.CategoryName, cancellationToken);

        TempData["StatusMessage"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var model = MapToViewModel(product);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.ProductId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = MapToEntity(model);
        await _productService.UpdateAsync(product, model.CategoryName, cancellationToken);

        TempData["StatusMessage"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static Product MapToEntity(ProductFormViewModel model)
    {
        return new Product
        {
            ProductId = model.ProductId ?? 0,
            Name = model.Name,
            SalesPrice = model.SalesPrice,
            PurchasePrice = model.PurchasePrice,
            IsArchived = model.IsArchived
        };
    }

    private static ProductFormViewModel MapToViewModel(Product product)
    {
        return new ProductFormViewModel
        {
            ProductId = product.ProductId,
            Name = product.Name,
            CategoryName = product.Category?.Name ?? string.Empty,
            SalesPrice = product.SalesPrice,
            PurchasePrice = product.PurchasePrice,
            IsArchived = product.IsArchived,
            CreatedOn = product.CreatedOn
        };
    }
}
