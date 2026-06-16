using StudentValidationSystem.Database;
using StudentValidationSystem.Models;

namespace StudentValidationSystem.Services;

public class AuthService
{
    private readonly DBHelper _db;
    private readonly StudentDatabase _studentDb = new();

    public AuthService(DBHelper db)
    {
        _db = db;
        _db.Initialize();
        _studentDb.Initialize();
    }

    public AuthResult RegisterStudent(
        string registerNumber,
        string fullName,
        string department,
        string semester,
        string section,
        string email,
        string mobile,
        string password,
        string confirmPassword)
    {
        var validation = ValidateRequired(
            (registerNumber, "Register Number"),
            (fullName, "Full Name"),
            (department, "Department"),
            (semester, "Semester"),
            (section, "Section"),
            (email, "Email"),
            (mobile, "Mobile Number"),
            (password, "Password"),
            (confirmPassword, "Confirm Password"));
        if (validation != null) return AuthResult.Fail(validation);
        if (password != confirmPassword) return AuthResult.Fail("Password and Confirm Password must match.");

        registerNumber = registerNumber.Trim();
        email = email.Trim();

        try
        {
            if (Exists("SELECT COUNT(*) FROM StudentProfile WHERE lower(RegisterNumber) = lower($value);", registerNumber)
                || Exists("SELECT COUNT(*) FROM Users WHERE Role = 'Student' AND lower(RegisterNumber) = lower($value);", registerNumber))
            {
                return AuthResult.Fail("This Register Number is already registered");
            }

            if (EmailExists(email)) return AuthResult.Fail("This Email is already registered");

            var studentId = _studentDb.RegisterStudent(registerNumber, fullName, department, semester, section, email, mobile, password);
            _db.Execute("""
                INSERT INTO Users
                    (Username, Password, Role, RegisterNumber, FacultyId, FullName, Department, Semester,
                     Section, Designation, Email, Mobile, CreatedDate, IsActive)
                VALUES
                    ($username, $password, 'Student', $registerNumber, '', $fullName, $department, $semester,
                     $section, '', $email, $mobile, datetime('now'), 1);
                """,
                ("$username", registerNumber),
                ("$password", DBHelper.HashPassword(password)),
                ("$registerNumber", registerNumber),
                ("$fullName", fullName.Trim()),
                ("$department", department.Trim()),
                ("$semester", semester.Trim()),
                ("$section", section.Trim()),
                ("$email", email),
                ("$mobile", mobile.Trim()));

            return AuthResult.Ok(new User
            {
                UserId = studentId,
                Username = registerNumber,
                Role = "Student",
                RegisterNumber = registerNumber,
                FullName = fullName.Trim(),
                Department = department.Trim(),
                Email = email
            }, 0, fullName.Trim(), department.Trim(), email);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail($"Registration failed: {ex.Message}");
        }
    }

    public AuthResult RegisterFaculty(
        string facultyId,
        string fullName,
        string department,
        string designation,
        string email,
        string mobile,
        string password,
        string confirmPassword)
    {
        var validation = ValidateRequired(
            (facultyId, "Faculty ID"),
            (fullName, "Full Name"),
            (department, "Department"),
            (designation, "Designation"),
            (email, "Email"),
            (mobile, "Mobile Number"),
            (password, "Password"),
            (confirmPassword, "Confirm Password"));
        if (validation != null) return AuthResult.Fail(validation);
        if (password != confirmPassword) return AuthResult.Fail("Password and Confirm Password must match.");

        facultyId = facultyId.Trim();
        email = email.Trim();

        try
        {
            if (Exists("SELECT COUNT(*) FROM Faculty WHERE lower(FacultyCode) = lower($value);", facultyId)
                || Exists("SELECT COUNT(*) FROM Users WHERE Role = 'Faculty' AND lower(FacultyId) = lower($value);", facultyId))
            {
                return AuthResult.Fail("This Faculty ID is already registered");
            }

            if (EmailExists(email)) return AuthResult.Fail("This Email is already registered");

            _db.Execute("""
                INSERT INTO Users
                    (Username, Password, Role, RegisterNumber, FacultyId, FullName, Department, Semester,
                     Section, Designation, Email, Mobile, CreatedDate, IsActive)
                VALUES
                    ($username, $password, 'Faculty', '', $facultyId, $fullName, $department, '',
                     '', $designation, $email, $mobile, datetime('now'), 1);
                """,
                ("$username", email),
                ("$password", DBHelper.HashPassword(password)),
                ("$facultyId", facultyId),
                ("$fullName", fullName.Trim()),
                ("$department", department.Trim()),
                ("$designation", designation.Trim()),
                ("$email", email),
                ("$mobile", mobile.Trim()));

            var userId = Convert.ToInt32(_db.Scalar("SELECT UserId FROM Users WHERE lower(Username) = lower($email);", ("$email", email)));
            var numericFacultyId = Convert.ToInt32(_db.Scalar("SELECT IFNULL(MAX(FacultyId), 0) + 1 FROM Faculty;"));
            _db.Execute("""
                INSERT INTO FacultyProfiles (UserId, FacultyName, Department)
                VALUES ($userId, $fullName, $department);

                INSERT INTO Faculty
                    (FacultyId, UserId, FacultyCode, Name, Department, Designation, Email, MobileNumber,
                     AssignedClasses, OfficeRoomNumber, OfficeRoom, Experience, Qualification, Mobile, ProfileImagePath, Status)
                VALUES
                    ($numericFacultyId, $userId, $facultyCode, $fullName, $department, $designation, $email, $mobile,
                     '', '', '', '', '', $mobile, '', 'Active');
                """,
                ("$userId", userId),
                ("$numericFacultyId", numericFacultyId),
                ("$facultyCode", facultyId),
                ("$fullName", fullName.Trim()),
                ("$department", department.Trim()),
                ("$designation", designation.Trim()),
                ("$email", email),
                ("$mobile", mobile.Trim()));

            return AuthResult.Ok(new User
            {
                UserId = userId,
                Username = email,
                Role = "Faculty",
                FacultyId = facultyId,
                FullName = fullName.Trim(),
                Department = department.Trim(),
                Email = email
            }, numericFacultyId, fullName.Trim(), department.Trim(), email);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail($"Registration failed: {ex.Message}");
        }
    }

