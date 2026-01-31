using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Services;

public static class DataSeeder
{
    public static async Task SeedData(ApplicationDbContext context)
    {
        if (context.Contacts.Any())
            return; // Database already seeded

        // Seed Contacts
        var contacts = new List<Contact>
        {
            new Contact { Name = "ABC Suppliers Ltd", Email = "info@abcsuppliers.com", Phone = "555-0101", Type = ContactType.Vendor, Address = "123 Vendor St" },
            new Contact { Name = "XYZ Timber Co", Email = "sales@xyztimber.com", Phone = "555-0102", Type = ContactType.Vendor, Address = "456 Wood Ave" },
            new Contact { Name = "John's Furniture Store", Email = "john@furniture.com", Phone = "555-0201", Type = ContactType.Customer, Address = "789 Customer Blvd" },
            new Contact { Name = "Modern Living Inc", Email = "info@modernliving.com", Phone = "555-0202", Type = ContactType.Customer, Address = "321 Retail Rd" },
            new Contact { Name = "Office Solutions", Email = "contact@officesolutions.com", Phone = "555-0203", Type = ContactType.Both, Address = "654 Business Park" }
        };
        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Seed Products
        var products = new List<Product>
        {
            new Product { Name = "Oak Wood Plank", Description = "Premium oak wood", Category = "Wood", UnitPrice = 25.00m, Unit = "Sq Ft" },
            new Product { Name = "Pine Wood Sheet", Description = "Standard pine", Category = "Wood", UnitPrice = 15.00m, Unit = "Sq Ft" },
            new Product { Name = "Office Desk", Description = "Modern office desk", Category = "Furniture", UnitPrice = 350.00m, Unit = "Unit" },
            new Product { Name = "Executive Chair", Description = "Ergonomic chair", Category = "Furniture", UnitPrice = 250.00m, Unit = "Unit" },
            new Product { Name = "Wood Varnish", Description = "Protective coating", Category = "Materials", UnitPrice = 45.00m, Unit = "Gallon" },
            new Product { Name = "Screws & Nails Kit", Description = "Hardware set", Category = "Materials", UnitPrice = 12.00m, Unit = "Kit" }
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Seed Analytical Accounts
        var analyticalAccounts = new List<AnalyticalAccount>
        {
            new AnalyticalAccount { Code = "PROD-001", Name = "Production Department", Description = "Main production activities" },
            new AnalyticalAccount { Code = "MKT-001", Name = "Marketing Department", Description = "Marketing and advertising" },
            new AnalyticalAccount { Code = "EXPO-2025", Name = "Furniture Expo 2025", Description = "Annual furniture exhibition" },
            new AnalyticalAccount { Code = "PROJ-OFF", Name = "Office Furniture Project", Description = "Corporate office furniture" },
            new AnalyticalAccount { Code = "R&D-001", Name = "Research & Development", Description = "Product development" }
        };
        context.AnalyticalAccounts.AddRange(analyticalAccounts);
        await context.SaveChangesAsync();

        // Seed Budgets
        var budgets = new List<Budget>
        {
            new Budget 
            { 
                Name = "Q1 2025 Production Budget", 
                AnalyticalAccountId = analyticalAccounts[0].Id, 
                StartDate = new DateTime(2025, 1, 1), 
                EndDate = new DateTime(2025, 3, 31),
                PlannedAmount = 50000m,
                Type = BudgetType.Expense
            },
            new Budget 
            { 
                Name = "Q1 2025 Marketing Budget", 
                AnalyticalAccountId = analyticalAccounts[1].Id, 
                StartDate = new DateTime(2025, 1, 1), 
                EndDate = new DateTime(2025, 3, 31),
                PlannedAmount = 15000m,
                Type = BudgetType.Expense
            },
            new Budget 
            { 
                Name = "Furniture Expo Revenue Target", 
                AnalyticalAccountId = analyticalAccounts[2].Id, 
                StartDate = new DateTime(2025, 6, 1), 
                EndDate = new DateTime(2025, 6, 30),
                PlannedAmount = 100000m,
                Type = BudgetType.Income
            },
            new Budget 
            { 
                Name = "Office Project Budget", 
                AnalyticalAccountId = analyticalAccounts[3].Id, 
                StartDate = new DateTime(2025, 1, 1), 
                EndDate = new DateTime(2025, 12, 31),
                PlannedAmount = 75000m,
                Type = BudgetType.Expense
            }
        };
        context.Budgets.AddRange(budgets);
        await context.SaveChangesAsync();

        // Seed Auto Analytical Model
        var autoModel = new AutoAnalyticalModel
        {
            Name = "Default Assignment Model",
            Description = "Automatic analytical account assignment based on product category",
            Priority = 1
        };
        context.AutoAnalyticalModels.Add(autoModel);
        await context.SaveChangesAsync();

        // Seed Auto Analytical Rules
        var rules = new List<AutoAnalyticalRule>
        {
            new AutoAnalyticalRule
            {
                ModelId = autoModel.Id,
                Condition = RuleCondition.ProductCategory,
                ProductCategory = "Wood",
                AnalyticalAccountId = analyticalAccounts[0].Id
            },
            new AutoAnalyticalRule
            {
                ModelId = autoModel.Id,
                Condition = RuleCondition.ProductCategory,
                ProductCategory = "Furniture",
                AnalyticalAccountId = analyticalAccounts[3].Id
            },
            new AutoAnalyticalRule
            {
                ModelId = autoModel.Id,
                Condition = RuleCondition.ProductCategory,
                ProductCategory = "Materials",
                AnalyticalAccountId = analyticalAccounts[0].Id
            }
        };
        context.AutoAnalyticalRules.AddRange(rules);
        await context.SaveChangesAsync();
    }
}
