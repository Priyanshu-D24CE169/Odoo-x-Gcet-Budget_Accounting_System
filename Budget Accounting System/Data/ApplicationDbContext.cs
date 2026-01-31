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
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ContactTag> ContactTags { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<AnalyticalAccount> AnalyticalAccounts { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<BudgetLine> BudgetLines { get; set; }
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
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Street).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateName).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Pincode).HasMaxLength(20);
        });

        // Configure Tag and ContactTag (many-to-many)
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ContactTag>(entity =>
        {
            entity.HasKey(ct => new { ct.ContactId, ct.TagId });
            
            entity.HasOne(ct => ct.Contact)
                .WithMany(c => c.ContactTags)
                .HasForeignKey(ct => ct.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(ct => ct.Tag)
                .WithMany(t => t.ContactTags)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SalesPrice).HasPrecision(18, 2);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.Unit).HasMaxLength(50);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
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
            
            // Revision tracking - self-referencing relationships
            entity.HasOne(e => e.RevisedFrom)
                .WithOne(e => e.RevisedWith)
                .HasForeignKey<Budget>(e => e.RevisedFromId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BudgetedAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Budget)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.AnalyticalAccount)
                .WithMany()
                .HasForeignKey(e => e.AnalyticalAccountId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Allow one analytical account to appear twice (once for Income, once for Expense)
            entity.HasIndex(e => new { e.BudgetId, e.AnalyticalAccountId, e.Type })
                .IsUnique();
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

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PONumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            
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
        });

        modelBuilder.Entity<VendorBill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BillNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Vendor)
                .WithMany(e => e.VendorBills)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(e => e.VendorBills)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VendorBillLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SONumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            
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
        });

        modelBuilder.Entity<CustomerInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Customer)
                .WithMany(e => e.CustomerInvoices)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.SalesOrder)
                .WithMany(e => e.CustomerInvoices)
                .HasForeignKey(e => e.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CustomerInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.VendorBill)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.CustomerInvoice)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.CustomerInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Override SaveChanges to auto-update ModifiedDate
    /// </summary>
    public override int SaveChanges()
    {
        AutoUpdateModifiedDate();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to auto-update ModifiedDate
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AutoUpdateModifiedDate();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically update ModifiedDate for all modified entities
    /// </summary>
    private void AutoUpdateModifiedDate()
    {
        var entries = ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is Contact contact)
                {
                    contact.UpdatedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is Product product)
                {
                    product.UpdatedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is VendorBill bill)
                {
                    bill.ModifiedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is CustomerInvoice invoice)
                {
                    invoice.ModifiedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is PurchaseOrder po)
                {
                    po.ModifiedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is SalesOrder so)
                {
                    so.ModifiedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is AnalyticalAccount account)
                {
                    account.ModifiedDate = DateTime.UtcNow;
                }
                // Budget no longer has ModifiedDate - uses state-specific dates instead
            }
        }
    }
}

