namespace ExpenseManagement.Settings;

public class GenAISettings
{
    public string Endpoint       { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o";
    public string SearchEndpoint { get; set; } = string.Empty;
}
