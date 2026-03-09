using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

/// <summary>
/// REST API for expense management operations.
/// All database interactions go through stored procedures via IExpenseService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
        => _expenseService = expenseService;

    /// <summary>Get all expenses, optionally filtered.</summary>
    /// <param name="filter">Text search across category, description, status, user name.</param>
    /// <param name="status">Filter by status: Draft, Submitted, Approved, Rejected.</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<Expense>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? filter, [FromQuery] string? status)
    {
        var (expenses, error) = await _expenseService.GetAllExpensesAsync(filter, status);
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(expenses);
    }

    /// <summary>Get a single expense by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Expense), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
        if (error != null) Response.Headers["X-Error"] = error;
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    /// <summary>Create a new expense (status = Draft).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (newId, error) = await _expenseService.CreateExpenseAsync(request);
        if (error != null) return BadRequest(new { error });
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { expenseId = newId });
    }

    /// <summary>Submit a Draft expense (Draft -> Submitted).</summary>
    [HttpPost("{id:int}/submit")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Submit(int id, [FromQuery] int userId)
    {
        var (success, error) = await _expenseService.SubmitExpenseAsync(id, userId);
        if (!success) return BadRequest(new { error });
        return Ok(new { message = "Expense submitted successfully." });
    }

    /// <summary>Approve a Submitted expense (Submitted -> Approved).</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Approve(int id, [FromBody] UpdateStatusRequest request)
    {
        var (success, error) = await _expenseService.ApproveExpenseAsync(id, request.ReviewerUserId);
        if (!success) return BadRequest(new { error });
        return Ok(new { message = "Expense approved successfully." });
    }

    /// <summary>Reject a Submitted expense (Submitted -> Rejected).</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Reject(int id, [FromBody] UpdateStatusRequest request)
    {
        var (success, error) = await _expenseService.RejectExpenseAsync(id, request.ReviewerUserId);
        if (!success) return BadRequest(new { error });
        return Ok(new { message = "Expense rejected successfully." });
    }

    /// <summary>Get all expenses awaiting manager approval (status = Submitted).</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<Expense>), 200)]
    public async Task<IActionResult> GetPending([FromQuery] string? filter)
    {
        var (expenses, error) = await _expenseService.GetPendingExpensesAsync(filter);
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(expenses);
    }
}
