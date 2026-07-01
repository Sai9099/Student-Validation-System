namespace StudentValidationSystem.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
    public string RegisterNumber { get; set; } = "";
    public string FacultyId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Email { get; set; } = "";
}
