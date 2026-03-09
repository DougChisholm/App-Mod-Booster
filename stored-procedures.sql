/*
  stored-procedures.sql
  Stored procedures for the Expense Management System
  All app code uses these stored procedures - no direct T-SQL in the application.
  Use CREATE OR ALTER PROCEDURE to be idempotent (safe to run multiple times).
*/

-- ============================================================
-- sp_GetAllExpenses
-- Returns all expenses with joined category, status and user info.
-- Optional @Filter parameter performs a LIKE search across
-- category name, description, and status name.
-- Optional @StatusFilter restricts to a specific status name.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetAllExpenses
    @Filter       NVARCHAR(255) = NULL,
    @StatusFilter NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rev.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u             ON e.UserId     = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s     ON e.StatusId   = s.StatusId
    LEFT JOIN dbo.Users rev      ON e.ReviewedBy = rev.UserId
    WHERE
        (@Filter IS NULL OR (
            c.CategoryName  LIKE '%' + @Filter + '%' OR
            e.Description   LIKE '%' + @Filter + '%' OR
            s.StatusName    LIKE '%' + @Filter + '%' OR
            u.UserName      LIKE '%' + @Filter + '%'
        ))
        AND
        (@StatusFilter IS NULL OR s.StatusName = @StatusFilter)
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO

-- ============================================================
-- sp_GetExpenseById
-- Returns a single expense record by its primary key.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rev.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u             ON e.UserId     = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s     ON e.StatusId   = s.StatusId
    LEFT JOIN dbo.Users rev      ON e.ReviewedBy = rev.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- ============================================================
-- sp_CreateExpense
-- Creates a new expense with status = Draft.
-- Returns the newly created ExpenseId.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_CreateExpense
    @UserId      INT,
    @CategoryId  INT,
    @AmountMinor INT,
    @Currency    NVARCHAR(3)    = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DraftStatusId INT;
    SELECT @DraftStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';

    INSERT INTO dbo.Expenses
        (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES
        (@UserId, @CategoryId, @DraftStatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());

    SELECT SCOPE_IDENTITY() AS NewExpenseId;
END
GO

-- ============================================================
-- sp_SubmitExpense
-- Transitions an expense from Draft -> Submitted.
-- Only the expense owner may submit.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_SubmitExpense
    @ExpenseId INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';

    UPDATE dbo.Expenses
    SET StatusId    = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId
      AND UserId    = @UserId
      AND StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ============================================================
-- sp_ApproveExpense
-- Transitions an expense from Submitted -> Approved.
-- @ReviewerUserId is the manager performing the action.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_ApproveExpense
    @ExpenseId     INT,
    @ReviewerUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ApprovedStatusId INT;
    SELECT @ApprovedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';

    UPDATE dbo.Expenses
    SET StatusId   = @ApprovedStatusId,
        ReviewedBy = @ReviewerUserId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId
      AND StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ============================================================
-- sp_RejectExpense
-- Transitions an expense from Submitted -> Rejected.
-- @ReviewerUserId is the manager performing the action.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_RejectExpense
    @ExpenseId      INT,
    @ReviewerUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RejectedStatusId INT;
    SELECT @RejectedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';

    UPDATE dbo.Expenses
    SET StatusId   = @RejectedStatusId,
        ReviewedBy = @ReviewerUserId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId
      AND StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ============================================================
-- sp_GetPendingExpenses
-- Returns all expenses with status = Submitted (manager review queue).
-- Optional @Filter applies a LIKE search.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetPendingExpenses
    @Filter NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u             ON e.UserId     = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s     ON e.StatusId   = s.StatusId
    WHERE s.StatusName = 'Submitted'
      AND (@Filter IS NULL OR (
            c.CategoryName LIKE '%' + @Filter + '%' OR
            e.Description  LIKE '%' + @Filter + '%' OR
            u.UserName     LIKE '%' + @Filter + '%'
          ))
    ORDER BY e.SubmittedAt ASC;
END
GO

-- ============================================================
-- sp_GetExpenseCategories
-- Returns all active expense categories.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetExpenseCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- ============================================================
-- sp_GetAllUsers
-- Returns all active users with their role names.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- ============================================================
-- sp_GetExpensesByUser
-- Returns all expenses for a specific user.
-- Optional @Filter and @StatusFilter supported.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetExpensesByUser
    @UserId       INT,
    @Filter       NVARCHAR(255) = NULL,
    @StatusFilter NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rev.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u             ON e.UserId     = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s     ON e.StatusId   = s.StatusId
    LEFT JOIN dbo.Users rev      ON e.ReviewedBy = rev.UserId
    WHERE e.UserId = @UserId
      AND (@Filter IS NULL OR (
            c.CategoryName LIKE '%' + @Filter + '%' OR
            e.Description  LIKE '%' + @Filter + '%' OR
            s.StatusName   LIKE '%' + @Filter + '%'
          ))
      AND (@StatusFilter IS NULL OR s.StatusName = @StatusFilter)
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO
