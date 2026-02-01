using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Infrastructure;

public static class DomainDataSeeder
{
    private static readonly string[] AnalyticalAccountNames =
    {
        "Deepawali",
        "Furniture Expo 2026",
        "Marriage Session 2026"
    };

    private static readonly (string Name, ContactType Type, string Email)[] ContactSeeds =
    {
        ("Mr. A", ContactType.Customer, "mr.a@example.local"),
        ("Azure Interior", ContactType.Vendor, "azure.interior@example.local")
    };

    private static readonly string[] ProductCategoryNames =
    {
        "Wooden Furniture",
        "Metal Furniture"
    };

    private static readonly (string Name, string Category)[] ProductSeeds =
    {
        ("Chair", "Wooden Furniture"),
        ("Table", "Wooden Furniture")
    };

    private sealed record AutoAnalyticalModelSeed(
        string? PartnerName,
        string? ProductCategoryName,
        string? ProductName,
        string AnalyticalAccountName);

    private static readonly AutoAnalyticalModelSeed[] AutoModelSeeds =
    {
        new("Mr. A", "Wooden Furniture", null, "Deepawali"),
        new(null, "Wooden Furniture", null, "Furniture Expo 2026"),
        new(null, null, "Chair", "Marriage Session 2026")
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var dbContext = scopedProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DomainDataSeeder");

        var analyticalAccounts = await EnsureAnalyticalAccountsAsync(dbContext, logger);
        var productCategories = await EnsureProductCategoriesAsync(dbContext, logger);
        var products = await EnsureProductsAsync(dbContext, productCategories, logger);
        var contacts = await EnsureContactsAsync(dbContext, logger);

        await EnsureAutoAnalyticalModelsAsync(dbContext, analyticalAccounts, productCategories, products, contacts, logger);
    }

    private static async Task<Dictionary<string, AnalyticalAccount>> EnsureAnalyticalAccountsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var map = new Dictionary<string, AnalyticalAccount>(StringComparer.OrdinalIgnoreCase);
        var existing = await dbContext.AnalyticalAccounts
            .Where(a => AnalyticalAccountNames.Contains(a.Name))
            .ToListAsync();

        foreach (var account in existing)
        {
            map[account.Name] = account;
        }

        foreach (var name in AnalyticalAccountNames)
        {
            if (map.ContainsKey(name))
            {
                continue;
            }

            var entity = new AnalyticalAccount
            {
                Name = name,
                Description = $"Automatically seeded account for {name} initiatives.",
                CreatedOn = DateTime.UtcNow
            };
            dbContext.AnalyticalAccounts.Add(entity);
            map[name] = entity;
            logger.LogInformation("Seeded analytical account {AccountName}", name);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        return map;
    }

    private static async Task<Dictionary<string, ProductCategory>> EnsureProductCategoriesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var map = new Dictionary<string, ProductCategory>(StringComparer.OrdinalIgnoreCase);
        var existing = await dbContext.ProductCategories
            .Where(c => ProductCategoryNames.Contains(c.Name))
            .ToListAsync();

        foreach (var category in existing)
        {
            map[category.Name] = category;
        }

        foreach (var name in ProductCategoryNames)
        {
            if (map.ContainsKey(name))
            {
                continue;
            }

            var entity = new ProductCategory
            {
                Name = name
            };
            dbContext.ProductCategories.Add(entity);
            map[name] = entity;
            logger.LogInformation("Seeded product category {Category}", name);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        return map;
    }

    private static async Task<Dictionary<string, Product>> EnsureProductsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyDictionary<string, ProductCategory> categories,
        ILogger logger)
    {
        var map = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        var productNames = ProductSeeds.Select(p => p.Name).ToArray();

        var existing = await dbContext.Products
            .Where(p => productNames.Contains(p.Name))
            .ToListAsync();

        foreach (var product in existing)
        {
            map[product.Name] = product;
        }

        foreach (var (name, categoryName) in ProductSeeds)
        {
            if (map.ContainsKey(name))
            {
                continue;
            }

            if (!categories.TryGetValue(categoryName, out var category))
            {
                logger.LogWarning("Skipping product {Product} because category {Category} is missing.", name, categoryName);
                continue;
            }

            var entity = new Product
            {
                Name = name,
                ProductCategoryId = category.ProductCategoryId,
                SalesPrice = 0,
                PurchasePrice = 0,
                CreatedOn = DateTime.UtcNow
            };
            dbContext.Products.Add(entity);
            map[name] = entity;
            logger.LogInformation("Seeded product {ProductName}", name);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        return map;
    }

    private static async Task<Dictionary<string, Contact>> EnsureContactsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var map = new Dictionary<string, Contact>(StringComparer.OrdinalIgnoreCase);
        var emails = ContactSeeds.Select(c => c.Email).ToArray();

        var existing = await dbContext.Contacts
            .Where(c => emails.Contains(c.Email))
            .ToListAsync();

        foreach (var contact in existing)
        {
            map[contact.Name] = contact;
        }

        foreach (var (name, type, email) in ContactSeeds)
        {
            if (map.ContainsKey(name))
            {
                continue;
            }

            var entity = new Contact
            {
                Name = name,
                Email = email,
                Type = type,
                CreatedOn = DateTime.UtcNow
            };
            dbContext.Contacts.Add(entity);
            map[name] = entity;
            logger.LogInformation("Seeded contact {ContactName}", name);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        return map;
    }

    private static async Task EnsureAutoAnalyticalModelsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyDictionary<string, AnalyticalAccount> accounts,
        IReadOnlyDictionary<string, ProductCategory> categories,
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyDictionary<string, Contact> contacts,
        ILogger logger)
    {
        foreach (var seed in AutoModelSeeds)
        {
            if (!accounts.TryGetValue(seed.AnalyticalAccountName, out var account))
            {
                logger.LogWarning("Skipping auto analytical model because analytical account {Account} is missing.", seed.AnalyticalAccountName);
                continue;
            }

            int? partnerId = null;
            if (!string.IsNullOrWhiteSpace(seed.PartnerName))
            {
                if (!contacts.TryGetValue(seed.PartnerName, out var contact))
                {
                    logger.LogWarning("Skipping rule for partner {Partner} because contact is missing.", seed.PartnerName);
                    continue;
                }
                partnerId = contact.ContactId;
            }

            int? productCategoryId = null;
            if (!string.IsNullOrWhiteSpace(seed.ProductCategoryName))
            {
                if (!categories.TryGetValue(seed.ProductCategoryName, out var category))
                {
                    logger.LogWarning("Skipping rule targeting category {Category} because it is missing.", seed.ProductCategoryName);
                    continue;
                }
                productCategoryId = category.ProductCategoryId;
            }

            int? productId = null;
            if (!string.IsNullOrWhiteSpace(seed.ProductName))
            {
                if (!products.TryGetValue(seed.ProductName, out var product))
                {
                    logger.LogWarning("Skipping rule targeting product {Product} because it is missing.", seed.ProductName);
                    continue;
                }
                productId = product.ProductId;
            }

            var exists = await dbContext.AutoAnalyticalModels.AnyAsync(m =>
                m.PartnerId == partnerId &&
                m.PartnerTagId == null &&
                m.ProductId == productId &&
                m.ProductCategoryId == productCategoryId &&
                m.AnalyticalAccountId == account.AnalyticalAccountId &&
                m.Status == AnalyticalModelStatus.Confirmed &&
                !m.IsArchived);

            if (exists)
            {
                continue;
            }

            dbContext.AutoAnalyticalModels.Add(new AutoAnalyticalModel
            {
                PartnerId = partnerId,
                ProductId = productId,
                ProductCategoryId = productCategoryId,
                AnalyticalAccountId = account.AnalyticalAccountId,
                Status = AnalyticalModelStatus.Confirmed,
                CreatedOn = DateTime.UtcNow
            });
            logger.LogInformation("Seeded auto analytical model targeting account {Account}", seed.AnalyticalAccountName);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }
    }
}
