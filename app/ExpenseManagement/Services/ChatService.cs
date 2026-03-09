using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using ExpenseManagement.Settings;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace ExpenseManagement.Services;

/// <summary>
/// Chat service that connects to Azure OpenAI using the user-assigned Managed Identity.
/// Implements function calling so the AI can interact with the expense database via the
/// IExpenseService APIs. Falls back to a helpful dummy response when GenAI is not configured.
/// </summary>
public class ChatService : IChatService
{
    private readonly IExpenseService         _expenseService;
    private readonly GenAISettings           _settings;
    private readonly ILogger<ChatService>    _logger;
    private readonly IConfiguration          _config;

    public ChatService(
        IExpenseService expenseService,
        IOptions<GenAISettings> settings,
        ILogger<ChatService> logger,
        IConfiguration config)
    {
        _expenseService = expenseService;
        _settings       = settings.Value;
        _logger         = logger;
        _config         = config;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        // If GenAI is not configured, return a helpful dummy response
        if (string.IsNullOrEmpty(_settings.Endpoint))
        {
            return new ChatResponse
            {
                Message = "ℹ️ **GenAI services are not yet deployed.**\n\n" +
                          "To enable the AI assistant, run **`./deploy-with-chat.sh`** which will provision " +
                          "Azure OpenAI (GPT-4o) and configure this App Service automatically.\n\n" +
                          "Once deployed, I will be able to:\n" +
                          "- List and filter your expenses\n" +
                          "- Add new expenses\n" +
                          "- Show pending approvals\n" +
                          "- Approve or reject expenses\n\n" +
                          "For now, please use the **Expenses** and **Approve** pages in the navigation."
            };
        }

        try
        {
            var client = CreateOpenAIClient();
            var chatClient = client.GetChatClient(_settings.DeploymentName);

            // Build the message history
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(BuildSystemPrompt())
            };

            foreach (var h in request.History)
            {
                if (h.Role == "user")
                    messages.Add(ChatMessage.CreateUserMessage(h.Content));
                else if (h.Role == "assistant")
                    messages.Add(ChatMessage.CreateAssistantMessage(h.Content));
            }
            messages.Add(ChatMessage.CreateUserMessage(request.Message));

            // Define function tools
            var tools = BuildFunctionTools();

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
                options.Tools.Add(tool);

            // Function calling orchestration loop
            string finalResponse = "";
            int    maxIterations = 5;

            for (int i = 0; i < maxIterations; i++)
            {
                var completion = await chatClient.CompleteChatAsync(messages, options);

                if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Execute the function calls and add results
                    messages.Add(ChatMessage.CreateAssistantMessage(completion.Value));

                    foreach (var toolCall in completion.Value.ToolCalls)
                    {
                        var result = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result));
                    }
                }
                else
                {
                    finalResponse = completion.Value.Content[0].Text;
                    break;
                }
            }

            return new ChatResponse { Message = finalResponse };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatService.SendMessageAsync failed");
            return new ChatResponse
            {
                Message  = $"Sorry, I encountered an error: {ex.Message}",
                IsError  = true
            };
        }
    }

    // ---------------------------------------------------------------
    // Create OpenAI client using Managed Identity
    // ---------------------------------------------------------------
    private AzureOpenAIClient CreateOpenAIClient()
    {
        var managedIdentityClientId = _config["ManagedIdentityClientId"];
        Azure.Core.TokenCredential credential;

        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
            credential = new ManagedIdentityCredential(managedIdentityClientId);
        }
        else
        {
            _logger.LogInformation("Using DefaultAzureCredential");
            credential = new DefaultAzureCredential();
        }

        return new AzureOpenAIClient(new Uri(_settings.Endpoint), credential);
    }

    // ---------------------------------------------------------------
    // System prompt
    // ---------------------------------------------------------------
    private static string BuildSystemPrompt() =>
        """
        You are an intelligent assistant for the Expense Management System.
        You help employees and managers manage business expenses.
        
        You have access to the following functions:
        - get_all_expenses: Retrieve all expenses, optionally filtered
        - get_pending_expenses: Get expenses awaiting manager approval
        - create_expense: Add a new expense record
        - approve_expense: Approve a submitted expense
        - reject_expense: Reject a submitted expense
        - get_categories: List available expense categories
        - get_users: List all users in the system
        
        When listing expenses or other items, format them clearly using numbered or bulleted lists.
        Always display monetary amounts in GBP (£). 
        Be helpful, concise, and professional.
        If an operation succeeds, confirm it clearly.
        If an operation fails, explain what went wrong and suggest a fix.
        """;

    // ---------------------------------------------------------------
    // Define function tools
    // ---------------------------------------------------------------
    private static List<ChatTool> BuildFunctionTools() => new()
    {
        ChatTool.CreateFunctionTool(
            "get_all_expenses",
            "Retrieves all expenses from the database, optionally filtered by a search term or status.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "filter":       { "type": "string", "description": "Optional text filter (searches category, description, status, user name)" },
                    "statusFilter": { "type": "string", "description": "Optional status filter: Draft, Submitted, Approved, or Rejected" }
                  }
                }
                """)),

        ChatTool.CreateFunctionTool(
            "get_pending_expenses",
            "Gets all expenses with status 'Submitted' that are waiting for manager approval.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "filter": { "type": "string", "description": "Optional text filter" }
                  }
                }
                """)),

        ChatTool.CreateFunctionTool(
            "create_expense",
            "Creates a new expense record with status Draft.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "userId":      { "type": "integer", "description": "ID of the user submitting the expense" },
                    "categoryId":  { "type": "integer", "description": "ID of the expense category" },
                    "amountMinor": { "type": "integer", "description": "Amount in pence (e.g. 1250 = £12.50)" },
                    "expenseDate": { "type": "string",  "description": "Date of the expense in YYYY-MM-DD format" },
                    "description": { "type": "string",  "description": "Description of the expense" }
                  },
                  "required": ["userId","categoryId","amountMinor","expenseDate"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            "approve_expense",
            "Approves a submitted expense.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "expenseId":      { "type": "integer", "description": "ID of the expense to approve" },
                    "reviewerUserId": { "type": "integer", "description": "ID of the manager approving the expense" }
                  },
                  "required": ["expenseId","reviewerUserId"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            "reject_expense",
            "Rejects a submitted expense.",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "expenseId":      { "type": "integer", "description": "ID of the expense to reject" },
                    "reviewerUserId": { "type": "integer", "description": "ID of the manager rejecting the expense" }
                  },
                  "required": ["expenseId","reviewerUserId"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            "get_categories",
            "Lists all available expense categories.",
            BinaryData.FromString("""{ "type": "object", "properties": {} }""")),

        ChatTool.CreateFunctionTool(
            "get_users",
            "Lists all users in the system.",
            BinaryData.FromString("""{ "type": "object", "properties": {} }"""))
    };

    // ---------------------------------------------------------------
    // Execute a function call and return the result as a JSON string
    // ---------------------------------------------------------------
    private async Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        try
        {
            using var args = JsonDocument.Parse(argumentsJson);

            switch (functionName)
            {
                case "get_all_expenses":
                {
                    var filter       = GetOptionalString(args, "filter");
                    var statusFilter = GetOptionalString(args, "statusFilter");
                    var (expenses, err) = await _expenseService.GetAllExpensesAsync(filter, statusFilter);
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(expenses);
                }

                case "get_pending_expenses":
                {
                    var filter = GetOptionalString(args, "filter");
                    var (expenses, err) = await _expenseService.GetPendingExpensesAsync(filter);
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(expenses);
                }

                case "create_expense":
                {
                    var req = new CreateExpenseRequest
                    {
                        UserId      = args.RootElement.GetProperty("userId").GetInt32(),
                        CategoryId  = args.RootElement.GetProperty("categoryId").GetInt32(),
                        AmountMinor = args.RootElement.GetProperty("amountMinor").GetInt32(),
                        ExpenseDate = DateTime.Parse(args.RootElement.GetProperty("expenseDate").GetString()!),
                        Description = GetOptionalString(args, "description")
                    };
                    var (newId, err) = await _expenseService.CreateExpenseAsync(req);
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(new { success = true, newExpenseId = newId });
                }

                case "approve_expense":
                {
                    var expenseId      = args.RootElement.GetProperty("expenseId").GetInt32();
                    var reviewerUserId = args.RootElement.GetProperty("reviewerUserId").GetInt32();
                    var (success, err) = await _expenseService.ApproveExpenseAsync(expenseId, reviewerUserId);
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(new { success });
                }

                case "reject_expense":
                {
                    var expenseId      = args.RootElement.GetProperty("expenseId").GetInt32();
                    var reviewerUserId = args.RootElement.GetProperty("reviewerUserId").GetInt32();
                    var (success, err) = await _expenseService.RejectExpenseAsync(expenseId, reviewerUserId);
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(new { success });
                }

                case "get_categories":
                {
                    var (categories, err) = await _expenseService.GetCategoriesAsync();
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(categories);
                }

                case "get_users":
                {
                    var (users, err) = await _expenseService.GetUsersAsync();
                    return err != null
                        ? JsonSerializer.Serialize(new { error = err })
                        : JsonSerializer.Serialize(users);
                }

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteFunction {FunctionName} failed", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string? GetOptionalString(JsonDocument doc, string property)
    {
        if (doc.RootElement.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String)
            return el.GetString();
        return null;
    }
}
