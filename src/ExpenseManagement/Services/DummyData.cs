using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public static class DummyData
{
    public static List<ExpenseCategory> GetCategories() => new()
    {
        new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
        new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
        new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
        new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
        new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
    };

    public static List<ExpenseStatus> GetStatuses() => new()
    {
        new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
        new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
        new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
        new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
    };

    public static List<User> GetUsers() => new()
    {
        new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true },
        new User { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true }
    };

    public static List<Expense> GetExpenses() => new()
    {
        new Expense
        {
            ExpenseId = 1,
            UserId = 1,
            UserName = "Alice Example",
            CategoryId = 1,
            CategoryName = "Travel",
            StatusId = 2,
            StatusName = "Submitted",
            AmountMinor = 12000,
            AmountDisplay = 120.00m,
            Currency = "GBP",
            ExpenseDate = new DateTime(2024, 1, 15),
            Description = "Taxi to client site",
            SubmittedAt = DateTime.UtcNow.AddDays(-5)
        },
        new Expense
        {
            ExpenseId = 2,
            UserId = 1,
            UserName = "Alice Example",
            CategoryId = 2,
            CategoryName = "Food",
            StatusId = 2,
            StatusName = "Submitted",
            AmountMinor = 6900,
            AmountDisplay = 69.00m,
            Currency = "GBP",
            ExpenseDate = new DateTime(2023, 1, 10),
            Description = "Client lunch",
            SubmittedAt = DateTime.UtcNow.AddDays(-3)
        },
        new Expense
        {
            ExpenseId = 3,
            UserId = 1,
            UserName = "Alice Example",
            CategoryId = 3,
            CategoryName = "Office Supplies",
            StatusId = 3,
            StatusName = "Approved",
            AmountMinor = 9950,
            AmountDisplay = 99.50m,
            Currency = "GBP",
            ExpenseDate = new DateTime(2023, 12, 4),
            Description = "Stationery",
            SubmittedAt = DateTime.UtcNow.AddDays(-10),
            ReviewedBy = 2,
            ReviewerName = "Bob Manager",
            ReviewedAt = DateTime.UtcNow.AddDays(-8)
        },
        new Expense
        {
            ExpenseId = 4,
            UserId = 1,
            UserName = "Alice Example",
            CategoryId = 1,
            CategoryName = "Transport",
            StatusId = 3,
            StatusName = "Approved",
            AmountMinor = 1920,
            AmountDisplay = 19.20m,
            Currency = "GBP",
            ExpenseDate = new DateTime(2023, 1, 18),
            Description = "Bus fare",
            SubmittedAt = DateTime.UtcNow.AddDays(-15),
            ReviewedBy = 2,
            ReviewerName = "Bob Manager",
            ReviewedAt = DateTime.UtcNow.AddDays(-12)
        }
    };

    public static List<ExpenseSummary> GetExpenseSummary() => new()
    {
        new ExpenseSummary { StatusName = "Draft", ExpenseCount = 0, TotalAmountMinor = 0, TotalAmountDisplay = 0m },
        new ExpenseSummary { StatusName = "Submitted", ExpenseCount = 2, TotalAmountMinor = 18900, TotalAmountDisplay = 189.00m },
        new ExpenseSummary { StatusName = "Approved", ExpenseCount = 2, TotalAmountMinor = 11870, TotalAmountDisplay = 118.70m },
        new ExpenseSummary { StatusName = "Rejected", ExpenseCount = 0, TotalAmountMinor = 0, TotalAmountDisplay = 0m }
    };

    public static List<CategorySummary> GetExpensesByCategory() => new()
    {
        new CategorySummary { CategoryName = "Travel", ExpenseCount = 2, TotalAmountMinor = 13920, TotalAmountDisplay = 139.20m },
        new CategorySummary { CategoryName = "Meals", ExpenseCount = 1, TotalAmountMinor = 6900, TotalAmountDisplay = 69.00m },
        new CategorySummary { CategoryName = "Supplies", ExpenseCount = 1, TotalAmountMinor = 9950, TotalAmountDisplay = 99.50m }
    };
}
