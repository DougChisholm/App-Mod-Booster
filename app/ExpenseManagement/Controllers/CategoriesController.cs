using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public CategoriesController(IExpenseService expenseService)
        => _expenseService = expenseService;

    /// <summary>Get all active expense categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Category>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var (categories, error) = await _expenseService.GetCategoriesAsync();
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(categories);
    }
}
