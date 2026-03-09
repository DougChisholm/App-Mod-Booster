using ExpenseManagement.Services;
using ExpenseManagement.Settings;
using Microsoft.OpenApi.Models;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages + API Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Expense Management API",
        Version     = "v1",
        Description = "REST API for the Expense Management System. Use /Index to view the web UI."
    });
});

// Bind GenAI settings from appsettings / environment variables
builder.Services.Configure<GenAISettings>(builder.Configuration.GetSection("GenAI"));

// Register services
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

// Swagger UI always enabled (helpful for testing)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
