using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly IChatService _chatService;

    public ChatModel(IChatService chatService)
    {
        _chatService = chatService;
    }

    public bool IsAIEnabled => _chatService.IsConfigured;

    public void OnGet()
    {
    }
}
