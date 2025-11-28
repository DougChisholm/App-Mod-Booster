using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all expenses
    /// </summary>
    /// <returns>List of all expenses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetAll()
    {
        var expenses = await _expenseService.GetAllExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Gets a specific expense by ID
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <returns>The expense</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Expense), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Expense>> GetById(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }

    /// <summary>
    /// Gets expenses by user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of expenses for the user</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetByUser(int userId)
    {
        var expenses = await _expenseService.GetExpensesByUserAsync(userId);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets all pending expenses awaiting approval
    /// </summary>
    /// <returns>List of pending expenses</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> GetPending()
    {
        var expenses = await _expenseService.GetPendingExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Searches expenses by description, category, or user
    /// </summary>
    /// <param name="term">Search term</param>
    /// <returns>List of matching expenses</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Expense>>> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Ok(await _expenseService.GetAllExpensesAsync());
        }
        var expenses = await _expenseService.SearchExpensesAsync(term);
        return Ok(expenses);
    }

    /// <summary>
    /// Creates a new expense
    /// </summary>
    /// <param name="request">The expense details</param>
    /// <returns>The created expense ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create([FromBody] CreateExpenseRequest request)
    {
        var expenseId = await _expenseService.CreateExpenseAsync(request);
        if (expenseId == 0)
        {
            return BadRequest("Failed to create expense");
        }
        return CreatedAtAction(nameof(GetById), new { id = expenseId }, expenseId);
    }

    /// <summary>
    /// Updates an existing expense
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <param name="request">The updated expense details</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest request)
    {
        if (id != request.ExpenseId)
        {
            return BadRequest("ID mismatch");
        }

        var success = await _expenseService.UpdateExpenseAsync(request);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Submits an expense for approval
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(int id)
    {
        var success = await _expenseService.SubmitExpenseAsync(id);
        if (!success)
        {
            return BadRequest("Failed to submit expense");
        }
        return NoContent();
    }

    /// <summary>
    /// Approves an expense (manager action)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <param name="reviewerId">The reviewer/manager ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(int id, [FromQuery] int reviewerId = 2)
    {
        var success = await _expenseService.ApproveExpenseAsync(id, reviewerId);
        if (!success)
        {
            return BadRequest("Failed to approve expense");
        }
        return NoContent();
    }

    /// <summary>
    /// Rejects an expense (manager action)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <param name="reviewerId">The reviewer/manager ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(int id, [FromQuery] int reviewerId = 2)
    {
        var success = await _expenseService.RejectExpenseAsync(id, reviewerId);
        if (!success)
        {
            return BadRequest("Failed to reject expense");
        }
        return NoContent();
    }

    /// <summary>
    /// Deletes an expense (only drafts can be deleted)
    /// </summary>
    /// <param name="id">The expense ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _expenseService.DeleteExpenseAsync(id);
        if (!success)
        {
            return BadRequest("Failed to delete expense. Only draft expenses can be deleted.");
        }
        return NoContent();
    }
}
