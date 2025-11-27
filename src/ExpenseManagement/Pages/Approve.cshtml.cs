using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly IErrorStateService _errorState;

    public List<Expense> PendingExpenses { get; set; } = new();
    public List<User> Managers { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ReviewerId { get; set; } = 2; // Default to manager user

    public string? ErrorMessage { get; set; }
    public string? ErrorLocation { get; set; }

    public ApproveModel(IExpenseService expenseService, IErrorStateService errorState)
    {
        _expenseService = expenseService;
        _errorState = errorState;
    }

    public async Task OnGetAsync()
    {
        var users = await _expenseService.GetUsersAsync();
        Managers = users.Where(u => u.RoleName == "Manager").ToList();
        
        if (!Managers.Any())
        {
            Managers = users; // Fallback to all users if no managers found
        }

        if (ReviewerId == 0 && Managers.Any())
        {
            ReviewerId = Managers.First().UserId;
        }

        PendingExpenses = await _expenseService.GetPendingExpensesAsync(SearchTerm);

        if (_errorState.HasError)
        {
            ErrorMessage = _errorState.ErrorMessage;
            ErrorLocation = _errorState.ErrorLocation;
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, int reviewerId)
    {
        await _expenseService.ApproveExpenseAsync(id, reviewerId);
        return RedirectToPage(new { reviewerId });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, int reviewerId)
    {
        await _expenseService.RejectExpenseAsync(id, reviewerId);
        return RedirectToPage(new { reviewerId });
    }
}
