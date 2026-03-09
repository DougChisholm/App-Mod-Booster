using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages.Expenses;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _service;
    public ApproveModel(IExpenseService service) => _service = service;

    public List<Expense> PendingExpenses { get; set; } = new();
    public string?       Filter          { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter;
        var (expenses, err) = await _service.GetPendingExpensesAsync(filter);
        PendingExpenses = expenses;
        if (err != null) ViewData["ErrorMessage"] = err;
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId, int reviewerUserId, string? filter)
    {
        var (_, error) = await _service.ApproveExpenseAsync(expenseId, reviewerUserId);
        if (error != null) TempData["ErrorMsg"] = error;
        else TempData["SuccessMessage"] = $"Expense #{expenseId} approved.";
        return RedirectToPage(new { filter });
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId, int reviewerUserId, string? filter)
    {
        var (_, error) = await _service.RejectExpenseAsync(expenseId, reviewerUserId);
        if (error != null) TempData["ErrorMsg"] = error;
        else TempData["SuccessMessage"] = $"Expense #{expenseId} rejected.";
        return RedirectToPage(new { filter });
    }
}
