using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<AnalyticalAccount> AnalyticalAccounts { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<BudgetRevision> BudgetRevisions { get; set; }
    public DbSet<AutoAnalyticalModel> AutoAnalyticalModels { get; set; }
    public DbSet<AutoAnalyticalRule> AutoAnalyticalRules { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<VendorBill> VendorBills { get; set; }
    public DbSet<VendorBillLine> VendorBillLines { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<SalesOrderLine> SalesOrderLines { get; set; }
    public DbSet<CustomerInvoice> CustomerInvoices { get; set; }
    public DbSet<CustomerInvoiceLine> CustomerInvoiceLines { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ApplicationUser and Contact relationship
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Contact)
                .WithOne(c => c.User)
                .HasForeignKey<ApplicationUser>(u => u.ContactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<AnalyticalAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Code).IsUnique();
            
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PlannedAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany(e => e.Budgets)
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetRevision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OldAmount).HasPrecision(18, 2);
            entity.Property(e => e.NewAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Budget)
                .WithMany(e => e.Revisions)
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AutoAnalyticalModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<AutoAnalyticalRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Model)
                .WithMany(e => e.Rules)
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigurePurchaseOrders(modelBuilder);
        ConfigureVendorBills(modelBuilder);
        ConfigureSalesOrders(modelBuilder);
        ConfigureCustomerInvoices(modelBuilder);
        ConfigurePayments(modelBuilder);
    }

    private void ConfigurePurchaseOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PONumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.PONumber).IsUnique();
            
            entity.HasOne(e => e.Vendor)
                .WithMany(e => e.PurchaseOrders)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.PurchaseOrderLines)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany(e => e.PurchaseOrderLines)
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureVendorBills(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VendorBill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BillNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.BillNumber).IsUnique();
            
            entity.HasOne(e => e.Vendor)
                .WithMany(e => e.VendorBills)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(e => e.VendorBills)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendorBillLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.VendorBill)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.VendorBillLines)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany(e => e.VendorBillLines)
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSalesOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SONumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.SONumber).IsUnique();
            
            entity.HasOne(e => e.Customer)
                .WithMany(e => e.SalesOrders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.SalesOrder)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.SalesOrderLines)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany(e => e.SalesOrderLines)
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCustomerInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            
            entity.HasOne(e => e.Customer)
                .WithMany(e => e.CustomerInvoices)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.SalesOrder)
                .WithMany(e => e.CustomerInvoices)
                .HasForeignKey(e => e.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.CustomerInvoice)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.CustomerInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.CustomerInvoiceLines)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany(e => e.CustomerInvoiceLines)
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            
            entity.HasOne(e => e.CustomerInvoice)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.CustomerInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.VendorBill)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
