# Budget Accounting System - Shiv Furniture

A comprehensive Budget Accounting System built with .NET 10 and Razor Pages for managing purchases, sales, payments, and budget monitoring with cost-center tracking.

## Features

### 1. Master Data Management
- **Contacts**: Manage customers and vendors
- **Products**: Product catalog with categories and pricing
- **Analytical Accounts (Cost Centers)**: Hierarchical cost center structure for tracking expenses by activity/project
- **Budgets**: Define budgets for specific periods linked to analytical accounts
- **Auto Analytical Models**: Automated rules to assign cost centers to transactions

### 2. Transaction Processing
- **Purchase Orders**: Create and track purchase orders with vendors
- **Vendor Bills**: Record vendor bills and link to purchase orders
- **Sales Orders**: Create sales orders for customers
- **Customer Invoices**: Generate invoices and link to sales orders
- **Payments**: Record payments and automatically update invoice/bill status

### 3. Budget Monitoring
- **Budget vs Actual Computation**: Real-time comparison of planned vs actual amounts
- **Achievement Percentage**: Track budget utilization percentage
- **Remaining Balance**: Monitor available budget
- **Budget Revisions**: Track budget changes with revision history
- **Budget Reports**: Comprehensive reports and charts

### 4. Customer Portal (Planned)
- View invoices and bills
- Download documents
- Make online payments
- View sales/purchase orders

## Technology Stack

- **.NET 10**: Latest .NET framework
- **ASP.NET Core Razor Pages**: Server-side rendering with Razor
- **Entity Framework Core 10**: ORM for database access
- **SQL Server**: Database engine (LocalDB for development)
- **Bootstrap 5**: Responsive UI framework

## Project Structure

```
Budget Accounting System/
??? Data/
?   ??? ApplicationDbContext.cs       # EF Core DbContext
??? Models/
?   ??? Contact.cs                    # Customer/Vendor model
?   ??? Product.cs                    # Product catalog model
?   ??? AnalyticalAccount.cs          # Cost center model
?   ??? Budget.cs                     # Budget and revision models
?   ??? AutoAnalyticalModel.cs        # Auto-assignment rules
?   ??? PurchaseOrder.cs              # PO and line items
?   ??? VendorBill.cs                 # Vendor bill and lines
?   ??? SalesOrder.cs                 # SO and line items
?   ??? CustomerInvoice.cs            # Invoice and lines
?   ??? Payment.cs                    # Payment transactions
??? Services/
?   ??? AnalyticalAccountService.cs   # Auto-assignment logic
?   ??? BudgetService.cs              # Budget calculations
?   ??? PaymentService.cs             # Payment reconciliation
??? Pages/
?   ??? Index.cshtml                  # Home page
?   ??? Error.cshtml                  # Error page
?   ??? Shared/
?   ?   ??? _Layout.cshtml            # Shared layout
?   ??? _ViewImports.cshtml           # Global imports
?   ??? _ViewStart.cshtml             # Layout selection
??? Program.cs                        # Application startup

```

## Database Schema

### Core Entities

1. **Contact**: Customers and vendors with type classification
2. **Product**: Product catalog with categories and pricing
3. **AnalyticalAccount**: Hierarchical cost centers for expense tracking
4. **Budget**: Budget definitions with start/end dates and planned amounts
5. **BudgetRevision**: Audit trail for budget changes
6. **AutoAnalyticalModel**: Rule-based auto-assignment models
7. **AutoAnalyticalRule**: Individual rules (by product, category, customer, vendor)

### Transaction Entities

8. **PurchaseOrder / PurchaseOrderLine**: Purchase orders with line items
9. **VendorBill / VendorBillLine**: Vendor bills linked to POs
10. **SalesOrder / SalesOrderLine**: Sales orders with line items
11. **CustomerInvoice / CustomerInvoiceLine**: Invoices linked to SOs
12. **Payment**: Payment records with reconciliation to invoices/bills

## Setup Instructions

### Prerequisites

- .NET 10 SDK installed
- SQL Server LocalDB (included with Visual Studio) or SQL Server
- Visual Studio 2022 or VS Code

### Installation Steps

1. **Clone or open the project**
   ```bash
   cd "Budget Accounting System"
   ```

2. **Update database connection string** (if needed)
   Edit `appsettings.json` to configure your SQL Server connection.

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   Navigate to `https://localhost:5001` or the port shown in the terminal.

## Key Business Logic

### Auto Analytical Account Assignment

The system automatically assigns cost centers to transaction lines based on configurable rules:

- **Product-based**: Specific products or product categories
- **Contact-based**: Specific customers or vendors
- **Priority-based**: Rules are evaluated in priority order

### Payment Reconciliation

Payment status is automatically calculated:

```
- Paid: Total paid >= Invoice total
- Partially Paid: Total paid > 0 and < Invoice total
- Not Paid: Total paid = 0
```

### Budget vs Actual Calculation

- **Income Budgets**: Actuals from posted Customer Invoices
- **Expense Budgets**: Actuals from posted Vendor Bills
- **Achievement %**: (Actual / Planned) × 100
- **Variance**: Planned - Actual
- **Remaining Balance**: Planned - Actual

## Next Steps

### Phase 1: Master Data CRUD (Current)
- [ ] Create Contacts CRUD pages
- [ ] Create Products CRUD pages
- [ ] Create Analytical Accounts CRUD pages
- [ ] Create Budgets CRUD pages
- [ ] Create Auto Analytical Models CRUD pages

### Phase 2: Transactions
- [ ] Purchase Order creation and management
- [ ] Vendor Bill creation with PO linking
- [ ] Sales Order creation and management
- [ ] Customer Invoice creation with SO linking
- [ ] Payment recording and reconciliation

### Phase 3: Budget Monitoring
- [ ] Budget vs Actual reports
- [ ] Budget achievement dashboard
- [ ] Charts and visualizations
- [ ] Budget revision tracking

### Phase 4: Customer Portal
- [ ] User authentication
- [ ] Invoice viewing
- [ ] Document downloads
- [ ] Online payment integration

## Contributing

This is a hackathon project for Shiv Furniture's Budget Accounting System.

## License

Proprietary - Shiv Furniture

---

**Built with ?? using .NET 10 and Razor Pages**
