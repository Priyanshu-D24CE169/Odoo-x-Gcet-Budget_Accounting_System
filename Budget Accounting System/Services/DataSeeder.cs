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
            new Contact { Name = "ABC Suppliers Ltd", Email = "info@abcsuppliers.com", Phone = "555-0101", Type = ContactType.Vendor, Street = "123 Vendor St", City = "Mumbai", State = ContactState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Contact { Name = "XYZ Timber Co", Email = "sales@xyztimber.com", Phone = "555-0102", Type = ContactType.Vendor, Street = "456 Wood Ave", City = "Delhi", State = ContactState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Contact { Name = "John's Furniture Store", Email = "john@furniture.com", Phone = "555-0201", Type = ContactType.Customer, Street = "789 Customer Blvd", City = "Bangalore", State = ContactState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Contact { Name = "Modern Living Inc", Email = "info@modernliving.com", Phone = "555-0202", Type = ContactType.Customer, Street = "321 Retail Rd", City = "Chennai", State = ContactState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Contact { Name = "Office Solutions", Email = "contact@officesolutions.com", Phone = "555-0203", Type = ContactType.Both, Street = "654 Business Park", City = "Pune", State = ContactState.Confirmed, ConfirmedDate = DateTime.UtcNow }
        };
        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Seed Categories
        var categories = new List<Category>
        {
            new Category { Name = "Wood" },
            new Category { Name = "Furniture" },
            new Category { Name = "Materials" },
            new Category { Name = "Hardware" },
            new Category { Name = "Finishing" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Seed Products
        var products = new List<Product>
        {
            new Product { Name = "Oak Wood Plank", Description = "Premium oak wood", CategoryId = categories[0].Id, SalesPrice = 30.00m, PurchasePrice = 25.00m, Unit = "Sq Ft", State = ProductState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Product { Name = "Pine Wood Sheet", Description = "Standard pine", CategoryId = categories[0].Id, SalesPrice = 20.00m, PurchasePrice = 15.00m, Unit = "Sq Ft", State = ProductState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Product { Name = "Office Desk", Description = "Modern office desk", CategoryId = categories[1].Id, SalesPrice = 450.00m, PurchasePrice = 350.00m, Unit = "Unit", State = ProductState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Product { Name = "Executive Chair", Description = "Ergonomic chair", CategoryId = categories[1].Id, SalesPrice = 320.00m, PurchasePrice = 250.00m, Unit = "Unit", State = ProductState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Product { Name = "Wood Varnish", Description = "Protective coating", CategoryId = categories[4].Id, SalesPrice = 55.00m, PurchasePrice = 45.00m, Unit = "Gallon", State = ProductState.Confirmed, ConfirmedDate = DateTime.UtcNow },
            new Product { Name = "Screws & Nails Kit", Description = "Hardware set", CategoryId = categories[3].Id, SalesPrice = 18.00m, PurchasePrice = 12.00m, Unit = "Kit", State = ProductState.Confirmed, ConfirmedDate = DateTime.UtcNow }
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

        // Seed Budgets with Budget Lines
        var budget1 = new Budget
        {
            Name = "Q1 2025 Multi-Department Budget",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 31),
            State = BudgetState.Draft,
            CreatedDate = DateTime.UtcNow
        };

        // Add budget lines for different departments
        budget1.Lines.Add(new BudgetLine
        {
            AnalyticalAccountId = analyticalAccounts[0].Id, // PROD-001
            Type = BudgetLineType.Expense,
            BudgetedAmount = 30000m
        });

        budget1.Lines.Add(new BudgetLine
        {
            AnalyticalAccountId = analyticalAccounts[1].Id, // MKT-001
            Type = BudgetLineType.Expense,
            BudgetedAmount = 15000m
        });

        budget1.Lines.Add(new BudgetLine
        {
            AnalyticalAccountId = analyticalAccounts[4].Id, // R&D-001
            Type = BudgetLineType.Expense,
            BudgetedAmount = 5000m
        });

        context.Budgets.Add(budget1);

        // Seed second budget - Expo Revenue
        var budget2 = new Budget
        {
            Name = "Furniture Expo 2025 - Revenue & Expense",
            StartDate = new DateTime(2025, 6, 1),
            EndDate = new DateTime(2025, 6, 30),
            State = BudgetState.Confirmed,
            CreatedDate = DateTime.UtcNow,
            ConfirmedDate = DateTime.UtcNow
        };

        // Income line
        budget2.Lines.Add(new BudgetLine
        {
            AnalyticalAccountId = analyticalAccounts[2].Id, // EXPO-2025
            Type = BudgetLineType.Income,
            BudgetedAmount = 100000m
        });

        // Expense line (same account, different type)
        budget2.Lines.Add(new BudgetLine
        {
            AnalyticalAccountId = analyticalAccounts[2].Id, // EXPO-2025
            Type = BudgetLineType.Expense,
            BudgetedAmount = 25000m
        });

        context.Budgets.Add(budget2);

        // Seed third budget - Office Project
        var budget3 = new Budget
        {
            Name = "Office Furniture Project - Annual Budget",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            State = BudgetState.Confirmed,
            CreatedDate = DateTime.UtcNow,
            ConfirmedDate = DateTime.UtcNow
        };

        budget3.Lines.Add(new BudgetLine
        {
            AnalyticalAccountId = analyticalAccounts[3].Id, // PROJ-OFF
            Type = BudgetLineType.Expense,
            BudgetedAmount = 75000m
        });

        context.Budgets.Add(budget3);

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
