using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? FilterTerm { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        FilterTerm = filter;
        
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                Expenses = await _expenseService.SearchExpensesAsync(filter);
            }
            else
            {
                Expenses = await _expenseService.GetAllExpensesAsync();
            }

            // Check for service errors
            if (_expenseService is ExpenseService service && service.LastError != null)
            {
                ErrorMessage = service.LastError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            ErrorMessage = $"Error loading expenses: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        var success = await _expenseService.SubmitExpenseAsync(expenseId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int expenseId)
    {
        var success = await _expenseService.DeleteExpenseAsync(expenseId);
        return RedirectToPage();
    }
}
