namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AmountMinor { get; set; }
    public decimal AmountGBP => AmountMinor / 100.0m;
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Display formatted amount
    public string FormattedAmount => $"£{AmountGBP:N2}";
    
    // Display formatted date
    public string FormattedDate => ExpenseDate.ToString("dd/MM/yyyy");
}

public class CreateExpenseRequest
{
    public int UserId { get; set; } = 1; // Default user for demo
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    
    public int AmountMinor => (int)(Amount * 100);
}

public class UpdateExpenseRequest
{
    public int ExpenseId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    
    public int AmountMinor => (int)(Amount * 100);
}
