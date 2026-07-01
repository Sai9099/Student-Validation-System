using StudentValidationSystem.Models;

namespace StudentValidationSystem.Helpers;

public static class StudentSessionManager
{
    public static StudentSession? Current { get; set; }
    public static int StudentId => Current?.User.StudentId ?? 0;
    public static string StudentName => Current?.User.FullName ?? "";
}
