using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _service;
    public IndexModel(IExpenseService service) => _service = service;

    public List<Expense>  Expenses   { get; set; } = new();
    public List<Expense>  Pending    { get; set; } = new();
    public string?        Filter     { get; set; }
    public string?        StatusFilter { get; set; }

    // Stats
    public int     TotalCount    { get; set; }
    public decimal TotalGBP      { get; set; }
    public int     PendingCount  { get; set; }
    public int     ApprovedCount { get; set; }

    public async Task OnGetAsync(string? filter, string? status)
    {
        Filter       = filter;
        StatusFilter = status;

        var (expenses, err)  = await _service.GetAllExpensesAsync(filter, status);
        var (pending, err2)  = await _service.GetPendingExpensesAsync();

        Expenses = expenses;
        Pending  = pending;

        TotalCount    = expenses.Count;
        TotalGBP      = expenses.Sum(e => e.AmountGBP);
        PendingCount  = expenses.Count(e => e.StatusName == "Submitted");
        ApprovedCount = expenses.Count(e => e.StatusName == "Approved");

        if (err  != null) ViewData["ErrorMessage"] = err;
        if (err2 != null) ViewData["ErrorMessage"] = err2;
    }
}
