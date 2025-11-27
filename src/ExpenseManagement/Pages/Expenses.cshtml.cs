using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly IErrorStateService _errorState;

    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? ErrorLocation { get; set; }

    public ExpensesModel(IExpenseService expenseService, IErrorStateService errorState)
    {
        _expenseService = expenseService;
        _errorState = errorState;
    }

    public async Task OnGetAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();
        Statuses = await _expenseService.GetStatusesAsync();
        Expenses = await _expenseService.GetExpensesAsync(null, StatusId, CategoryId, SearchTerm);

        if (_errorState.HasError)
        {
            ErrorMessage = _errorState.ErrorMessage;
            ErrorLocation = _errorState.ErrorLocation;
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        await _expenseService.SubmitExpenseAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _expenseService.DeleteExpenseAsync(id);
        return RedirectToPage();
    }
}
