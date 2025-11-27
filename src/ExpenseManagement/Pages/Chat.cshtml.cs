using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly IErrorStateService _errorState;

    public string? ErrorMessage { get; set; }
    public string? ErrorLocation { get; set; }

    public ChatModel(IErrorStateService errorState)
    {
        _errorState = errorState;
    }

    public void OnGet()
    {
        if (_errorState.HasError)
        {
            ErrorMessage = _errorState.ErrorMessage;
            ErrorLocation = _errorState.ErrorLocation;
        }
    }
}
