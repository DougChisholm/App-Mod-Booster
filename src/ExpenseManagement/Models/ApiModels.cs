namespace ExpenseManagement.Models;

public class CreateExpenseRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
}

public class UpdateExpenseRequest
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
}

public class ApproveRejectRequest
{
    public int ReviewerId { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? ErrorLocation { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error, string? location = null) => 
        new() { Success = false, Error = error, ErrorLocation = location };
}
