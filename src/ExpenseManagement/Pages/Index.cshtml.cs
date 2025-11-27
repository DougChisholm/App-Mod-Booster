using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly IErrorStateService _errorState;
    private readonly ILogger<IndexModel> _logger;

    public List<ExpenseSummary> ExpenseSummary { get; set; } = new();
    public List<CategorySummary> CategorySummary { get; set; } = new();
    public List<Expense> RecentExpenses { get; set; } = new();
    public int PendingCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorLocation { get; set; }

    public IndexModel(IExpenseService expenseService, IErrorStateService errorState, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _errorState = errorState;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        ExpenseSummary = await _expenseService.GetExpenseSummaryAsync();
        CategorySummary = await _expenseService.GetExpensesByCategoryAsync();
        RecentExpenses = (await _expenseService.GetExpensesAsync()).Take(5).ToList();
        var pending = await _expenseService.GetPendingExpensesAsync();
        PendingCount = pending.Count;

        if (_errorState.HasError)
        {
            ErrorMessage = _errorState.ErrorMessage;
            ErrorLocation = _errorState.ErrorLocation;
        }
    }
}
