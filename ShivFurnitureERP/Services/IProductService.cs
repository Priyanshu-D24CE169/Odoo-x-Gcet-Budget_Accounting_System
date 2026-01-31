using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels.Products;

namespace ShivFurnitureERP.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string? search, bool includeArchived, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Product> CreateAsync(Product product, string categoryName, CancellationToken cancellationToken);
    Task UpdateAsync(Product product, string categoryName, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken);
}
