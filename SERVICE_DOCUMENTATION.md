# Service Layer Documentation

## Overview

The Budget Accounting System implements a clean service layer architecture with three core services handling business logic:

1. **AnalyticalAccountService** - Auto-assignment of cost centers
2. **BudgetService** - Budget vs Actual calculations
3. **PaymentService** - Payment reconciliation

---

## 1. AnalyticalAccountService

### Interface: `IAnalyticalAccountService`

Handles automatic assignment of analytical accounts (cost centers) to transaction lines based on configurable rules.

### Methods

#### `GetAnalyticalAccountForTransaction`

```csharp
Task<int?> GetAnalyticalAccountForTransaction(
    int productId, 
    int? contactId, 
    string? productCategory)
```

**Purpose**: Finds the appropriate analytical account for a transaction based on auto-assignment rules.

**Parameters**:
- `productId` - The product involved in the transaction
- `contactId` - The customer or vendor (optional)
- `productCategory` - The product's category (optional)

**Returns**: `int?` - The ID of the matching analytical account, or null if no rules match

**Logic**:
1. Retrieves all active auto-analytical models ordered by priority
2. Evaluates rules in priority order
3. Checks each rule condition type:
   - **SpecificProduct**: Matches exact product ID
   - **ProductCategory**: Matches product category (case-insensitive)
   - **Customer/Vendor**: Matches contact ID
4. Returns the first matching analytical account ID

**Example Usage**:
```csharp
var analyticalAccountId = await _service.GetAnalyticalAccountForTransaction(
    productId: 5,
    contactId: 10,
    productCategory: "Wood"
);

if (analyticalAccountId.HasValue)
{
    // Assign to transaction line
    transactionLine.AnalyticalAccountId = analyticalAccountId.Value;
}
```

---

#### `ApplyAutoAnalyticalRules`

```csharp
Task ApplyAutoAnalyticalRules(int transactionLineId, string transactionType)
```

**Purpose**: Automatically applies analytical account assignment to a transaction line.

**Parameters**:
- `transactionLineId` - The ID of the transaction line
- `transactionType` - Type: "purchaseorder", "vendorbill", "salesorder", "customerinvoice"

**Returns**: `Task` (void)

**Logic**:
1. Loads the transaction line with related entities
2. Checks if analytical account is already assigned
3. If not assigned, calls `GetAnalyticalAccountForTransaction`
4. Updates the line with the found analytical account
5. Saves changes to database

**Example Usage**:
```csharp
// After creating a purchase order line
await _analyticalAccountService.ApplyAutoAnalyticalRules(
    transactionLineId: newLine.Id,
    transactionType: "purchaseorder"
);
```

**Supported Transaction Types**:
- `"purchaseorder"` - PurchaseOrderLine
- `"vendorbill"` - VendorBillLine
- `"salesorder"` - SalesOrderLine
- `"customerinvoice"` - CustomerInvoiceLine

---

## 2. BudgetService

### Interface: `IBudgetService`

Handles all budget vs actual calculations, achievement tracking, and budget analysis.

### Methods

#### `GetBudgetAnalysis`

```csharp
Task<BudgetAnalysisResult> GetBudgetAnalysis(int budgetId)
```

**Purpose**: Analyzes a specific budget and calculates actuals, variance, and achievement.

**Parameters**:
- `budgetId` - The ID of the budget to analyze

**Returns**: `BudgetAnalysisResult` containing:
- `PlannedAmount` - Budgeted amount
- `ActualAmount` - Actual income/expenses from posted transactions
- `Variance` - Planned - Actual
- `AchievementPercentage` - (Actual / Planned) × 100
- `RemainingBalance` - Planned - Actual
- `RevisionCount` - Number of budget revisions

**Calculation Logic**:
- For **Income Budgets**: Sums posted CustomerInvoice lines
- For **Expense Budgets**: Sums posted VendorBill lines
- Filters by analytical account and date range
- Only includes transactions with `Status = Posted`

