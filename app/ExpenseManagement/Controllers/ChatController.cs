using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
        => _chatService = chatService;

    /// <summary>Send a message to the AI assistant and receive a response.</summary>
    /// <remarks>
    /// The assistant can list/filter expenses, add expenses, approve/reject submissions,
    /// and more — all via natural language.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var response = await _chatService.SendMessageAsync(request);
        return Ok(response);
    }
}
