using ExpenseManagement.Models;
using System.Runtime.CompilerServices;

namespace ExpenseManagement.Services;

public class ExpenseServiceWithFallback : IExpenseService
{
    private readonly ExpenseService _realService;
    private readonly IErrorStateService _errorState;
    private readonly ILogger<ExpenseServiceWithFallback> _logger;

    public ExpenseServiceWithFallback(
        ExpenseService realService,
        IErrorStateService errorState,
        ILogger<ExpenseServiceWithFallback> logger)
    {
        _realService = realService;
        _errorState = errorState;
        _logger = logger;
    }

    private void HandleError(Exception ex, [CallerMemberName] string? methodName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath ?? "Unknown");
        var location = $"{fileName}:{methodName} (line {lineNumber})";
        
        string detailedMessage;
        if (ex.Message.Contains("managed identity", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("AZURE_CLIENT_ID", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("ManagedIdentityCredential", StringComparison.OrdinalIgnoreCase))
        {
            detailedMessage = $"Managed Identity Error: {ex.Message}. " +
                "FIX: Ensure the AZURE_CLIENT_ID environment variable is set in Azure App Service settings " +
                "with the Client ID of the user-assigned managed identity. Also verify the managed identity " +
                "has been granted db_datareader and db_datawriter roles on the database.";
        }
        else if (ex.Message.Contains("Login failed", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            detailedMessage = $"Database Authentication Error: {ex.Message}. " +
                "FIX: Verify the connection string uses 'Authentication=Active Directory Managed Identity' " +
                "and the managed identity has been added as a database user with proper permissions.";
        }
        else if (ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            detailedMessage = $"Database Connection Error: {ex.Message}. " +
                "FIX: Check that the SQL Server firewall allows connections from Azure services " +
                "and the connection string server name is correct.";
        }
        else
        {
            detailedMessage = $"Database Error: {ex.Message}";
        }

        _logger.LogError(ex, "Error in {Method} at {Location}: {Message}", methodName, location, detailedMessage);
        _errorState.SetError(detailedMessage, location);
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetStatusesAsync();
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetStatuses();
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetUsersAsync();
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetUsers();
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetUserByIdAsync(userId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetUsers().FirstOrDefault(u => u.UserId == userId);
        }
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, int? categoryId = null, string? searchTerm = null)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetExpensesAsync(userId, statusId, categoryId, searchTerm);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            var expenses = DummyData.GetExpenses();
            if (userId.HasValue)
                expenses = expenses.Where(e => e.UserId == userId.Value).ToList();
            if (statusId.HasValue)
                expenses = expenses.Where(e => e.StatusId == statusId.Value).ToList();
            if (categoryId.HasValue)
                expenses = expenses.Where(e => e.CategoryId == categoryId.Value).ToList();
            if (!string.IsNullOrEmpty(searchTerm))
                expenses = expenses.Where(e => e.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true).ToList();
            return expenses;
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetExpenseByIdAsync(expenseId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync(string? searchTerm = null)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetPendingExpensesAsync(searchTerm);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            var expenses = DummyData.GetExpenses().Where(e => e.StatusName == "Submitted").ToList();
            if (!string.IsNullOrEmpty(searchTerm))
                expenses = expenses.Where(e => e.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true).ToList();
            return expenses;
        }
    }

    public async Task<int> CreateExpenseAsync(int userId, int categoryId, int amountMinor, DateTime expenseDate, string? description)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.CreateExpenseAsync(userId, categoryId, amountMinor, expenseDate, description);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            // Return a fake ID for dummy mode
            return DummyData.GetExpenses().Max(e => e.ExpenseId) + 1;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int expenseId, int categoryId, int amountMinor, DateTime expenseDate, string? description)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.UpdateExpenseAsync(expenseId, categoryId, amountMinor, expenseDate, description);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return true; // Pretend it succeeded in dummy mode
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.SubmitExpenseAsync(expenseId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return true;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.ApproveExpenseAsync(expenseId, reviewerId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return true;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.RejectExpenseAsync(expenseId, reviewerId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return true;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.DeleteExpenseAsync(expenseId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return true;
        }
    }

    public async Task<List<ExpenseSummary>> GetExpenseSummaryAsync(int? userId = null)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetExpenseSummaryAsync(userId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetExpenseSummary();
        }
    }

    public async Task<List<CategorySummary>> GetExpensesByCategoryAsync(int? userId = null)
    {
        try
        {
            _errorState.ClearError();
            return await _realService.GetExpensesByCategoryAsync(userId);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return DummyData.GetExpensesByCategory();
        }
    }
}
