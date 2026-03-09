namespace ExpenseManagement.Models;

public class Expense
{
    public int        ExpenseId        { get; set; }
    public int        UserId           { get; set; }
    public string     UserName         { get; set; } = string.Empty;
    public string     Email            { get; set; } = string.Empty;
    public int        CategoryId       { get; set; }
    public string     CategoryName     { get; set; } = string.Empty;
    public int        StatusId         { get; set; }
    public string     StatusName       { get; set; } = string.Empty;
    public int        AmountMinor      { get; set; }  // pence
    public decimal    AmountGBP        { get; set; }  // calculated by stored proc
    public string     Currency         { get; set; } = "GBP";
    public DateTime   ExpenseDate      { get; set; }
    public string?    Description      { get; set; }
    public string?    ReceiptFile      { get; set; }
    public DateTime?  SubmittedAt      { get; set; }
    public int?       ReviewedBy       { get; set; }
    public string?    ReviewedByName   { get; set; }
    public DateTime?  ReviewedAt       { get; set; }
    public DateTime   CreatedAt        { get; set; }
}
