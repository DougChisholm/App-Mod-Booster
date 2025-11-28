using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to the AI chat assistant
    /// </summary>
    /// <param name="request">The chat request containing the user message</param>
    /// <returns>The AI response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required");
        }

        try
        {
            var response = await _chatService.GetChatResponseAsync(request.Message);
            return Ok(new ChatResponse
            {
                Message = response,
                IsAIEnabled = _chatService.IsConfigured
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return Ok(new ChatResponse
            {
                Message = $"I apologize, but I encountered an error: {ex.Message}",
                IsAIEnabled = false
            });
        }
    }

    /// <summary>
    /// Gets the chat service status
    /// </summary>
    /// <returns>Whether AI chat is enabled</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ChatStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<ChatStatusResponse> GetStatus()
    {
        return Ok(new ChatStatusResponse
        {
            IsAIEnabled = _chatService.IsConfigured
        });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool IsAIEnabled { get; set; }
}

public class ChatStatusResponse
{
    public bool IsAIEnabled { get; set; }
}
