using System.Data;
using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<ExpenseCategory>> GetCategoriesAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, int? categoryId = null, string? searchTerm = null);
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<List<Expense>> GetPendingExpensesAsync(string? searchTerm = null);
    Task<int> CreateExpenseAsync(int userId, int categoryId, int amountMinor, DateTime expenseDate, string? description);
    Task<bool> UpdateExpenseAsync(int expenseId, int categoryId, int amountMinor, DateTime expenseDate, string? description);
    Task<bool> SubmitExpenseAsync(int expenseId);
    Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<bool> RejectExpenseAsync(int expenseId, int reviewerId);
    Task<bool> DeleteExpenseAsync(int expenseId);
    Task<List<ExpenseSummary>> GetExpenseSummaryAsync(int? userId = null);
    Task<List<CategorySummary>> GetExpensesByCategoryAsync(int? userId = null);
}

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    private async Task<SqlConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32("CategoryId"),
                    CategoryName = reader.GetString("CategoryName"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories from database");
            throw;
        }
        return categories;
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32("StatusId"),
                    StatusName = reader.GetString("StatusName")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses from database");
            throw;
        }
        return statuses;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32("UserId"),
                    UserName = reader.GetString("UserName"),
                    Email = reader.GetString("Email"),
                    RoleId = reader.GetInt32("RoleId"),
                    RoleName = reader.GetString("RoleName"),
                    ManagerId = reader.IsDBNull("ManagerId") ? null : reader.GetInt32("ManagerId"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users from database");
            throw;
        }
        return users;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetUserById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32("UserId"),
                    UserName = reader.GetString("UserName"),
                    Email = reader.GetString("Email"),
                    RoleId = reader.GetInt32("RoleId"),
                    RoleName = reader.GetString("RoleName"),
                    ManagerId = reader.IsDBNull("ManagerId") ? null : reader.GetInt32("ManagerId"),
                    IsActive = reader.GetBoolean("IsActive")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} from database", userId);
            throw;
        }
        return null;
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, int? categoryId = null, string? searchTerm = null)
    {
        var expenses = new List<Expense>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses from database");
            throw;
        }
        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpenseFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId} from database", expenseId);
            throw;
        }
        return null;
    }

    public async Task<List<Expense>> GetPendingExpensesAsync(string? searchTerm = null)
    {
        var expenses = new List<Expense>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetPendingExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(new Expense
                {
                    ExpenseId = reader.GetInt32("ExpenseId"),
                    UserId = reader.GetInt32("UserId"),
                    UserName = reader.GetString("UserName"),
                    CategoryId = reader.GetInt32("CategoryId"),
                    CategoryName = reader.GetString("CategoryName"),
                    StatusId = reader.GetInt32("StatusId"),
                    StatusName = reader.GetString("StatusName"),
                    AmountMinor = reader.GetInt32("AmountMinor"),
                    AmountDisplay = reader.GetDecimal("AmountDisplay"),
                    Currency = reader.GetString("Currency"),
                    ExpenseDate = reader.GetDateTime("ExpenseDate"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                    SubmittedAt = reader.IsDBNull("SubmittedAt") ? null : reader.GetDateTime("SubmittedAt")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses from database");
            throw;
        }
        return expenses;
    }

    public async Task<int> CreateExpenseAsync(int userId, int categoryId, int amountMinor, DateTime expenseDate, string? description)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@AmountMinor", amountMinor);
            command.Parameters.AddWithValue("@ExpenseDate", expenseDate);
            command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", DBNull.Value);
            
            var outputParam = command.Parameters.Add("@ExpenseId", SqlDbType.Int);
            outputParam.Direction = ParameterDirection.Output;
            
            await command.ExecuteNonQueryAsync();
            return (int)outputParam.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense in database");
            throw;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int expenseId, int categoryId, int amountMinor, DateTime expenseDate, string? description)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@AmountMinor", amountMinor);
            command.Parameters.AddWithValue("@ExpenseDate", expenseDate);
            command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32("RowsAffected") > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId} in database", expenseId);
            throw;
        }
        return false;
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32("RowsAffected") > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            throw;
        }
        return false;
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32("RowsAffected") > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            throw;
        }
        return false;
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32("RowsAffected") > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            throw;
        }
        return false;
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32("RowsAffected") > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            throw;
        }
        return false;
    }

    public async Task<List<ExpenseSummary>> GetExpenseSummaryAsync(int? userId = null)
    {
        var summaries = new List<ExpenseSummary>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetExpenseSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new ExpenseSummary
                {
                    StatusName = reader.GetString("StatusName"),
                    ExpenseCount = reader.GetInt32("ExpenseCount"),
                    TotalAmountMinor = reader.GetInt32("TotalAmountMinor"),
                    TotalAmountDisplay = reader.GetDecimal("TotalAmountDisplay")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary from database");
            throw;
        }
        return summaries;
    }

    public async Task<List<CategorySummary>> GetExpensesByCategoryAsync(int? userId = null)
    {
        var summaries = new List<CategorySummary>();
        try
        {
            using var connection = await CreateConnectionAsync();
            using var command = new SqlCommand("usp_GetExpensesByCategory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new CategorySummary
                {
                    CategoryName = reader.GetString("CategoryName"),
                    ExpenseCount = reader.GetInt32("ExpenseCount"),
                    TotalAmountMinor = reader.GetInt32("TotalAmountMinor"),
                    TotalAmountDisplay = reader.GetDecimal("TotalAmountDisplay")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by category from database");
            throw;
        }
        return summaries;
    }

    private static Expense MapExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32("ExpenseId"),
            UserId = reader.GetInt32("UserId"),
            UserName = reader.GetString("UserName"),
            CategoryId = reader.GetInt32("CategoryId"),
            CategoryName = reader.GetString("CategoryName"),
            StatusId = reader.GetInt32("StatusId"),
            StatusName = reader.GetString("StatusName"),
            AmountMinor = reader.GetInt32("AmountMinor"),
            AmountDisplay = reader.GetDecimal("AmountDisplay"),
            Currency = reader.GetString("Currency"),
            ExpenseDate = reader.GetDateTime("ExpenseDate"),
            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
            ReceiptFile = reader.IsDBNull("ReceiptFile") ? null : reader.GetString("ReceiptFile"),
            SubmittedAt = reader.IsDBNull("SubmittedAt") ? null : reader.GetDateTime("SubmittedAt"),
            ReviewedBy = reader.IsDBNull("ReviewedBy") ? null : reader.GetInt32("ReviewedBy"),
            ReviewerName = reader.IsDBNull("ReviewerName") ? null : reader.GetString("ReviewerName"),
            ReviewedAt = reader.IsDBNull("ReviewedAt") ? null : reader.GetDateTime("ReviewedAt"),
            CreatedAt = reader.GetDateTime("CreatedAt")
        };
    }
}
