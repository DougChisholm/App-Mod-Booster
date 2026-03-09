using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<(List<Expense> Expenses, string? ErrorMessage)> GetAllExpensesAsync(string? filter = null, string? statusFilter = null);
    Task<(Expense? Expense, string? ErrorMessage)>       GetExpenseByIdAsync(int expenseId);
    Task<(int NewId, string? ErrorMessage)>              CreateExpenseAsync(CreateExpenseRequest request);
    Task<(bool Success, string? ErrorMessage)>           SubmitExpenseAsync(int expenseId, int userId);
    Task<(bool Success, string? ErrorMessage)>           ApproveExpenseAsync(int expenseId, int reviewerUserId);
    Task<(bool Success, string? ErrorMessage)>           RejectExpenseAsync(int expenseId, int reviewerUserId);
    Task<(List<Expense> Expenses, string? ErrorMessage)> GetPendingExpensesAsync(string? filter = null);
    Task<(List<Category> Categories, string? ErrorMessage)> GetCategoriesAsync();
    Task<(List<User> Users, string? ErrorMessage)>       GetUsersAsync();
}
