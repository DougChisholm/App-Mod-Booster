using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Models;

public class CreateExpenseRequest
{
    [Required]
    public int     UserId      { get; set; }

    [Required]
    public int     CategoryId  { get; set; }

    /// <summary>Amount in pence (e.g. 1234 = £12.34)</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public int     AmountMinor { get; set; }

    public string  Currency    { get; set; } = "GBP";

    [Required]
    public DateTime ExpenseDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ReceiptFile { get; set; }
}

public class UpdateStatusRequest
{
    [Required]
    public int ReviewerUserId { get; set; }
}

public class ChatRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItem> History { get; set; } = new();
}

public class ChatHistoryItem
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool   IsError { get; set; }
}