**Example Usage**:
```csharp
var analysis = await _budgetService.GetBudgetAnalysis(budgetId: 1);

Console.WriteLine($"Budget: {analysis.BudgetName}");
Console.WriteLine($"Planned: ${analysis.PlannedAmount:N2}");
Console.WriteLine($"Actual: ${analysis.ActualAmount:N2}");
Console.WriteLine($"Achievement: {analysis.AchievementPercentage:F1}%");
Console.WriteLine($"Remaining: ${analysis.RemainingBalance:N2}");
```

---

#### `GetBudgetAnalysisByPeriod`

```csharp
Task<List<BudgetAnalysisResult>> GetBudgetAnalysisByPeriod(
    DateTime startDate, 
    DateTime endDate)
```

**Purpose**: Analyzes all budgets active in a given period.

**Parameters**:
- `startDate` - Period start date
- `endDate` - Period end date

**Returns**: `List<BudgetAnalysisResult>` for all active budgets overlapping the period

**Logic**:
1. Finds all active budgets where:
   - Budget.StartDate <= endDate
   - Budget.EndDate >= startDate
2. Calculates analysis for each budget
3. Returns list of results

**Example Usage**:
```csharp
var q1Results = await _budgetService.GetBudgetAnalysisByPeriod(
    startDate: new DateTime(2025, 1, 1),
    endDate: new DateTime(2025, 3, 31)
);

foreach (var result in q1Results)
{
    Console.WriteLine($"{result.BudgetName}: {result.AchievementPercentage:F1}% achieved");
}
```

---

#### `GetBudgetAnalysisByAnalyticalAccount`

```csharp
Task<BudgetAnalysisResult> GetBudgetAnalysisByAnalyticalAccount(
    int analyticalAccountId, 
    DateTime startDate, 
    DateTime endDate)
```

**Purpose**: Aggregates all budgets for a specific cost center in a period.

**Parameters**:
- `analyticalAccountId` - The cost center to analyze
- `startDate` - Period start
- `endDate` - Period end

**Returns**: `BudgetAnalysisResult` with aggregated data

**Logic**:
1. Finds all budgets for the analytical account in the period
2. Sums planned amounts
3. Calculates income and expense actuals separately
4. Returns combined analysis

**Example Usage**:
```csharp
var prodAnalysis = await _budgetService.GetBudgetAnalysisByAnalyticalAccount(
    analyticalAccountId: 1, // Production Department
    startDate: new DateTime(2025, 1, 1),
    endDate: new DateTime(2025, 12, 31)
);

Console.WriteLine($"Department: {prodAnalysis.AnalyticalAccountName}");
Console.WriteLine($"Total Budget: ${prodAnalysis.PlannedAmount:N2}");
Console.WriteLine($"Total Spent: ${prodAnalysis.ActualAmount:N2}");
```

---

### BudgetAnalysisResult Class

```csharp
public class BudgetAnalysisResult
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; }
    public string AnalyticalAccountName { get; set; }
    public BudgetType BudgetType { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal AchievementPercentage { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RevisionCount { get; set; }
}
```

**Key Metrics**:
- **Variance**: Positive = Under budget, Negative = Over budget
- **Achievement %**: >100% means exceeded budget (good for income, bad for expenses)
- **Remaining Balance**: Available budget left

---

## 3. PaymentService

### Interface: `IPaymentService`

Handles payment recording and automatic reconciliation with invoices/bills.

### Methods

#### `RecordPayment`

```csharp
Task RecordPayment(Payment payment)
```

**Purpose**: Records a new payment and updates the related invoice/bill payment status.

**Parameters**:
- `payment` - Payment object with:
  - `Amount` - Payment amount
  - `PaymentDate` - Date of payment
  - `Type` - Received (from customer) or Paid (to vendor)
  - `Method` - Cash, BankTransfer, Check, etc.
  - `CustomerInvoiceId` or `VendorBillId` - Related document

**Returns**: `Task` (void)

**Logic**:
1. Adds payment to database
2. Saves payment record
3. Calls `UpdatePaymentStatus` to recalculate status

