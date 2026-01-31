# Quick Start Guide - Budget Accounting System

## ? Current Status

Your Budget Accounting System foundation is **complete and functional**!

### What's Been Built

1. **Database Schema**: 17 tables with proper relationships
2. **Business Models**: All domain entities (Contact, Product, Budget, etc.)
3. **Services**: Auto-assignment, Budget calculation, Payment reconciliation
4. **Sample Data**: Pre-seeded with realistic test data
5. **Home Page**: Dashboard with navigation to all modules

## ?? Running the Application

### Option 1: Visual Studio
1. Press **F5** or click the green "Run" button
2. Browser will open automatically to `https://localhost:5001`

### Option 2: Command Line
```bash
cd "Budget Accounting System"
dotnet run
```
Then navigate to: `https://localhost:5001`

## ?? What You'll See

### Home Page Dashboard
The home page shows 6 navigation cards:

1. **Master Data** (Blue)
   - Contacts
   - Products
   - Analytical Accounts

2. **Budgets** (Green)
   - Budgets
   - Budget Reports
   - Auto Models

3. **Purchases** (Yellow)
   - Purchase Orders
   - Vendor Bills

4. **Sales** (Cyan)
   - Sales Orders
   - Customer Invoices

5. **Payments** (Blue Card)
   - Manage Payments

6. **Customer Portal** (Gray Card)
   - Portal Login

### Current State
- All links are present in navigation
- Pages are **not yet created** (will show 404 errors)
- Database is ready with sample data

## ??? Viewing Your Database

### Option 1: Visual Studio SQL Server Object Explorer
1. View ? SQL Server Object Explorer
2. Expand: (localdb)\MSSQLLocalDB ? Databases ? BudgetAccountingDB
3. Explore tables and data

### Option 2: SQL Server Management Studio (SSMS)
Server: `(localdb)\MSSQLLocalDB`
Database: `BudgetAccountingDB`

### Sample Query to View Data
```sql
-- View all contacts
SELECT * FROM Contacts;

-- View products with categories
SELECT Name, Category, UnitPrice FROM Products;

-- View analytical accounts
SELECT Code, Name, Description FROM AnalyticalAccounts;

-- View budgets with details
SELECT b.Name, a.Name AS AnalyticalAccount, b.PlannedAmount, b.Type
FROM Budgets b
JOIN AnalyticalAccounts a ON b.AnalyticalAccountId = a.Id;

-- View auto-analytical rules
SELECT m.Name AS Model, r.Condition, r.ProductCategory, a.Name AS AssignTo
FROM AutoAnalyticalRules r
JOIN AutoAnalyticalModels m ON r.ModelId = m.Id
JOIN AnalyticalAccounts a ON r.AnalyticalAccountId = a.Id;
```

## ?? Sample Data Included

### Contacts (5 records)
- **ABC Suppliers Ltd** - Vendor
- **XYZ Timber Co** - Vendor
- **John's Furniture Store** - Customer
- **Modern Living Inc** - Customer
- **Office Solutions** - Both (Customer & Vendor)

### Products (6 records)
- **Oak Wood Plank** - Wood category - $25/sq ft
- **Pine Wood Sheet** - Wood category - $15/sq ft
- **Office Desk** - Furniture category - $350/unit
- **Executive Chair** - Furniture category - $250/unit
- **Wood Varnish** - Materials category - $45/gallon
- **Screws & Nails Kit** - Materials category - $12/kit

### Analytical Accounts (5 records)
- **PROD-001**: Production Department
- **MKT-001**: Marketing Department
- **EXPO-2025**: Furniture Expo 2025
- **PROJ-OFF**: Office Furniture Project
- **R&D-001**: Research & Development

### Budgets (4 records)
- **Q1 2025 Production Budget** - $50,000 (Expense)
- **Q1 2025 Marketing Budget** - $15,000 (Expense)
- **Furniture Expo Revenue Target** - $100,000 (Income)
- **Office Project Budget** - $75,000 (Expense)

### Auto-Analytical Rules (3 rules)
- **Wood products** ? Production Department
- **Furniture products** ? Office Furniture Project
- **Materials products** ? Production Department

