namespace ExpenseManagement.Models;

public class User
{
    public int    UserId    { get; set; }
    public string UserName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public int    RoleId    { get; set; }
    public string RoleName  { get; set; } = string.Empty;
    public int?   ManagerId { get; set; }
    public bool   IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}