**Example Usage**:
```csharp
var payment = new Payment
{
    PaymentNumber = "PAY-2025-001",
    Amount = 500.00m,
    PaymentDate = DateTime.UtcNow,
    Type = PaymentType.Received,
    Method = PaymentMethod.BankTransfer,
    CustomerInvoiceId = 5,
    Reference = "Bank Transfer #123456"
};

await _paymentService.RecordPayment(payment);
// Invoice #5 payment status automatically updated
```

---

#### `UpdatePaymentStatus`

```csharp
Task UpdatePaymentStatus(int? invoiceId, int? billId)
```

**Purpose**: Recalculates and updates payment status for an invoice or bill.

**Parameters**:
- `invoiceId` - Customer invoice ID (optional)
- `billId` - Vendor bill ID (optional)
- *Note: Provide one or the other, not both*

**Returns**: `Task` (void)

**Logic**:

**For Customer Invoices**:
1. Loads invoice with all related payments
2. Calculates `TotalPaid = Sum(Payments.Amount)`
3. Updates `Invoice.PaidAmount`
4. Sets payment status:
   - `Paid`: TotalPaid >= TotalAmount
   - `PartiallyPaid`: 0 < TotalPaid < TotalAmount
   - `NotPaid`: TotalPaid = 0
5. If fully paid, sets `Status = InvoiceStatus.Paid`

**For Vendor Bills** (same logic):
1-5. Same as invoices but for bills

**Example Usage**:
```csharp
// After recording a payment, manually trigger update:
await _paymentService.UpdatePaymentStatus(invoiceId: 5, billId: null);

// Or for a vendor bill:
await _paymentService.UpdatePaymentStatus(invoiceId: null, billId: 10);
```

**Automatic Triggering**:
- Called automatically by `RecordPayment`
- Can be called manually after payment edits/deletions

---

## Service Registration

Services are registered in `Program.cs` with scoped lifetime:

```csharp
builder.Services.AddScoped<IAnalyticalAccountService, AnalyticalAccountService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
```

**Scoped Lifetime**: New instance per HTTP request, shared across all classes in that request.

---

## Usage in Razor Pages

### Dependency Injection

```csharp
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAnalyticalAccountService _analyticalService;
    private readonly IBudgetService _budgetService;
    private readonly IPaymentService _paymentService;

    public CreateModel(
        ApplicationDbContext context,
        IAnalyticalAccountService analyticalService,
        IBudgetService budgetService,
        IPaymentService paymentService)
    {
        _context = context;
        _analyticalService = analyticalService;
        _budgetService = budgetService;
        _paymentService = paymentService;
    }
}
```

---

## Example Workflows

### Workflow 1: Create Purchase Order with Auto-Assignment

```csharp
public async Task<IActionResult> OnPostAsync()
{
    // 1. Create purchase order
    var po = new PurchaseOrder
    {
        OrderNumber = "PO-2025-001",
        VendorId = 1,
        OrderDate = DateTime.UtcNow,
        Status = OrderStatus.Draft
    };
    _context.PurchaseOrders.Add(po);
    await _context.SaveChangesAsync();

    // 2. Add lines
    var line = new PurchaseOrderLine
    {
        PurchaseOrderId = po.Id,
        ProductId = 5,
        Quantity = 100,
        UnitPrice = 25.00m,
        SubTotal = 2500.00m
    };
    _context.PurchaseOrderLines.Add(line);
    await _context.SaveChangesAsync();

    // 3. Auto-assign analytical account
    await _analyticalService.ApplyAutoAnalyticalRules(
        line.Id, 
        "purchaseorder"
    );

    return RedirectToPage("./Index");
}
```

---

### Workflow 2: Post Invoice and Check Budget Impact

