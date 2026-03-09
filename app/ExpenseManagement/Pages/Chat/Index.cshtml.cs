using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages.Chat;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "AI Assistant";
    }
}
