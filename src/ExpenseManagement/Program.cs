using ExpenseManagement.Services;
using ExpenseManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register services
builder.Services.AddSingleton<IErrorStateService, ErrorStateService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<IExpenseService, ExpenseServiceWithFallback>();
builder.Services.AddScoped<IChatService, ChatService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Expense Management API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// API Endpoints
app.MapGet("/api/categories", async (IExpenseService service) =>
{
    var categories = await service.GetCategoriesAsync();
    return Results.Ok(ApiResponse<List<ExpenseCategory>>.Ok(categories));
}).WithTags("Lookup");

app.MapGet("/api/statuses", async (IExpenseService service) =>
{
    var statuses = await service.GetStatusesAsync();
    return Results.Ok(ApiResponse<List<ExpenseStatus>>.Ok(statuses));
}).WithTags("Lookup");

app.MapGet("/api/users", async (IExpenseService service) =>
{
    var users = await service.GetUsersAsync();
    return Results.Ok(ApiResponse<List<User>>.Ok(users));
}).WithTags("Users");

app.MapGet("/api/users/{id}", async (int id, IExpenseService service) =>
{
    var user = await service.GetUserByIdAsync(id);
    return user != null 
        ? Results.Ok(ApiResponse<User>.Ok(user))
        : Results.NotFound(ApiResponse<User>.Fail("User not found"));
}).WithTags("Users");

app.MapGet("/api/expenses", async (int? userId, int? statusId, int? categoryId, string? search, IExpenseService service) =>
{
    var expenses = await service.GetExpensesAsync(userId, statusId, categoryId, search);
    return Results.Ok(ApiResponse<List<Expense>>.Ok(expenses));
}).WithTags("Expenses");

app.MapGet("/api/expenses/{id}", async (int id, IExpenseService service) =>
{
    var expense = await service.GetExpenseByIdAsync(id);
    return expense != null 
        ? Results.Ok(ApiResponse<Expense>.Ok(expense))
        : Results.NotFound(ApiResponse<Expense>.Fail("Expense not found"));
}).WithTags("Expenses");

app.MapGet("/api/expenses/pending", async (string? search, IExpenseService service) =>
{
    var expenses = await service.GetPendingExpensesAsync(search);
    return Results.Ok(ApiResponse<List<Expense>>.Ok(expenses));
}).WithTags("Expenses");

app.MapPost("/api/expenses", async (CreateExpenseRequest request, IExpenseService service) =>
{
    var amountMinor = (int)(request.Amount * 100);
    var id = await service.CreateExpenseAsync(request.UserId, request.CategoryId, amountMinor, request.ExpenseDate, request.Description);
    return Results.Created($"/api/expenses/{id}", ApiResponse<int>.Ok(id));
}).WithTags("Expenses");

app.MapPut("/api/expenses/{id}", async (int id, UpdateExpenseRequest request, IExpenseService service) =>
{
    var amountMinor = (int)(request.Amount * 100);
    var success = await service.UpdateExpenseAsync(id, request.CategoryId, amountMinor, request.ExpenseDate, request.Description);
    return success 
        ? Results.Ok(ApiResponse<bool>.Ok(true))
        : Results.NotFound(ApiResponse<bool>.Fail("Expense not found or update failed"));
}).WithTags("Expenses");

app.MapPost("/api/expenses/{id}/submit", async (int id, IExpenseService service) =>
{
    var success = await service.SubmitExpenseAsync(id);
    return success 
        ? Results.Ok(ApiResponse<bool>.Ok(true))
        : Results.BadRequest(ApiResponse<bool>.Fail("Failed to submit expense"));
}).WithTags("Expenses");

app.MapPost("/api/expenses/{id}/approve", async (int id, ApproveRejectRequest request, IExpenseService service) =>
{
    var success = await service.ApproveExpenseAsync(id, request.ReviewerId);
    return success 
        ? Results.Ok(ApiResponse<bool>.Ok(true))
        : Results.BadRequest(ApiResponse<bool>.Fail("Failed to approve expense"));
}).WithTags("Expenses");

app.MapPost("/api/expenses/{id}/reject", async (int id, ApproveRejectRequest request, IExpenseService service) =>
{
    var success = await service.RejectExpenseAsync(id, request.ReviewerId);
    return success 
        ? Results.Ok(ApiResponse<bool>.Ok(true))
        : Results.BadRequest(ApiResponse<bool>.Fail("Failed to reject expense"));
}).WithTags("Expenses");

app.MapDelete("/api/expenses/{id}", async (int id, IExpenseService service) =>
{
    var success = await service.DeleteExpenseAsync(id);
    return success 
        ? Results.Ok(ApiResponse<bool>.Ok(true))
        : Results.NotFound(ApiResponse<bool>.Fail("Expense not found or cannot be deleted"));
}).WithTags("Expenses");

app.MapGet("/api/expenses/summary", async (int? userId, IExpenseService service) =>
{
    var summary = await service.GetExpenseSummaryAsync(userId);
    return Results.Ok(ApiResponse<List<ExpenseSummary>>.Ok(summary));
}).WithTags("Reports");

app.MapGet("/api/expenses/by-category", async (int? userId, IExpenseService service) =>
{
    var summary = await service.GetExpensesByCategoryAsync(userId);
    return Results.Ok(ApiResponse<List<CategorySummary>>.Ok(summary));
}).WithTags("Reports");

// Chat API
app.MapPost("/api/chat", async (ChatRequest request, IChatService chatService) =>
{
    var response = await chatService.GetResponseAsync(request);
    return Results.Ok(response);
}).WithTags("Chat");

app.MapGet("/api/chat/status", (IChatService chatService) =>
{
    return Results.Ok(new { enabled = chatService.IsEnabled });
}).WithTags("Chat");

app.Run();