    public AuthResult LoginStudent(string registerNumber, string password)
    {
        if (string.IsNullOrWhiteSpace(registerNumber)) return AuthResult.Fail("Register Number is required.");
        if (string.IsNullOrWhiteSpace(password)) return AuthResult.Fail("Password is required.");

        try
        {
            var user = _studentDb.Authenticate(registerNumber, password);
            if (user == null) return AuthResult.Fail("Invalid register number or password.");

            var table = _db.GetDataTable("""
                SELECT UserId, Username, Password, Role, RegisterNumber, FullName, Department, Email
                FROM Users
                WHERE Role = 'Student' AND lower(RegisterNumber) = lower($registerNumber)
                LIMIT 1;
                """, ("$registerNumber", registerNumber.Trim()));

            var sessionUser = table.Rows.Count == 0
                ? new User()
                : new User
                {
                    UserId = Convert.ToInt32(table.Rows[0]["UserId"]),
                    Username = table.Rows[0]["Username"].ToString() ?? "",
                    Password = table.Rows[0]["Password"].ToString() ?? "",
                    Role = table.Rows[0]["Role"].ToString() ?? "",
                    RegisterNumber = table.Rows[0]["RegisterNumber"].ToString() ?? "",
                    FullName = table.Rows[0]["FullName"].ToString() ?? "",
                    Department = table.Rows[0]["Department"].ToString() ?? "",
                    Email = table.Rows[0]["Email"].ToString() ?? ""
                };

            if (!string.Equals(sessionUser.Role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                sessionUser.UserId = user.StudentUserId;
                sessionUser.Username = user.RegisterNumber;
                sessionUser.Role = "Student";
                sessionUser.RegisterNumber = user.RegisterNumber;
                sessionUser.FullName = user.FullName;
                sessionUser.Email = user.Email;
            }

            return AuthResult.Ok(sessionUser, 0, user.FullName, sessionUser.Department, user.Email);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail($"Login failed: {ex.Message}");
        }
    }

    public AuthResult LoginFaculty(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) return AuthResult.Fail("Faculty email is required.");
        if (string.IsNullOrWhiteSpace(password)) return AuthResult.Fail("Password is required.");

        try
        {
            var table = _db.GetDataTable("""
                SELECT u.UserId, COALESCE(f.FacultyId, u.UserId) AS NumericFacultyId, u.Username, u.Password, u.Role,
                       COALESCE(NULLIF(u.FacultyId, ''), f.FacultyCode, '') AS FacultyCode,
                       COALESCE(NULLIF(u.FullName, ''), fp.FacultyName, f.Name, u.Username) AS FacultyName,
                       COALESCE(NULLIF(u.Department, ''), fp.Department, f.Department, 'Not assigned') AS Department,
                       COALESCE(NULLIF(u.Email, ''), f.Email, u.Username) AS Email,
                       COALESCE(NULLIF(u.Mobile, ''), f.MobileNumber, '') AS Mobile,
                       COALESCE(f.Designation, u.Designation, '') AS Designation,
                       COALESCE(f.ProfileImagePath, '') AS ProfileImagePath,
                       COALESCE(f.Qualification, '') AS Qualification,
                       COALESCE(f.Experience, '') AS Experience
                FROM Users u
                LEFT JOIN FacultyProfiles fp ON fp.UserId = u.UserId
                LEFT JOIN Faculty f ON f.UserId = u.UserId OR lower(f.Email) = lower(u.Email)
                WHERE lower(COALESCE(NULLIF(u.Email, ''), u.Username)) = lower($username)
                LIMIT 1;
                """, ("$username", username.Trim()));

            if (table.Rows.Count == 0) return AuthResult.Fail("Invalid faculty email or password.");

            var row = table.Rows[0];
            var role = row["Role"].ToString() ?? "";
            if (!string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Fail("This account is not a Faculty account.");
            }

            var storedHash = row["Password"].ToString() ?? "";
            if (!string.Equals(storedHash, DBHelper.HashPassword(password), StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Fail("Invalid faculty email or password.");
            }

            var user = new User
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Username = row["Username"].ToString() ?? "",
                Password = storedHash,
                Role = role,
                FacultyId = row["FacultyCode"].ToString() ?? "",
                FullName = row["FacultyName"].ToString() ?? "",
                Department = row["Department"].ToString() ?? "",
                Email = row["Email"].ToString() ?? ""
            };

            _db.Execute("UPDATE Users SET LastLogin = datetime('now') WHERE UserId = $userId;", ("$userId", user.UserId));
            return AuthResult.Ok(user, Convert.ToInt32(row["NumericFacultyId"]), row["FacultyName"].ToString() ?? user.Username, row["Department"].ToString() ?? "", row["Email"].ToString() ?? "");
        }
        catch (Exception ex)
        {
            return AuthResult.Fail($"Login failed: {ex.Message}");
        }
    }

