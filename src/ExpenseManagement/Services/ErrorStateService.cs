namespace ExpenseManagement.Services;

public interface IErrorStateService
{
    string? ErrorMessage { get; }
    string? ErrorLocation { get; }
    bool HasError { get; }
    void SetError(string message, string location);
    void ClearError();
}

public class ErrorStateService : IErrorStateService
{
    public string? ErrorMessage { get; private set; }
    public string? ErrorLocation { get; private set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public void SetError(string message, string location)
    {
        ErrorMessage = message;
        ErrorLocation = location;
    }

    public void ClearError()
    {
        ErrorMessage = null;
        ErrorLocation = null;
    }
}
