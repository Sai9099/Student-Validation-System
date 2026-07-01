using StudentValidationSystem.Models;

namespace StudentValidationSystem.Helpers;

public static class FacultySessionManager
{
    public static FacultySession? Current { get; set; }
    public static int FacultyId => Current?.FacultyId ?? 0;
    public static string FacultyName => Current?.Name ?? "";
    public static string Department => Current?.Department ?? "";
    public static string Email => Current?.Email ?? "";
    public static string MobileNumber => Current?.MobileNumber ?? "";
    public static string ProfileImagePath => Current?.ProfileImagePath ?? "";

    public static void Clear() => Current = null;
}
