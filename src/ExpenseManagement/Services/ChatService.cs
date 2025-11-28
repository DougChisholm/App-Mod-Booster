using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.Text.Json;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage);
    bool IsConfigured { get; }
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;
    private readonly AzureOpenAIClient? _openAIClient;
    private readonly string? _deploymentName;
    private readonly bool _isConfigured;

    public bool IsConfigured => _isConfigured;

    public ChatService(
        IConfiguration configuration,
        IExpenseService expenseService,
        ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;

        var endpoint = configuration["OpenAI:Endpoint"];
        _deploymentName = configuration["OpenAI:DeploymentName"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName))
        {
            try
            {
                // Use ManagedIdentityCredential with explicit client ID if available
                var managedIdentityClientId = configuration["ManagedIdentityClientId"];
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

                _openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                _isConfigured = true;
                _logger.LogInformation("ChatService configured with endpoint: {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
                _isConfigured = false;
            }
        }
        else
        {
            _logger.LogWarning("Azure OpenAI not configured. Chat will return dummy responses.");
            _isConfigured = false;
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        if (!_isConfigured || _openAIClient == null)
        {
            return GetDummyResponse(userMessage);
        }

        try
        {
            var chatClient = _openAIClient.GetChatClient(_deploymentName);

            // Define function tools for expense operations
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_all_expenses",
                    "Retrieves all expenses from the database"),
                ChatTool.CreateFunctionTool(
                    "get_pending_expenses",
                    "Retrieves all pending expenses awaiting approval"),
                ChatTool.CreateFunctionTool(
                    "get_categories",
                    "Retrieves all expense categories"),
                ChatTool.CreateFunctionTool(
                    "search_expenses",
                    "Searches expenses by description, category, or user name",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            searchTerm = new { type = "string", description = "The search term to find expenses" }
                        },
                        required = new[] { "searchTerm" }
                    })),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves an expense (manager action)",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "The ID of the expense to approve" }
                        },
                        required = new[] { "expenseId" }
                    })),
                ChatTool.CreateFunctionTool(
                    "reject_expense",
                    "Rejects an expense (manager action)",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "The ID of the expense to reject" }
                        },
                        required = new[] { "expenseId" }
                    }))
            };

            var systemMessage = @"You are a helpful expense management assistant. You can help users:
- View and search expenses
- Check pending approvals
- Approve or reject expenses (as a manager)
- Get information about expense categories

When listing expenses, format them nicely with:
- Date
- Category
- Amount (in GBP)
- Status
- Description

Use the available functions to interact with the expense database.
Always be helpful and provide clear, formatted responses.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemMessage),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Function calling loop
            var maxIterations = 5;
            for (int i = 0; i < maxIterations; i++)
            {
                var response = await chatClient.CompleteChatAsync(messages, options);
                var completion = response.Value;

                // Check if we need to call functions
                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    messages.Add(new AssistantChatMessage(completion));

                    foreach (var toolCall in completion.ToolCalls)
                    {
                        var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                    }
                }
                else
                {
                    // Return the final response
                    return completion.Content[0].Text;
                }
            }

            return "I apologize, but I couldn't complete your request. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"I encountered an error processing your request: {ex.Message}";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            switch (functionName)
            {
                case "get_all_expenses":
                    var allExpenses = await _expenseService.GetAllExpensesAsync();
                    return JsonSerializer.Serialize(allExpenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.FormattedDate,
                        e.CategoryName,
                        e.FormattedAmount,
                        e.StatusName,
                        e.Description,
                        e.UserName
                    }));

                case "get_pending_expenses":
                    var pendingExpenses = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pendingExpenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.FormattedDate,
                        e.CategoryName,
                        e.FormattedAmount,
                        e.Description,
                        e.UserName
                    }));

                case "get_categories":
                    var categories = await _expenseService.GetCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "search_expenses":
                    var searchArgs = JsonSerializer.Deserialize<SearchExpensesArgs>(arguments);
                    if (searchArgs?.searchTerm != null)
                    {
                        var searchResults = await _expenseService.SearchExpensesAsync(searchArgs.searchTerm);
                        return JsonSerializer.Serialize(searchResults.Select(e => new
                        {
                            e.ExpenseId,
                            e.FormattedDate,
                            e.CategoryName,
                            e.FormattedAmount,
                            e.StatusName,
                            e.Description
                        }));
                    }
                    return "[]";

                case "approve_expense":
                    var approveArgs = JsonSerializer.Deserialize<ExpenseIdArgs>(arguments);
                    if (approveArgs?.expenseId > 0)
                    {
                        var success = await _expenseService.ApproveExpenseAsync(approveArgs.expenseId, 2); // Default manager ID
                        return success ? "Expense approved successfully." : "Failed to approve expense.";
                    }
                    return "Invalid expense ID.";

                case "reject_expense":
                    var rejectArgs = JsonSerializer.Deserialize<ExpenseIdArgs>(arguments);
                    if (rejectArgs?.expenseId > 0)
                    {
                        var success = await _expenseService.RejectExpenseAsync(rejectArgs.expenseId, 2); // Default manager ID
                        return success ? "Expense rejected successfully." : "Failed to reject expense.";
                    }
                    return "Invalid expense ID.";

                default:
                    return $"Unknown function: {functionName}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function: {FunctionName}", functionName);
            return $"Error executing {functionName}: {ex.Message}";
        }
    }

    private string GetDummyResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();

        if (lowerMessage.Contains("expense") && (lowerMessage.Contains("list") || lowerMessage.Contains("show") || lowerMessage.Contains("all")))
        {
            return @"**Expense List** (Demo Data)

Here are the current expenses:

1. **15/01/2024** - Travel - £120.00 - Submitted
   _Taxi from airport to client site_

2. **10/01/2023** - Food - £69.00 - Submitted
   _Client lunch meeting_

3. **04/12/2023** - Office Supplies - £99.50 - Approved
   _Office stationery_

4. **18/12/2023** - Transport - £19.20 - Submitted
   _Train tickets to conference_

---
⚠️ **Note**: This is demo data. To enable AI-powered responses with real database access, deploy the GenAI services using:
```
./deploy-with-chat.sh
```";
        }

        if (lowerMessage.Contains("pending") || lowerMessage.Contains("approve"))
        {
            return @"**Pending Expenses** (Demo Data)

The following expenses are awaiting approval:

1. **ID: 1** - Travel - £120.00
   _Submitted by Alice Example_

2. **ID: 2** - Food - £69.00
   _Submitted by Alice Example_

4. **ID: 4** - Transport - £19.20
   _Submitted by Alice Example_

---
⚠️ **Note**: This is demo data. GenAI services are not deployed.";
        }

        if (lowerMessage.Contains("categor"))
        {
            return @"**Expense Categories**

1. Travel
2. Food
3. Office Supplies
4. Accommodation
5. Other

---
⚠️ **Note**: This is demo data.";
        }

        return @"Hello! I'm the Expense Management Assistant. 

I can help you with:
- **List expenses**: ""Show me all expenses""
- **Pending approvals**: ""What expenses need approval?""
- **Categories**: ""What expense categories are available?""
- **Search**: ""Find travel expenses""

---
⚠️ **Note**: GenAI services are not currently deployed. I'm providing demo responses.
To enable full AI capabilities, run: `./deploy-with-chat.sh`";
    }

    private class SearchExpensesArgs
    {
        public string? searchTerm { get; set; }
    }

    private class ExpenseIdArgs
    {
        public int expenseId { get; set; }
    }
}
