using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.Text.Json;
using ChatMessageModel = ExpenseManagement.Models.ChatMessage;
using ChatRequestModel = ExpenseManagement.Models.ChatRequest;
using ChatResponseModel = ExpenseManagement.Models.ChatResponse;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponseModel> GetResponseAsync(ChatRequestModel request);
    bool IsEnabled { get; }
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;
    private readonly string? _endpoint;
    private readonly string? _deploymentName;
    private readonly string? _managedIdentityClientId;

    public bool IsEnabled => !string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_deploymentName);

    public ChatService(
        IConfiguration configuration,
        IExpenseService expenseService,
        ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;
        _endpoint = configuration["OpenAI:Endpoint"];
        _deploymentName = configuration["OpenAI:DeploymentName"];
        _managedIdentityClientId = configuration["ManagedIdentityClientId"];
    }

    public async Task<ChatResponseModel> GetResponseAsync(ChatRequestModel request)
    {
        if (!IsEnabled)
        {
            return new ChatResponseModel
            {
                Success = true,
                IsGenAIEnabled = false,
                Message = "GenAI services are not configured. To enable the AI assistant, deploy the application using 'deploy-with-chat.sh' which provisions Azure OpenAI and AI Search resources. " +
                         "This allows you to interact with your expense data using natural language queries like 'Show me all pending expenses' or 'Create a new travel expense for £50'."
            };
        }

        try
        {
            Azure.Core.TokenCredential credential;
            if (!string.IsNullOrEmpty(_managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", _managedIdentityClientId);
                credential = new ManagedIdentityCredential(_managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var client = new AzureOpenAIClient(new Uri(_endpoint!), credential);
            var chatClient = client.GetChatClient(_deploymentName);

            var tools = GetFunctionTools();
            var systemMessage = GetSystemPrompt();

            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(systemMessage)
            };

            // Add conversation history
            foreach (var msg in request.History)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    messages.Add(new UserChatMessage(msg.Content));
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    messages.Add(new AssistantChatMessage(msg.Content));
            }

            // Add current message
            messages.Add(new UserChatMessage(request.Message));

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Function calling loop
            var maxIterations = 10;
            var iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;
                var response = await chatClient.CompleteChatAsync(messages, options);
                var completion = response.Value;

                if (completion.FinishReason == ChatFinishReason.Stop)
                {
                    var content = completion.Content.FirstOrDefault()?.Text ?? "I couldn't generate a response.";
                    return new ChatResponseModel
                    {
                        Success = true,
                        IsGenAIEnabled = true,
                        Message = content
                    };
                }

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    var assistantMessage = new AssistantChatMessage(completion);
                    messages.Add(assistantMessage);

                    foreach (var toolCall in completion.ToolCalls)
                    {
                        var result = await ExecuteFunctionAsync(toolCall);
                        messages.Add(new ToolChatMessage(toolCall.Id, result));
                    }
                }
                else
                {
                    break;
                }
            }

            return new ChatResponseModel
            {
                Success = true,
                IsGenAIEnabled = true,
                Message = "I completed processing your request."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return new ChatResponseModel
            {
                Success = false,
                IsGenAIEnabled = true,
                Error = $"Error communicating with AI service: {ex.Message}"
            };
        }
    }

    private string GetSystemPrompt()
    {
        return """
            You are a helpful assistant for an Expense Management System. You can help users:
            - View and filter expenses
            - Create new expenses
            - Submit expenses for approval
            - Approve or reject expenses (for managers)
            - Get summaries and reports

            When listing expenses or other data, format the output nicely with:
            - Use **bold** for headers and important values
            - Use numbered lists (1. 2. 3.) for expense items
            - Include relevant details like date, category, amount, and status
            - Format amounts with the £ symbol

            Available functions allow you to:
            - get_expenses: List expenses with optional filters (userId, statusId, categoryId, searchTerm)
            - get_pending_expenses: List expenses waiting for approval
            - get_expense_by_id: Get details of a specific expense
            - create_expense: Create a new expense (requires userId, categoryId, amount, expenseDate, description)
            - submit_expense: Submit an expense for approval
            - approve_expense: Approve a pending expense (requires reviewerId)
            - reject_expense: Reject a pending expense (requires reviewerId)
            - get_categories: List available expense categories
            - get_users: List users in the system
            - get_expense_summary: Get summary statistics by status
            - get_expenses_by_category: Get summary statistics by category

            Always be helpful and provide clear responses. If an operation fails, explain what went wrong.
            """;
    }

    private List<ChatTool> GetFunctionTools()
    {
        return new List<ChatTool>
        {
            ChatTool.CreateFunctionTool("get_expenses", "Get a list of expenses with optional filters",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "userId": { "type": "integer", "description": "Filter by user ID" },
                        "statusId": { "type": "integer", "description": "Filter by status ID (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)" },
                        "categoryId": { "type": "integer", "description": "Filter by category ID" },
                        "searchTerm": { "type": "string", "description": "Search term for description" }
                    }
                }
                """)),

            ChatTool.CreateFunctionTool("get_pending_expenses", "Get expenses pending approval",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "searchTerm": { "type": "string", "description": "Optional search term" }
                    }
                }
                """)),

            ChatTool.CreateFunctionTool("get_expense_by_id", "Get details of a specific expense",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": { "type": "integer", "description": "The expense ID" }
                    },
                    "required": ["expenseId"]
                }
                """)),

            ChatTool.CreateFunctionTool("create_expense", "Create a new expense",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "userId": { "type": "integer", "description": "User ID creating the expense" },
                        "categoryId": { "type": "integer", "description": "Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)" },
                        "amount": { "type": "number", "description": "Amount in GBP (e.g., 25.50)" },
                        "expenseDate": { "type": "string", "description": "Expense date in YYYY-MM-DD format" },
                        "description": { "type": "string", "description": "Description of the expense" }
                    },
                    "required": ["userId", "categoryId", "amount", "expenseDate"]
                }
                """)),

            ChatTool.CreateFunctionTool("submit_expense", "Submit an expense for approval",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": { "type": "integer", "description": "The expense ID to submit" }
                    },
                    "required": ["expenseId"]
                }
                """)),

            ChatTool.CreateFunctionTool("approve_expense", "Approve a pending expense",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": { "type": "integer", "description": "The expense ID to approve" },
                        "reviewerId": { "type": "integer", "description": "The manager's user ID" }
                    },
                    "required": ["expenseId", "reviewerId"]
                }
                """)),

            ChatTool.CreateFunctionTool("reject_expense", "Reject a pending expense",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": { "type": "integer", "description": "The expense ID to reject" },
                        "reviewerId": { "type": "integer", "description": "The manager's user ID" }
                    },
                    "required": ["expenseId", "reviewerId"]
                }
                """)),

            ChatTool.CreateFunctionTool("get_categories", "Get list of expense categories",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {}
                }
                """)),

            ChatTool.CreateFunctionTool("get_users", "Get list of users",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {}
                }
                """)),

            ChatTool.CreateFunctionTool("get_expense_summary", "Get expense summary by status",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "userId": { "type": "integer", "description": "Optional user ID to filter by" }
                    }
                }
                """)),

            ChatTool.CreateFunctionTool("get_expenses_by_category", "Get expense summary by category",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "userId": { "type": "integer", "description": "Optional user ID to filter by" }
                    }
                }
                """))
        };
    }

    private async Task<string> ExecuteFunctionAsync(ChatToolCall toolCall)
    {
        try
        {
            var functionName = toolCall.FunctionName;
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.FunctionArguments.ToString()) 
                ?? new Dictionary<string, JsonElement>();

            _logger.LogInformation("Executing function: {Function} with args: {Args}", functionName, toolCall.FunctionArguments);

            return functionName switch
            {
                "get_expenses" => JsonSerializer.Serialize(await _expenseService.GetExpensesAsync(
                    GetIntArg(arguments, "userId"),
                    GetIntArg(arguments, "statusId"),
                    GetIntArg(arguments, "categoryId"),
                    GetStringArg(arguments, "searchTerm"))),

                "get_pending_expenses" => JsonSerializer.Serialize(await _expenseService.GetPendingExpensesAsync(
                    GetStringArg(arguments, "searchTerm"))),

                "get_expense_by_id" => JsonSerializer.Serialize(await _expenseService.GetExpenseByIdAsync(
                    GetIntArg(arguments, "expenseId") ?? throw new ArgumentException("expenseId is required"))),

                "create_expense" => await CreateExpenseAsync(arguments),

                "submit_expense" => JsonSerializer.Serialize(new { success = await _expenseService.SubmitExpenseAsync(
                    GetIntArg(arguments, "expenseId") ?? throw new ArgumentException("expenseId is required")) }),

                "approve_expense" => JsonSerializer.Serialize(new { success = await _expenseService.ApproveExpenseAsync(
                    GetIntArg(arguments, "expenseId") ?? throw new ArgumentException("expenseId is required"),
                    GetIntArg(arguments, "reviewerId") ?? throw new ArgumentException("reviewerId is required")) }),

                "reject_expense" => JsonSerializer.Serialize(new { success = await _expenseService.RejectExpenseAsync(
                    GetIntArg(arguments, "expenseId") ?? throw new ArgumentException("expenseId is required"),
                    GetIntArg(arguments, "reviewerId") ?? throw new ArgumentException("reviewerId is required")) }),

                "get_categories" => JsonSerializer.Serialize(await _expenseService.GetCategoriesAsync()),

                "get_users" => JsonSerializer.Serialize(await _expenseService.GetUsersAsync()),

                "get_expense_summary" => JsonSerializer.Serialize(await _expenseService.GetExpenseSummaryAsync(
                    GetIntArg(arguments, "userId"))),

                "get_expenses_by_category" => JsonSerializer.Serialize(await _expenseService.GetExpensesByCategoryAsync(
                    GetIntArg(arguments, "userId"))),

                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {Function}", toolCall.FunctionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> CreateExpenseAsync(Dictionary<string, JsonElement> arguments)
    {
        var userId = GetIntArg(arguments, "userId") ?? throw new ArgumentException("userId is required");
        var categoryId = GetIntArg(arguments, "categoryId") ?? throw new ArgumentException("categoryId is required");
        var amount = GetDecimalArg(arguments, "amount") ?? throw new ArgumentException("amount is required");
        var dateStr = GetStringArg(arguments, "expenseDate") ?? throw new ArgumentException("expenseDate is required");
        var description = GetStringArg(arguments, "description");

        if (!DateTime.TryParse(dateStr, out var expenseDate))
            throw new ArgumentException("Invalid date format");

        var amountMinor = (int)(amount * 100);
        var expenseId = await _expenseService.CreateExpenseAsync(userId, categoryId, amountMinor, expenseDate, description);

        return JsonSerializer.Serialize(new { success = true, expenseId });
    }

    private static int? GetIntArg(Dictionary<string, JsonElement> args, string key)
    {
        if (args.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number)
            return element.GetInt32();
        return null;
    }

    private static string? GetStringArg(Dictionary<string, JsonElement> args, string key)
    {
        if (args.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
            return element.GetString();
        return null;
    }

    private static decimal? GetDecimalArg(Dictionary<string, JsonElement> args, string key)
    {
        if (args.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number)
            return element.GetDecimal();
        return null;
    }
}
