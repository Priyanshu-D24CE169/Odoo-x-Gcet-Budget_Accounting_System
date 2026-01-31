using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Extensions;

/// <summary>
/// Extension methods for automatic data synchronization across models
/// </summary>
public static class ModelSyncExtensions
{
    /// <summary>
    /// Updates payment status based on paid amount vs total amount
    /// </summary>
    public static void UpdatePaymentStatus(this VendorBill bill)
    {
        if (bill.PaidAmount <= 0)
        {
            bill.PaymentStatus = PaymentStatus.NotPaid;
        }
        else if (bill.PaidAmount >= bill.TotalAmount)
        {
            bill.PaymentStatus = PaymentStatus.Paid;
        }
        else
        {
            bill.PaymentStatus = PaymentStatus.Partial;
        }
        
        bill.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates payment status based on paid amount vs total amount
    /// </summary>
    public static void UpdatePaymentStatus(this CustomerInvoice invoice)
    {
        if (invoice.PaidAmount <= 0)
        {
            invoice.PaymentStatus = PaymentStatus.NotPaid;
        }
        else if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.PaymentStatus = PaymentStatus.Paid;
        }
        else
        {
            invoice.PaymentStatus = PaymentStatus.Partial;
        }
        
        invoice.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates total from all lines
    /// </summary>
    public static void RecalculateTotals(this VendorBill bill)
    {
        bill.TotalAmount = bill.Lines.Sum(l => l.LineTotal);
        bill.UpdatePaymentStatus();
        bill.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates total from all lines
    /// </summary>
    public static void RecalculateTotals(this CustomerInvoice invoice)
    {
        invoice.TotalAmount = invoice.Lines.Sum(l => l.LineTotal);
        invoice.UpdatePaymentStatus();
        invoice.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates total from all lines
    /// </summary>
    public static void RecalculateTotals(this PurchaseOrder po)
    {
        po.TotalAmount = po.Lines.Sum(l => l.LineTotal);
        po.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates total from all lines
    /// </summary>
    public static void RecalculateTotals(this SalesOrder so)
    {
        so.TotalAmount = so.Lines.Sum(l => l.LineTotal);
        so.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates line total from quantity and unit price
    /// </summary>
    public static void CalculateLineTotal(this VendorBillLine line)
    {
        line.LineTotal = line.Quantity * line.UnitPrice;
    }

    /// <summary>
    /// Calculates line total from quantity and unit price
    /// </summary>
    public static void CalculateLineTotal(this CustomerInvoiceLine line)
    {
        line.LineTotal = line.Quantity * line.UnitPrice;
    }

    /// <summary>
    /// Calculates line total from quantity and unit price
    /// </summary>
    public static void CalculateLineTotal(this PurchaseOrderLine line)
    {
        line.LineTotal = line.Quantity * line.UnitPrice;
    }

    /// <summary>
    /// Calculates line total from quantity and unit price
    /// </summary>
    public static void CalculateLineTotal(this SalesOrderLine line)
    {
        line.LineTotal = line.Quantity * line.UnitPrice;
    }

    /// <summary>
    /// Records a payment and updates the bill/invoice
    /// </summary>
    public static void RecordPayment(this VendorBill bill, decimal amount)
    {
        bill.PaidAmount += amount;
        bill.UpdatePaymentStatus();
        bill.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a payment and updates the invoice
    /// </summary>
    public static void RecordPayment(this CustomerInvoice invoice, decimal amount)
    {
        invoice.PaidAmount += amount;
        invoice.UpdatePaymentStatus();
        invoice.ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets outstanding balance
    /// </summary>
    public static decimal GetOutstandingBalance(this VendorBill bill)
    {
        return bill.TotalAmount - bill.PaidAmount;
    }

    /// <summary>
    /// Gets outstanding balance
    /// </summary>
    public static decimal GetOutstandingBalance(this CustomerInvoice invoice)
    {
        return invoice.TotalAmount - invoice.PaidAmount;
    }

    /// <summary>
    /// Checks if payment is overdue
    /// </summary>
    public static bool IsOverdue(this VendorBill bill)
    {
        return bill.DueDate.HasValue && 
               bill.DueDate.Value < DateTime.Today && 
               bill.PaymentStatus != PaymentStatus.Paid;
    }

    /// <summary>
    /// Checks if payment is overdue
    /// </summary>
    public static bool IsOverdue(this CustomerInvoice invoice)
    {
        return invoice.DueDate.HasValue && 
               invoice.DueDate.Value < DateTime.Today && 
               invoice.PaymentStatus != PaymentStatus.Paid;
    }

    /// <summary>
    /// Gets days until due (negative if overdue)
    /// </summary>
    public static int GetDaysUntilDue(this VendorBill bill)
    {
        if (!bill.DueDate.HasValue) return int.MaxValue;
        return (bill.DueDate.Value - DateTime.Today).Days;
    }

    /// <summary>
    /// Gets days until due (negative if overdue)
    /// </summary>
    public static int GetDaysUntilDue(this CustomerInvoice invoice)
    {
        if (!invoice.DueDate.HasValue) return int.MaxValue;
        return (invoice.DueDate.Value - DateTime.Today).Days;
    }
}

