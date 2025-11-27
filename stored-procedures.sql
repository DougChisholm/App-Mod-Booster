-- Stored Procedures for Expense Management System
-- These procedures abstract all data access from the application

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Get all expense categories
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetCategories]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- =============================================
-- Get all expense statuses
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetStatuses]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- =============================================
-- Get all users
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetUsers]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName, u.ManagerId, u.IsActive
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- =============================================
-- Get user by ID
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserById]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName, u.ManagerId, u.IsActive
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.UserId = @UserId;
END
GO

-- =============================================
-- Get all expenses with optional filtering
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpenses]
    @UserId INT = NULL,
    @StatusId INT = NULL,
    @CategoryId INT = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDisplay,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        m.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users m ON e.ReviewedBy = m.UserId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
      AND (@StatusId IS NULL OR e.StatusId = @StatusId)
      AND (@CategoryId IS NULL OR e.CategoryId = @CategoryId)
      AND (@SearchTerm IS NULL OR e.Description LIKE '%' + @SearchTerm + '%')
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO

-- =============================================
-- Get expense by ID
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpenseById]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDisplay,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        m.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users m ON e.ReviewedBy = m.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- =============================================
-- Get pending expenses for approval
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetPendingExpenses]
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDisplay,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.SubmittedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
      AND (@SearchTerm IS NULL OR e.Description LIKE '%' + @SearchTerm + '%' OR c.CategoryName LIKE '%' + @SearchTerm + '%')
    ORDER BY e.SubmittedAt ASC;
END
GO

-- =============================================
-- Create a new expense
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_CreateExpense]
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL,
    @ExpenseId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get Draft status ID
    DECLARE @DraftStatusId INT;
    SELECT @DraftStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES (@UserId, @CategoryId, @DraftStatusId, @AmountMinor, 'GBP', @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());
    
    SET @ExpenseId = SCOPE_IDENTITY();
END
GO

-- =============================================
-- Update an expense
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateExpense]
    @ExpenseId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Expenses
    SET CategoryId = @CategoryId,
        AmountMinor = @AmountMinor,
        ExpenseDate = @ExpenseDate,
        Description = @Description,
        ReceiptFile = ISNULL(@ReceiptFile, ReceiptFile)
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- Submit an expense for approval
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_SubmitExpense]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    
    UPDATE dbo.Expenses
    SET StatusId = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- Approve an expense
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_ApproveExpense]
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ApprovedStatusId INT;
    SELECT @ApprovedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET StatusId = @ApprovedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- Reject an expense
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_RejectExpense]
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RejectedStatusId INT;
    SELECT @RejectedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET StatusId = @RejectedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- Delete an expense (only drafts)
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteExpense]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only allow deleting draft expenses
    DECLARE @DraftStatusId INT;
    SELECT @DraftStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    DELETE FROM dbo.Expenses
    WHERE ExpenseId = @ExpenseId AND StatusId = @DraftStatusId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- Get expense summary by status
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpenseSummary]
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        s.StatusName,
        COUNT(*) AS ExpenseCount,
        SUM(e.AmountMinor) AS TotalAmountMinor,
        CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(10,2)) AS TotalAmountDisplay
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
    GROUP BY s.StatusId, s.StatusName
    ORDER BY s.StatusId;
END
GO

-- =============================================
-- Get expense summary by category
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpensesByCategory]
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        c.CategoryName,
        COUNT(*) AS ExpenseCount,
        SUM(e.AmountMinor) AS TotalAmountMinor,
        CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(10,2)) AS TotalAmountDisplay
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
    GROUP BY c.CategoryId, c.CategoryName
    ORDER BY TotalAmountMinor DESC;
END
GO
