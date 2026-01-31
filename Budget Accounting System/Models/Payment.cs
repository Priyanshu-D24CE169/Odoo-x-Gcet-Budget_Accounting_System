namespace Budget_Accounting_System.Models;

public class Payment
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public int? CustomerInvoiceId { get; set; }
    public int? VendorBillId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public CustomerInvoice? CustomerInvoice { get; set; }
    public VendorBill? VendorBill { get; set; }
}

public enum PaymentType
{
    Received,
    Paid
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Check,
    CreditCard,
    Online
}
