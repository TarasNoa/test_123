using System;
using System.Collections.Generic;

namespace Libr4.Payments.Domain.Invoices;

public enum InvoiceStatus { Draft, Sent, Paid, Overdue, Cancelled }
public enum InvoiceType { Standard, Recurring, Proforma }

public class InvoiceLineItem
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;
    public string? TaxCode { get; set; }
}

public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public Guid IssuerId { get; set; }
    public Guid RecipientId { get; set; }
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = [];
    public decimal Subtotal => LineItems.Sum(l => l.Amount);
    public decimal TaxAmount { get; set; }
    public decimal Total => Subtotal + TaxAmount;
    public string? Notes { get; set; }
    public string? PdfUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Send(DateTimeOffset now) { Status = InvoiceStatus.Sent; UpdatedAt = now; }
    public void MarkAsPaid(DateTimeOffset now) { Status = InvoiceStatus.Paid; UpdatedAt = now; }
    public void Cancel(DateTimeOffset now) { Status = InvoiceStatus.Cancelled; UpdatedAt = now; }
    public bool IsOverdue() => Status != InvoiceStatus.Paid && DateTimeOffset.UtcNow > DueDate;
}
