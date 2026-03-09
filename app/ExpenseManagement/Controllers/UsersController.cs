using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public UsersController(IExpenseService expenseService)
        => _expenseService = expenseService;

    /// <summary>Get all active users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<User>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var (users, error) = await _expenseService.GetUsersAsync();
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(users);
    }
}
