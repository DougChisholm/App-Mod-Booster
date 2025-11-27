using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly IErrorStateService _errorState;

    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<User> Users { get; set; } = new();

    [BindProperty]
    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [BindProperty]
    [Required]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [BindProperty]
    [Required]
    public int CategoryId { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    [Required]
    public int UserId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? ErrorLocation { get; set; }

    public AddExpenseModel(IExpenseService expenseService, IErrorStateService errorState)
    {
        _expenseService = expenseService;
        _errorState = errorState;
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDataAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var amountMinor = (int)(Amount * 100);
        await _expenseService.CreateExpenseAsync(UserId, CategoryId, amountMinor, ExpenseDate, Description);

        return RedirectToPage("/Expenses");
    }

    private async Task LoadDataAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();
        Users = await _expenseService.GetUsersAsync();

        if (_errorState.HasError)
        {
            ErrorMessage = _errorState.ErrorMessage;
            ErrorLocation = _errorState.ErrorLocation;
        }
    }
}