```csharp
public async Task<IActionResult> OnPostPostInvoice(int id)
{
    var invoice = await _context.CustomerInvoices.FindAsync(id);
    if (invoice == null)
        return NotFound();

    // 1. Post the invoice
    invoice.Status = InvoiceStatus.Posted;
    await _context.SaveChangesAsync();

    // 2. Check budget impact for this analytical account
    var lines = await _context.CustomerInvoiceLines
        .Where(l => l.CustomerInvoiceId == id && l.AnalyticalAccountId != null)
        .ToListAsync();

    foreach (var line in lines)
    {
        var budgets = await _context.Budgets
            .Where(b => b.AnalyticalAccountId == line.AnalyticalAccountId
                     && b.Type == BudgetType.Income
                     && b.StartDate <= invoice.InvoiceDate
                     && b.EndDate >= invoice.InvoiceDate)
            .ToListAsync();

        foreach (var budget in budgets)
        {
            var analysis = await _budgetService.GetBudgetAnalysis(budget.Id);
            
            if (analysis.AchievementPercentage > 90)
            {
                TempData["BudgetAlert"] = $"Budget {budget.Name} is at {analysis.AchievementPercentage:F1}% achievement!";
            }
        }
    }

    return RedirectToPage("./Index");
}
```

---

### Workflow 3: Record Payment and Check Bill Status

```csharp
public async Task<IActionResult> OnPostRecordPayment()
{
    var payment = new Payment
    {
        PaymentNumber = $"PAY-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
        Amount = PaymentAmount,
        PaymentDate = PaymentDate,
        Type = PaymentType.Paid,
        Method = PaymentMethod,
        VendorBillId = BillId,
        Reference = Reference,
        Notes = Notes
    };

    // Record payment - automatically updates bill status
    await _paymentService.RecordPayment(payment);

    // Check if bill is now fully paid
    var bill = await _context.VendorBills.FindAsync(BillId);
    if (bill.PaymentStatus == PaymentStatus.Paid)
    {
        TempData["Success"] = $"Bill {bill.BillNumber} has been fully paid!";
    }
    else if (bill.PaymentStatus == PaymentStatus.PartiallyPaid)
    {
        var remaining = bill.TotalAmount - bill.PaidAmount;
        TempData["Info"] = $"Partial payment recorded. Remaining: ${remaining:N2}";
    }

    return RedirectToPage("./Index");
}
```

---

## Testing the Services

### Unit Test Example (using xUnit and Moq)

```csharp
public class BudgetServiceTests
{
    [Fact]
    public async Task GetBudgetAnalysis_CalculatesCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        using var context = new ApplicationDbContext(options);
        var service = new BudgetService(context);

        // Create test data
        var account = new AnalyticalAccount { Code = "TEST", Name = "Test" };
        context.AnalyticalAccounts.Add(account);
        await context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            AnalyticalAccountId = account.Id,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 31),
            PlannedAmount = 10000m,
            Type = BudgetType.Expense
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBudgetAnalysis(budget.Id);

        // Assert
        Assert.Equal(10000m, result.PlannedAmount);
        Assert.Equal("Test Budget", result.BudgetName);
    }
}
```

---

## Performance Considerations

### Analytical Account Service
- Uses `.Include()` to eager load related entities
- Evaluates rules in memory after loading (considers adding database-level filtering for large rule sets)

### Budget Service
- Queries only posted transactions for actuals
- Uses indexes on Status, Date, and AnalyticalAccountId
- Consider caching budget analysis results for frequently accessed budgets

### Payment Service
- Updates happen in single transaction
- Uses `.Include()` to load payments in one query
- Payment status calculation is fast (simple aggregation)

---

## Future Enhancements

### Potential Service Additions

1. **ReportingService**
   - Generate PDF invoices
   - Export budget reports to Excel
   - Dashboard data aggregation

2. **NotificationService**
   - Email alerts for budget thresholds
   - Payment reminders
   - Order confirmations

3. **AuditService**
   - Track all changes to transactions
   - User activity logging
   - Change history

4. **ValidationService**
   - Pre-save validation rules
   - Business rule enforcement
   - Data integrity checks

---

**Service Layer Complete**: All core business logic is encapsulated in services, making the system maintainable and testable.
