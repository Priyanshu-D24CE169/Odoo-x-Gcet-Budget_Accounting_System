using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContactTag> ContactTags => Set<ContactTag>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AnalyticalAccount> AnalyticalAccounts => Set<AnalyticalAccount>();
    public DbSet<AutoAnalyticalModel> AutoAnalyticalModels => Set<AutoAnalyticalModel>();
    public DbSet<AnalyticalBudget> AnalyticalBudgets => Set<AnalyticalBudget>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<VendorBill> VendorBills => Set<VendorBill>();
    public DbSet<VendorBillLine> VendorBillLines => Set<VendorBillLine>();
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<CustomerInvoice> CustomerInvoices => Set<CustomerInvoice>();
    public DbSet<CustomerInvoiceLine> CustomerInvoiceLines => Set<CustomerInvoiceLine>();
    public DbSet<CustomerInvoicePayment> CustomerInvoicePayments => Set<CustomerInvoicePayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.LoginId)
                .HasMaxLength(12)
                .IsRequired();

            entity.HasIndex(x => x.LoginId)
                .IsUnique();

            entity.HasOne(x => x.Contact)
                .WithOne(c => c.PortalUser)
                .HasForeignKey<ApplicationUser>(x => x.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedOn)
                .HasColumnType("datetime2");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(x => x.OrderDate)
                .HasColumnType("datetime2");

            entity.Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(x => x.ContactId);

            entity.Property(x => x.CreatedOn)
                .HasColumnType("datetime2");

            entity.HasIndex(x => x.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(x => x.TagId);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ContactTag>(entity =>
        {
            entity.HasKey(x => new { x.ContactId, x.TagId });

            entity.HasOne(x => x.Contact)
                .WithMany(c => c.ContactTags)
                .HasForeignKey(x => x.ContactId);

            entity.HasOne(x => x.Tag)
                .WithMany(t => t.ContactTags)
                .HasForeignKey(x => x.TagId);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(x => x.ProductCategoryId);
            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.SalesPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.PurchasePrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.CreatedOn)
                .HasColumnType("datetime2");

            entity.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AnalyticalAccount>(entity =>
        {
            entity.HasKey(x => x.AnalyticalAccountId);
            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.CreatedOn)
                .HasColumnType("datetime2");

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<AutoAnalyticalModel>(entity =>
        {
            entity.HasKey(x => x.ModelId);
            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.Property(x => x.CreatedOn)
                .HasColumnType("datetime2");

            entity.HasOne(x => x.Partner)
                .WithMany()
                .HasForeignKey(x => x.PartnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PartnerTag)
                .WithMany()
                .HasForeignKey(x => x.PartnerTagId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProductCategory)
                .WithMany()
                .HasForeignKey(x => x.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(x => x.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.PartnerId, x.PartnerTagId, x.ProductId, x.ProductCategoryId, x.Status });
        });

        modelBuilder.Entity<AnalyticalBudget>(entity =>
        {
            entity.HasKey(x => x.AnalyticalBudgetId);
            entity.Property(x => x.BudgetName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(x => x.BudgetType)
                .HasConversion<int>();
            entity.Property(x => x.PeriodStart).HasColumnType("datetime2");
            entity.Property(x => x.PeriodEnd).HasColumnType("datetime2");
            entity.Property(x => x.LimitAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");
            entity.Property(x => x.IsReadOnly).HasDefaultValue(false);
            entity.Property(x => x.Status).HasConversion<int>();

            entity.HasOne(x => x.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(x => x.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.OriginalBudget)
                .WithMany(x => x.Revisions)
                .HasForeignKey(x => x.OriginalBudgetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.AnalyticalAccountId, x.PeriodStart, x.PeriodEnd });
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(x => x.PurchaseOrderId);
            entity.Property(x => x.PONumber)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(x => x.PONumber)
                .IsUnique();

            entity.Property(x => x.PODate).HasColumnType("datetime2");
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");
            entity.Property(x => x.ConfirmedOn).HasColumnType("datetime2");
            entity.Property(x => x.CancelledOn).HasColumnType("datetime2");
            entity.Property(x => x.Status).HasConversion<int>();

            entity.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(x => x.PurchaseOrderLineId);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Total).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.PurchaseOrder)
                .WithMany(o => o.Lines)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(x => x.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendorBill>(entity =>
        {
            entity.HasKey(x => x.VendorBillId);
            entity.Property(x => x.BillNumber)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(x => x.BillNumber).IsUnique();

            entity.Property(x => x.BillDate).HasColumnType("datetime2");
            entity.Property(x => x.DueDate).HasColumnType("datetime2");
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");
            entity.Property(x => x.ConfirmedOn).HasColumnType("datetime2");
            entity.Property(x => x.CancelledOn).HasColumnType("datetime2");
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.PaymentStatus).HasConversion<int>();
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PurchaseOrder)
                .WithMany()
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VendorBillLine>(entity =>
        {
            entity.HasKey(x => x.VendorBillLineId);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Total).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.VendorBill)
                .WithMany(b => b.Lines)
                .HasForeignKey(x => x.VendorBillId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(x => x.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BillPayment>(entity =>
        {
            entity.HasKey(x => x.BillPaymentId);
            entity.Property(x => x.PaymentNumber)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(x => x.PaymentNumber)
                .IsUnique();

            entity.Property(x => x.PaymentDate).HasColumnType("datetime2");
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaymentMode).HasConversion<int>();

            entity.HasOne(x => x.VendorBill)
                .WithMany(b => b.Payments)
                .HasForeignKey(x => x.VendorBillId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(x => x.SalesOrderId);
            entity.Property(x => x.SONumber)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(x => x.SONumber).IsUnique();

            entity.Property(x => x.SODate).HasColumnType("datetime2");
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");
            entity.Property(x => x.ConfirmedOn).HasColumnType("datetime2");
            entity.Property(x => x.CancelledOn).HasColumnType("datetime2");
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Reference).HasMaxLength(150);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.HasKey(x => x.SalesOrderLineId);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Total).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.SalesOrder)
                .WithMany(o => o.Lines)
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(x => x.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerInvoice>(entity =>
        {
            entity.HasKey(x => x.CustomerInvoiceId);
            entity.Property(x => x.InvoiceNumber)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(x => x.InvoiceNumber)
                .IsUnique();

            entity.Property(x => x.InvoiceDate).HasColumnType("datetime2");
            entity.Property(x => x.DueDate).HasColumnType("datetime2");
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");
            entity.Property(x => x.ConfirmedOn).HasColumnType("datetime2");
            entity.Property(x => x.CancelledOn).HasColumnType("datetime2");
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.PaymentStatus).HasConversion<int>();
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerInvoiceLine>(entity =>
        {
            entity.HasKey(x => x.CustomerInvoiceLineId);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Total).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.CustomerInvoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(x => x.CustomerInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(x => x.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerInvoicePayment>(entity =>
        {
            entity.HasKey(x => x.CustomerInvoicePaymentId);
            entity.Property(x => x.PaymentNumber)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(x => x.PaymentNumber)
                .IsUnique();

            entity.Property(x => x.PaymentDate).HasColumnType("datetime2");
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaymentMode).HasConversion<int>();
            entity.Property(x => x.CreatedOn).HasColumnType("datetime2");

            entity.HasOne(x => x.CustomerInvoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(x => x.CustomerInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
