using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<List<Expense>> GetExpensesByUserAsync(int userId);
    Task<List<Expense>> GetPendingExpensesAsync();
    Task<List<Expense>> SearchExpensesAsync(string searchTerm);
    Task<int> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> UpdateExpenseAsync(UpdateExpenseRequest request);
    Task<bool> SubmitExpenseAsync(int expenseId);
    Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<bool> RejectExpenseAsync(int expenseId, int reviewerId);
    Task<bool> DeleteExpenseAsync(int expenseId);
    Task<List<Category>> GetCategoriesAsync();
    Task<List<User>> GetUsersAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;
    private readonly bool _useDummyData;
    private string? _lastError;

    public string? LastError => _lastError;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _logger = logger;
        _useDummyData = string.IsNullOrEmpty(_connectionString);
        
        if (_useDummyData)
        {
            _lastError = "Database connection string not configured. Using dummy data.";
            _logger.LogWarning(_lastError);
        }
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        if (_useDummyData) return GetDummyExpenses();

        try
        {
            var expenses = new List<Expense>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetAllExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetAllExpensesAsync));
            _logger.LogError(ex, "Error getting all expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        if (_useDummyData) return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return MapExpenseFromReader(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetExpenseByIdAsync));
            _logger.LogError(ex, "Error getting expense by ID: {ExpenseId}", expenseId);
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<List<Expense>> GetExpensesByUserAsync(int userId)
    {
        if (_useDummyData) return GetDummyExpenses().Where(e => e.UserId == userId).ToList();

        try
        {
            var expenses = new List<Expense>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetExpensesByUser", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetExpensesByUserAsync));
            _logger.LogError(ex, "Error getting expenses by user: {UserId}", userId);
            return GetDummyExpenses().Where(e => e.UserId == userId).ToList();
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        if (_useDummyData) return GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();

        try
        {
            var expenses = new List<Expense>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetPendingExpensesAsync));
            _logger.LogError(ex, "Error getting pending expenses");
            return GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList();
        }
    }

    public async Task<List<Expense>> SearchExpensesAsync(string searchTerm)
    {
        if (_useDummyData)
        {
            return GetDummyExpenses()
                .Where(e => (e.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           e.CategoryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           e.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        try
        {
            var expenses = new List<Expense>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_SearchExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SearchTerm", searchTerm);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(SearchExpensesAsync));
            _logger.LogError(ex, "Error searching expenses: {SearchTerm}", searchTerm);
            return GetDummyExpenses();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        if (_useDummyData)
        {
            _lastError = "Cannot create expense: Database not connected. This is a demo with dummy data.";
            return 0;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            var outputParam = command.Parameters.Add("@ExpenseId", SqlDbType.Int);
            outputParam.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync();

            _lastError = null;
            return (int)outputParam.Value;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(CreateExpenseAsync));
            _logger.LogError(ex, "Error creating expense");
            return 0;
        }
    }

    public async Task<bool> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        if (_useDummyData)
        {
            _lastError = "Cannot update expense: Database not connected. This is a demo with dummy data.";
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            _lastError = null;
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(UpdateExpenseAsync));
            _logger.LogError(ex, "Error updating expense: {ExpenseId}", request.ExpenseId);
            return false;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        if (_useDummyData)
        {
            _lastError = "Cannot submit expense: Database not connected. This is a demo with dummy data.";
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            var result = await command.ExecuteScalarAsync();
            _lastError = null;
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(SubmitExpenseAsync));
            _logger.LogError(ex, "Error submitting expense: {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        if (_useDummyData)
        {
            _lastError = "Cannot approve expense: Database not connected. This is a demo with dummy data.";
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            var result = await command.ExecuteScalarAsync();
            _lastError = null;
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(ApproveExpenseAsync));
            _logger.LogError(ex, "Error approving expense: {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        if (_useDummyData)
        {
            _lastError = "Cannot reject expense: Database not connected. This is a demo with dummy data.";
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);

            var result = await command.ExecuteScalarAsync();
            _lastError = null;
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(RejectExpenseAsync));
            _logger.LogError(ex, "Error rejecting expense: {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        if (_useDummyData)
        {
            _lastError = "Cannot delete expense: Database not connected. This is a demo with dummy data.";
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            var result = await command.ExecuteScalarAsync();
            _lastError = null;
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(DeleteExpenseAsync));
            _logger.LogError(ex, "Error deleting expense: {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        if (_useDummyData) return GetDummyCategories();

        try
        {
            var categories = new List<Category>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetAllCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            _lastError = null;
            return categories;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetCategoriesAsync));
            _logger.LogError(ex, "Error getting categories");
            return GetDummyCategories();
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        if (_useDummyData) return GetDummyUsers();

        try
        {
            var users = new List<User>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetAllUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                    ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                    ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            _lastError = null;
            return users;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetUsersAsync));
            _logger.LogError(ex, "Error getting users");
            return GetDummyUsers();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        if (_useDummyData) return GetDummyStatuses();

        try
        {
            var statuses = new List<ExpenseStatus>();
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetAllStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }

            _lastError = null;
            return statuses;
        }
        catch (Exception ex)
        {
            _lastError = FormatError(ex, nameof(GetStatusesAsync));
            _logger.LogError(ex, "Error getting statuses");
            return GetDummyStatuses();
        }
    }

    private Expense MapExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? "GBP" : reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private string FormatError(Exception ex, string methodName)
    {
        var errorMessage = $"Error in {methodName}: {ex.Message}";
        
        // Add managed identity specific guidance
        if (ex.Message.Contains("Managed Identity") || ex.Message.Contains("authentication") || ex.Message.Contains("token"))
        {
            errorMessage += "\n\nManaged Identity Issue: Ensure the App Service has a User-Assigned Managed Identity configured, " +
                           "and that the identity has been granted access to the SQL database using:\n" +
                           "CREATE USER [identity-name] FROM EXTERNAL PROVIDER;\n" +
                           "ALTER ROLE db_datareader ADD MEMBER [identity-name];\n" +
                           "ALTER ROLE db_datawriter ADD MEMBER [identity-name];";
        }
        
        return errorMessage;
    }

    // Dummy data for when database is not available
    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 12000,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 15),
                Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 2,
                CategoryName = "Food",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 6900,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 1, 10),
                Description = "Client lunch meeting",
                SubmittedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 3,
                CategoryName = "Office Supplies",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 9950,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 12, 4),
                Description = "Office stationery",
                SubmittedAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-11)
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Transport",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 1920,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 12, 18),
                Description = "Train tickets to conference",
                SubmittedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };
    }

    private List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Food", IsActive = true },
            new Category { CategoryId = 3, CategoryName = "Office Supplies", IsActive = true },
            new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true },
            new User { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }
}