    public AuthResult LoginAdmin(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail)) return AuthResult.Fail("Admin username or email is required.");
        if (string.IsNullOrWhiteSpace(password)) return AuthResult.Fail("Password is required.");

        try
        {
            var table = _db.GetDataTable("""
                SELECT UserId, Username, Password, Role, COALESCE(Email, '') AS Email, COALESCE(IsActive, 1) AS IsActive
                FROM Users
                WHERE lower(Username) = lower($login) OR lower(COALESCE(Email, '')) = lower($login)
                LIMIT 1;
                """, ("$login", usernameOrEmail.Trim()));

            if (table.Rows.Count == 0) return AuthResult.Fail("Invalid admin username/email or password.");

            var row = table.Rows[0];
            if (Convert.ToInt32(row["IsActive"]) == 0) return AuthResult.Fail("This admin account is inactive.");
            var role = row["Role"].ToString() ?? "";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Fail("This account is not an Admin account.");
            }

            var storedHash = row["Password"].ToString() ?? "";
            if (!string.Equals(storedHash, DBHelper.HashPassword(password), StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Fail("Invalid admin username/email or password.");
            }

            _db.Execute("UPDATE Users SET LastLogin = datetime('now') WHERE UserId = $userId;", ("$userId", Convert.ToInt32(row["UserId"])));
            var user = new User
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Username = row["Username"].ToString() ?? "",
                Password = storedHash,
                Role = role,
                FullName = row["Username"].ToString() ?? "",
                Email = row["Email"].ToString() ?? ""
            };

            return AuthResult.Ok(user, 0, user.Username, "Administration", user.Email);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail($"Login failed: {ex.Message}");
        }
    }

    private bool Exists(string sql, string value) =>
        Convert.ToInt32(_db.Scalar(sql, ("$value", value.Trim()))) > 0;

    private bool EmailExists(string email) =>
        Exists("SELECT COUNT(*) FROM Users WHERE lower(COALESCE(Email, '')) = lower($value) OR lower(Username) = lower($value);", email)
        || Exists("SELECT COUNT(*) FROM StudentProfile WHERE lower(Email) = lower($value);", email)
        || Exists("SELECT COUNT(*) FROM Faculty WHERE lower(Email) = lower($value);", email);

    private static string? ValidateRequired(params (string Value, string Label)[] fields)
    {
        foreach (var (value, label) in fields)
        {
            if (string.IsNullOrWhiteSpace(value)) return $"{label} is required.";
        }

        return null;
    }
}

public class AuthResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public User? User { get; init; }
    public int FacultyId { get; init; }
    public string FacultyName { get; init; } = "";
    public string Department { get; init; } = "";
    public string Email { get; init; } = "";

    public static AuthResult Ok(User user, int facultyId, string facultyName, string department, string email = "") => new()
    {
        Success = true,
        User = user,
        FacultyId = facultyId,
        FacultyName = facultyName,
        Department = department,
        Email = email
    };

    public static AuthResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
