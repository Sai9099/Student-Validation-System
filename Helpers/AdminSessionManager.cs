using StudentValidationSystem.Models;

namespace StudentValidationSystem.Helpers;

public static class AdminSessionManager
{
    public static User? Current { get; set; }
}
