using StudentValidationSystem.Models;

namespace StudentValidationSystem.Helpers;

public static class SessionManager
{
    public static User? CurrentUser { get; private set; }

    public static int UserId => CurrentUser?.UserId ?? 0;
    public static string Role => CurrentUser?.Role ?? "";
    public static string RegisterNumber => CurrentUser?.RegisterNumber ?? "";
    public static string FacultyId => CurrentUser?.FacultyId ?? "";
    public static string FullName => CurrentUser?.FullName ?? "";
    public static string Email => CurrentUser?.Email ?? "";

    public static void Set(User user) => CurrentUser = user;

    public static void Clear() => CurrentUser = null;
}
