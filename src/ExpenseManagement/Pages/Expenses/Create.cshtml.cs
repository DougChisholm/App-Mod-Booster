using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagement.Pages.Expenses;

public class CreateModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IExpenseService expenseService, ILogger<CreateModel> logger)
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
        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public async Task OnGetAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();
        Input.ExpenseDate = DateTime.Today;
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
            var request = new CreateExpenseRequest
            {
                UserId = 1, // Default user for demo
                CategoryId = Input.CategoryId,
                Amount = Input.Amount,
                ExpenseDate = Input.ExpenseDate,
                Description = Input.Description
            };

            var expenseId = await _expenseService.CreateExpenseAsync(request);

            if (expenseId > 0)
            {
                return RedirectToPage("/Index");
            }
            else
            {
                if (_expenseService is ExpenseService service)
                {
                    ErrorMessage = service.LastError ?? "Failed to create expense.";
                }
                else
                {
                    ErrorMessage = "Failed to create expense.";
                }
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = $"Error creating expense: {ex.Message}";
            return Page();
        }
    }
}
