using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Approval;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string? FilterTerm { get; set; }

    public async Task OnGetAsync(string? filter, string? success)
    {
        FilterTerm = filter;
        SuccessMessage = success;

        try
        {
            var allPending = await _expenseService.GetPendingExpensesAsync();
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                PendingExpenses = allPending
                    .Where(e => (e.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                               e.CategoryName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                               e.UserName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                PendingExpenses = allPending;
            }

            if (_expenseService is ExpenseService service && service.LastError != null)
            {
                ErrorMessage = service.LastError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending expenses");
            ErrorMessage = $"Error loading pending expenses: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        try
        {
            var success = await _expenseService.ApproveExpenseAsync(expenseId, 2); // Default manager ID = 2
            
            if (success)
            {
                return RedirectToPage(new { success = "Expense approved successfully." });
            }
            else
            {
                TempData["Error"] = "Failed to approve expense.";
                return RedirectToPage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            TempData["Error"] = $"Error approving expense: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        try
        {
            var success = await _expenseService.RejectExpenseAsync(expenseId, 2); // Default manager ID = 2
            
            if (success)
            {
                return RedirectToPage(new { success = "Expense rejected." });
            }
            else
            {
                TempData["Error"] = "Failed to reject expense.";
                return RedirectToPage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            TempData["Error"] = $"Error rejecting expense: {ex.Message}";
            return RedirectToPage();
        }
    }
}
