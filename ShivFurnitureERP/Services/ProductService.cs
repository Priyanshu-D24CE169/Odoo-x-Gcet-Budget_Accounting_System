using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ApplicationDbContext dbContext, ILogger<ProductService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(string? search, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(p => !p.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Category != null && p.Category.Name.Contains(term)));
        }

        return await query
            .OrderByDescending(p => p.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);
    }

    public async Task<Product> CreateAsync(Product product, string categoryName, CancellationToken cancellationToken)
    {
        product.ProductCategoryId = await EnsureCategoryAsync(categoryName, cancellationToken);
        product.CreatedOn = DateTime.UtcNow;

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task UpdateAsync(Product updatedProduct, string categoryName, CancellationToken cancellationToken)
    {
        var existingProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == updatedProduct.ProductId, cancellationToken);
        if (existingProduct is null)
        {
            throw new InvalidOperationException($"Product {updatedProduct.ProductId} not found.");
        }

        existingProduct.Name = updatedProduct.Name;
        existingProduct.ProductCategoryId = await EnsureCategoryAsync(categoryName, cancellationToken);
        existingProduct.SalesPrice = updatedProduct.SalesPrice;
        existingProduct.PurchasePrice = updatedProduct.PurchasePrice;
        existingProduct.IsArchived = updatedProduct.IsArchived;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ProductCategories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task<int> EnsureCategoryAsync(string categoryName, CancellationToken cancellationToken)
    {
        var normalized = (categoryName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationException("Category name is required.");
        }

        var existing = await _dbContext.ProductCategories.FirstOrDefaultAsync(c => c.Name == normalized, cancellationToken);
        if (existing is not null)
        {
            return existing.ProductCategoryId;
        }

        var category = new ProductCategory
        {
            Name = normalized
        };

        _dbContext.ProductCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return category.ProductCategoryId;
    }
}
