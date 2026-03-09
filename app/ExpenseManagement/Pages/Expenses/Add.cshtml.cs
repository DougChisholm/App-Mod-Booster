using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseManagement.Pages.Expenses;

public class AddModel : PageModel
{
    private readonly IExpenseService _service;
    public AddModel(IExpenseService service) => _service = service;

    [BindProperty]
    public CreateExpenseRequest Input { get; set; } = new()
    {
        ExpenseDate = DateTime.Today,
        Currency    = "GBP"
    };

    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Users      { get; set; } = new();
    public string?              SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDropdownsAsync();

        if (!ModelState.IsValid) return Page();

        var (newId, error) = await _service.CreateExpenseAsync(Input);
        if (error != null)
        {
            ViewData["ErrorMessage"] = error;
            ModelState.AddModelError(string.Empty, "Could not save expense. See the error bar above.");
            return Page();
        }

        TempData["SuccessMessage"] = $"Expense #{newId} created successfully.";
        return RedirectToPage("/Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var (cats, catErr) = await _service.GetCategoriesAsync();
        if (catErr != null) ViewData["ErrorMessage"] = catErr;
        Categories = cats.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString())).ToList();

        var (users, userErr) = await _service.GetUsersAsync();
        if (userErr != null) ViewData["ErrorMessage"] = userErr;
        Users = users.Select(u => new SelectListItem(u.UserName, u.UserId.ToString())).ToList();
    }
}
