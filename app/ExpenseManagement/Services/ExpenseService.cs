using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Services;

/// <summary>
/// Implements all database operations via stored procedures.
/// Connects to Azure SQL using the user-assigned Managed Identity - no username/password.
/// Falls back to dummy data when the database is unreachable, and surfaces a detailed
/// error message (including class/method name) for display in the header error bar.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration config, ILogger<ExpenseService> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        _logger = logger;
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    private static string BuildError(Exception ex, string context) =>
        $"Database error in ExpenseService.{context}: {ex.GetType().Name} – {ex.Message}. " +
        $"If using Managed Identity, ensure the identity has been granted db_datareader/db_datawriter " +
        $"roles on the Northwind database (run run-sql-dbrole.py) and that the App Service has " +
        $"AZURE_CLIENT_ID set to the managed identity client ID.";

    public async Task<(List<Expense> Expenses, string? ErrorMessage)> GetAllExpensesAsync(
        string? filter = null, string? statusFilter = null)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetAllExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Filter",       (object?)filter       ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
            return (await ReadExpensesAsync(cmd), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllExpenses failed");
            return (GetDummyExpenses(), BuildError(ex, "GetAllExpensesAsync"));
        }
    }

    public async Task<(Expense? Expense, string? ErrorMessage)> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetExpenseById", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            var list = await ReadExpensesAsync(cmd);
            return (list.FirstOrDefault(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetExpenseById failed");
            return (null, BuildError(ex, "GetExpenseByIdAsync"));
        }
    }

    public async Task<(int NewId, string? ErrorMessage)> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_CreateExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId",      request.UserId);
            cmd.Parameters.AddWithValue("@CategoryId",  request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency",    request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return (Convert.ToInt32(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateExpense failed");
            return (-1, BuildError(ex, "CreateExpenseAsync"));
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SubmitExpenseAsync(int expenseId, int userId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_SubmitExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@UserId",    userId);
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return (rows > 0, rows == 0 ? "Expense not found or not in Draft status." : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitExpense failed");
            return (false, BuildError(ex, "SubmitExpenseAsync"));
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ApproveExpenseAsync(int expenseId, int reviewerUserId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_ApproveExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId",      expenseId);
            cmd.Parameters.AddWithValue("@ReviewerUserId", reviewerUserId);
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return (rows > 0, rows == 0 ? "Expense not found or not in Submitted status." : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApproveExpense failed");
            return (false, BuildError(ex, "ApproveExpenseAsync"));
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RejectExpenseAsync(int expenseId, int reviewerUserId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_RejectExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId",      expenseId);
            cmd.Parameters.AddWithValue("@ReviewerUserId", reviewerUserId);
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return (rows > 0, rows == 0 ? "Expense not found or not in Submitted status." : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RejectExpense failed");
            return (false, BuildError(ex, "RejectExpenseAsync"));
        }
    }

    public async Task<(List<Expense> Expenses, string? ErrorMessage)> GetPendingExpensesAsync(string? filter = null)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetPendingExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Filter", (object?)filter ?? DBNull.Value);
            return (await ReadExpensesAsync(cmd), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPendingExpenses failed");
            return (GetDummyPendingExpenses(), BuildError(ex, "GetPendingExpensesAsync"));
        }
    }

    public async Task<(List<Category> Categories, string? ErrorMessage)> GetCategoriesAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetExpenseCategories", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Category>();
            while (await reader.ReadAsync())
            {
                list.Add(new Category
                {
                    CategoryId   = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
                });
            }
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCategories failed");
            return (GetDummyCategories(), BuildError(ex, "GetCategoriesAsync"));
        }
    }

    public async Task<(List<User> Users, string? ErrorMessage)> GetUsersAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetAllUsers", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<User>();
            while (await reader.ReadAsync())
            {
                list.Add(new User
                {
                    UserId    = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName  = reader.GetString(reader.GetOrdinal("UserName")),
                    Email     = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId    = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName  = reader.GetString(reader.GetOrdinal("RoleName")),
                    ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                    IsActive  = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUsers failed");
            return (GetDummyUsers(), BuildError(ex, "GetUsersAsync"));
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static async Task<List<Expense>> ReadExpensesAsync(SqlCommand cmd)
    {
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Expense>();
        while (await reader.ReadAsync())
        {
            list.Add(new Expense
            {
                ExpenseId      = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
                UserId         = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName       = reader.GetString(reader.GetOrdinal("UserName")),
                Email          = reader.GetString(reader.GetOrdinal("Email")),
                CategoryId     = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName   = reader.GetString(reader.GetOrdinal("CategoryName")),
                StatusId       = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName     = reader.GetString(reader.GetOrdinal("StatusName")),
                AmountMinor    = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
                AmountGBP      = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
                Currency       = reader.GetString(reader.GetOrdinal("Currency")),
                ExpenseDate    = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                Description    = reader.IsDBNull(reader.GetOrdinal("Description"))    ? null : reader.GetString(reader.GetOrdinal("Description")),
                ReceiptFile    = reader.IsDBNull(reader.GetOrdinal("ReceiptFile"))    ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
                SubmittedAt    = reader.IsDBNull(reader.GetOrdinal("SubmittedAt"))    ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                ReviewedBy     = reader.IsDBNull(reader.GetOrdinal("ReviewedBy"))    ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
                ReviewedByName = reader.IsDBNull(reader.GetOrdinal("ReviewedByName"))? null : reader.GetString(reader.GetOrdinal("ReviewedByName")),
                ReviewedAt     = reader.IsDBNull(reader.GetOrdinal("ReviewedAt"))    ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
                CreatedAt      = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }
        return list;
    }

    private static List<Expense> GetDummyExpenses() => new()
    {
        new Expense { ExpenseId=1, UserId=1, UserName="Alice Example", Email="alice@example.co.uk", CategoryId=1, CategoryName="Travel",       StatusId=2, StatusName="Submitted", AmountMinor=2540,  AmountGBP=25.40m,  Currency="GBP", ExpenseDate=new DateTime(2025,10,20), Description="Taxi from airport to client site",  CreatedAt=DateTime.UtcNow },
        new Expense { ExpenseId=2, UserId=1, UserName="Alice Example", Email="alice@example.co.uk", CategoryId=2, CategoryName="Meals",        StatusId=3, StatusName="Approved",  AmountMinor=1425,  AmountGBP=14.25m,  Currency="GBP", ExpenseDate=new DateTime(2025,9,15),  Description="Client lunch meeting",              CreatedAt=DateTime.UtcNow },
        new Expense { ExpenseId=3, UserId=1, UserName="Alice Example", Email="alice@example.co.uk", CategoryId=3, CategoryName="Supplies",     StatusId=1, StatusName="Draft",     AmountMinor=799,   AmountGBP=7.99m,   Currency="GBP", ExpenseDate=new DateTime(2025,11,1),  Description="Office stationery",                 CreatedAt=DateTime.UtcNow },
        new Expense { ExpenseId=4, UserId=1, UserName="Alice Example", Email="alice@example.co.uk", CategoryId=4, CategoryName="Accommodation",StatusId=3, StatusName="Approved",  AmountMinor=12300, AmountGBP=123.00m, Currency="GBP", ExpenseDate=new DateTime(2025,8,10),  Description="Hotel during client visit",         CreatedAt=DateTime.UtcNow }
    };

    private static List<Expense> GetDummyPendingExpenses() => new()
    {
        new Expense { ExpenseId=1, UserId=1, UserName="Alice Example", Email="alice@example.co.uk", CategoryId=1, CategoryName="Travel",   StatusId=2, StatusName="Submitted", AmountMinor=2540, AmountGBP=25.40m, Currency="GBP", ExpenseDate=new DateTime(2025,10,20), Description="Taxi from airport to client site", CreatedAt=DateTime.UtcNow },
        new Expense { ExpenseId=5, UserId=1, UserName="Alice Example", Email="alice@example.co.uk", CategoryId=3, CategoryName="Supplies", StatusId=2, StatusName="Submitted", AmountMinor=9950, AmountGBP=99.50m, Currency="GBP", ExpenseDate=new DateTime(2025,12,14), Description="Office Supplies",                  CreatedAt=DateTime.UtcNow }
    };

    private static List<Category> GetDummyCategories() => new()
    {
        new Category { CategoryId=1, CategoryName="Travel"        },
        new Category { CategoryId=2, CategoryName="Meals"         },
        new Category { CategoryId=3, CategoryName="Supplies"      },
        new Category { CategoryId=4, CategoryName="Accommodation" },
        new Category { CategoryId=5, CategoryName="Other"         }
    };

    private static List<User> GetDummyUsers() => new()
    {
        new User { UserId=1, UserName="Alice Example", Email="alice@example.co.uk",       RoleId=1, RoleName="Employee", IsActive=true, CreatedAt=DateTime.UtcNow },
        new User { UserId=2, UserName="Bob Manager",   Email="bob.manager@example.co.uk", RoleId=2, RoleName="Manager",  IsActive=true, CreatedAt=DateTime.UtcNow }
    };
}
