using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Pages.Expenses;

public class EditModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IExpenseService expenseService, ILogger<EditModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    [BindProperty]
    public ExpenseInput Input { get; set; } = new();

    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public class ExpenseInput
    {
        public int ExpenseId { get; set; }

        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Categories = await _expenseService.GetCategoriesAsync();

        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return NotFound();
        }

        Input = new ExpenseInput
        {
            ExpenseId = expense.ExpenseId,
            Amount = expense.AmountGBP,
            ExpenseDate = expense.ExpenseDate,
            CategoryId = expense.CategoryId,
            Description = expense.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var request = new UpdateExpenseRequest
            {
                ExpenseId = Input.ExpenseId,
                CategoryId = Input.CategoryId,
                Amount = Input.Amount,
                ExpenseDate = Input.ExpenseDate,
                Description = Input.Description
            };

            var success = await _expenseService.UpdateExpenseAsync(request);

            if (success)
            {
                return RedirectToPage("/Index");
            }
            else
            {
                if (_expenseService is ExpenseService service)
                {
                    ErrorMessage = service.LastError ?? "Failed to update expense.";
                }
                else
                {
                    ErrorMessage = "Failed to update expense.";
                }
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            ErrorMessage = $"Error updating expense: {ex.Message}";
            return Page();
        }
    }
}