## ?? Next Development Steps

### Priority 1: Master Data CRUD (Week 1)

Start with these pages in order:

1. **Contacts Module** (Easiest)
   ```
   Pages/Contacts/
   ??? Index.cshtml        (List view with search)
   ??? Create.cshtml       (Add new contact)
   ??? Edit.cshtml         (Edit existing)
   ??? Details.cshtml      (View details)
   ??? Delete.cshtml       (Delete confirmation)
   ```

2. **Products Module**
   - Similar structure to Contacts
   - Add category dropdown
   - Price formatting

3. **Analytical Accounts Module**
   - Tree view for hierarchy
   - Parent selection dropdown

4. **Budgets Module**
   - Date range pickers
   - Amount formatting
   - Type selection (Income/Expense)
   - Analytical account dropdown

5. **Auto Analytical Models Module**
   - Manage models and rules
   - Rule condition selection
   - Priority ordering

### Priority 2: Transaction CRUD (Week 2)

6. Purchase Orders (with line items)
7. Vendor Bills (linking to POs)
8. Sales Orders (with line items)
9. Customer Invoices (linking to SOs)
10. Payments (with reconciliation)

### Priority 3: Reporting (Week 3)

11. Budget vs Actual reports
12. Dashboard with charts
13. Achievement visualization

### Priority 4: Customer Portal (Week 4)

14. Authentication
15. Customer views
16. Payment integration

## ?? Development Tips

### Creating CRUD Pages - Example Pattern

For each entity, follow this pattern:

**Index.cshtml.cs** (List)
```csharp
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public IList<Contact> Contacts { get; set; }
    
    public async Task OnGetAsync()
    {
        Contacts = await _context.Contacts
            .Where(c => c.IsActive)
            .ToListAsync();
    }
}
```

**Create.cshtml.cs** (Add New)
```csharp
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    [BindProperty]
    public Contact Contact { get; set; }
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();
            
        _context.Contacts.Add(Contact);
        await _context.SaveChangesAsync();
        
        return RedirectToPage("./Index");
    }
}
```

### Useful EF Core Queries

```csharp
// Get all with related data
var orders = await _context.PurchaseOrders
    .Include(o => o.Vendor)
    .Include(o => o.Lines)
        .ThenInclude(l => l.Product)
    .ToListAsync();

// Filter and search
var results = await _context.Products
    .Where(p => p.IsActive && p.Category == "Wood")
    .OrderBy(p => p.Name)
    .ToListAsync();

// Calculate totals
var total = await _context.VendorBills
    .Where(b => b.Status == BillStatus.Posted)
    .SumAsync(b => b.TotalAmount);
```

## ?? Troubleshooting

### Issue: Database not found
**Solution**: Run `dotnet ef database update` in the project directory

### Issue: Build errors
**Solution**: 
```bash
dotnet restore
dotnet build
```

### Issue: Port already in use
**Solution**: Change port in `Properties/launchSettings.json`

### Issue: Sample data not appearing
**Solution**: Check that DataSeeder is being called in Program.cs

## ?? Resources

- **EF Core Docs**: https://docs.microsoft.com/ef/core/
- **Razor Pages Guide**: https://docs.microsoft.com/aspnet/core/razor-pages/
- **Bootstrap 5**: https://getbootstrap.com/docs/5.0/

## ?? Key Concepts to Understand

1. **Analytical Accounts**: Track WHERE money is spent (cost centers)
2. **Budget vs Actual**: Compare planned vs real transactions
3. **Auto-Assignment**: Automatic cost center allocation based on rules
4. **Payment Reconciliation**: Matching payments to invoices/bills

## ? Success Criteria

Your system is ready when you can:
- ? Run the application without errors
- ? View the home page dashboard
- ? Connect to the database
- ? Query sample data
- [ ] Create/Edit/Delete contacts
- [ ] Create/Edit/Delete products
- [ ] Create budgets and view reports
- [ ] Record transactions and track budgets

---

**Current Status**: Foundation Complete ?  
**Next Step**: Build Contacts CRUD pages  
**Target**: Full system in 4 weeks  

Good luck with your hackathon! ??
