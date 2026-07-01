using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using StudentValidationSystem.Models;

namespace StudentValidationSystem.Database;

public class StudentDatabase
{
    public static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    public static readonly string DatabasePath = Path.Combine(DataDirectory, "student_module.db");
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        Pooling = false,
        DefaultTimeout = 10
    }.ToString();

    public void Initialize()
    {
        Directory.CreateDirectory(DataDirectory);
        using var connection = CreateConnection();
        connection.Open();
        Execute(connection, SchemaSql);
        Seed(connection);
        EnsureSubjectCatalog(connection);
        EnsureLearningEvidenceSchema(connection);
        EnsureAttendanceCatalog(connection);
        EnsureMarksCatalog(connection);
        EnsureNotesCatalog(connection);
        EnsureFacultySampleData(connection);
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    public StudentUser? Authenticate(string registerOrEmail, string password)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT su.StudentUserId, su.StudentId, sp.RegisterNumber, sp.Email, sp.FullName, su.PasswordHash
            FROM StudentUsers su
            JOIN StudentProfile sp ON sp.StudentId = su.StudentId
            WHERE su.IsActive = 1
              AND lower(sp.RegisterNumber) = lower($login);
            """;
        command.Parameters.AddWithValue("$login", registerOrEmail.Trim());
        StudentUser user;
        string storedHash;
        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) return null;

            storedHash = reader.GetString(5);
            user = new StudentUser
            {
                StudentUserId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                RegisterNumber = reader.GetString(2),
                Email = reader.GetString(3),
                FullName = reader.GetString(4)
            };
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedHash),
                Encoding.UTF8.GetBytes(HashPassword(password))))
        {
            return null;
        }

        Execute(connection, "UPDATE StudentUsers SET LastLogin = datetime('now') WHERE StudentUserId = $id;", null, ("$id", user.StudentUserId));
        return user;
    }

    public int RegisterStudent(
        string registerNumber,
        string fullName,
        string department,
        string semester,
        string section,
        string email,
        string mobile,
        string password)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        Execute(connection, """
            INSERT INTO StudentProfile
                (FullName, RegisterNumber, DateOfBirth, Gender, Email, MobileNumber, Address, ProfilePhotoPath)
            VALUES
                ($fullName, $registerNumber, '', '', $email, $mobile, '', '');
            """, tx,
            ("$fullName", fullName.Trim()),
            ("$registerNumber", registerNumber.Trim()),
            ("$email", email.Trim()),
            ("$mobile", mobile.Trim()));

        var studentId = Convert.ToInt32(Scalar(connection, "SELECT last_insert_rowid();", tx));
        var currentSemester = int.TryParse(semester.Trim(), out var parsedSemester) ? parsedSemester : 1;
        var year = Math.Max(1, (currentSemester + 1) / 2);

        Execute(connection, """
            INSERT INTO StudentUsers (StudentId, PasswordHash, IsActive)
            VALUES ($studentId, $passwordHash, 1);

            INSERT INTO AcademicDetails
                (StudentId, Department, Degree, Year, CurrentSemester, Section, Batch, RollNumber,
                 AdmissionNumber, MentorName, CGPA, Backlogs)
            VALUES
                ($studentId, $department, 'B.Tech', $year, $semester, $section, '', '', '', '', 0, 0);

            INSERT INTO GuardianDetails
                (StudentId, FatherName, MotherName, ParentMobileNumber, ParentEmail, EmergencyContact)
            VALUES
                ($studentId, '', '', '', '', '');
            """, tx,
            ("$studentId", studentId),
            ("$passwordHash", HashPassword(password)),
            ("$department", department.Trim()),
            ("$year", year),
            ("$semester", currentSemester),
            ("$section", section.Trim()));

        tx.Commit();
        return studentId;
    }

    public DashboardSummary GetDashboardSummary(int studentId)
    {
        var academic = GetAcademicDetails(studentId);
        return new DashboardSummary
        {
            CurrentSemester = academic.CurrentSemester,
            AttendancePercentage = Convert.ToDouble(Scalar("""
                SELECT IFNULL(ROUND(SUM(PresentCount) * 100.0 / NULLIF(SUM(TotalClassesConducted), 0), 2), 0)
                FROM Attendance WHERE StudentId = $studentId AND Semester = $semester;
                """, ("$studentId", studentId), ("$semester", academic.CurrentSemester))),
            CGPA = academic.CGPA,
            TotalSubjects = Convert.ToInt32(Scalar("""
                SELECT COUNT(*) FROM SemesterSubjects WHERE Semester = $semester;
                """, ("$semester", academic.CurrentSemester))),
            PendingEvidence = Convert.ToInt32(Scalar("""
                SELECT COUNT(*) FROM LearningEvidence WHERE StudentId = $studentId AND Status IN ('Pending', 'Submitted', 'Resubmitted');
                """, ("$studentId", studentId))),
            ApprovedEvidence = Convert.ToInt32(Scalar("""
                SELECT COUNT(*) FROM LearningEvidence WHERE StudentId = $studentId AND Status = 'Approved';
                """, ("$studentId", studentId))),
            UpcomingEvents = Convert.ToInt32(Scalar("""
                SELECT COUNT(*) FROM CampusEvents WHERE EventDate >= date('now');
                """)),
            LatestInternalMarks = Convert.ToDouble(Scalar("""
                SELECT IFNULL(ROUND(AVG(TotalMarks), 2), 0)
                FROM InternalMarks WHERE StudentId = $studentId AND Semester = $semester;
                """, ("$studentId", studentId), ("$semester", academic.CurrentSemester)))
        };
    }

    public StudentProfile GetStudentProfile(int studentId)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT StudentId, FullName, RegisterNumber, DateOfBirth, Gender, Email, MobileNumber, Address, ProfilePhotoPath
            FROM StudentProfile WHERE StudentId = $studentId;
            """;
        command.Parameters.AddWithValue("$studentId", studentId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Student profile was not found.");

        return new StudentProfile
        {
            StudentId = reader.GetInt32(0),
            FullName = reader.GetString(1),
            RegisterNumber = reader.GetString(2),
            DateOfBirth = reader.GetString(3),
            Gender = reader.GetString(4),
            Email = reader.GetString(5),
            MobileNumber = reader.GetString(6),
            Address = reader.GetString(7),
            ProfilePhotoPath = reader.GetString(8)
        };
    }

    public AcademicDetails GetAcademicDetails(int studentId)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT StudentId, Department, Degree, Year, CurrentSemester, Section, Batch, RollNumber,
                   AdmissionNumber, MentorName, CGPA, Backlogs
            FROM AcademicDetails WHERE StudentId = $studentId;
            """;
        command.Parameters.AddWithValue("$studentId", studentId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Academic details were not found.");

        return new AcademicDetails
        {
            StudentId = reader.GetInt32(0),
            Department = reader.GetString(1),
            Degree = reader.GetString(2),
            Year = reader.GetInt32(3),
            CurrentSemester = reader.GetInt32(4),
            Section = reader.GetString(5),
            Batch = reader.GetString(6),
            RollNumber = reader.GetString(7),
            AdmissionNumber = reader.GetString(8),
            MentorName = reader.GetString(9),
            CGPA = reader.GetDouble(10),
            Backlogs = reader.GetInt32(11)
        };
    }

    public GuardianDetails GetGuardianDetails(int studentId)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT StudentId, FatherName, MotherName, ParentMobileNumber, ParentEmail, EmergencyContact
            FROM GuardianDetails WHERE StudentId = $studentId;
            """;
        command.Parameters.AddWithValue("$studentId", studentId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return new GuardianDetails { StudentId = studentId };

        return new GuardianDetails
        {
            StudentId = reader.GetInt32(0),
            FatherName = reader.GetString(1),
            MotherName = reader.GetString(2),
            ParentMobileNumber = reader.GetString(3),
            ParentEmail = reader.GetString(4),
            EmergencyContact = reader.GetString(5)
        };
    }

    public void SaveProfile(StudentProfile profile, AcademicDetails academic, GuardianDetails guardian)
    {
        if (string.IsNullOrWhiteSpace(profile.FullName)) throw new ArgumentException("Full name is required.");
        if (string.IsNullOrWhiteSpace(profile.RegisterNumber)) throw new ArgumentException("Register number is required.");
        if (!profile.Email.Contains('@') || !profile.Email.Contains('.')) throw new ArgumentException("Enter a valid email address.");
        if (profile.MobileNumber.Length < 10) throw new ArgumentException("Enter a valid mobile number.");
        if (academic.Year is < 1 or > 6) throw new ArgumentException("Academic year must be between 1 and 6.");
        if (academic.CurrentSemester is < 1 or > 10) throw new ArgumentException("Semester must be between 1 and 10.");
        if (academic.CGPA is < 0 or > 10) throw new ArgumentException("CGPA must be between 0 and 10.");
        if (academic.Backlogs < 0) throw new ArgumentException("Backlogs cannot be negative.");

        using var connection = CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();
        Execute(connection, """
            UPDATE StudentProfile
            SET FullName=$fullName, RegisterNumber=$registerNumber, DateOfBirth=$dob, Gender=$gender,
                Email=$email, MobileNumber=$mobile, Address=$address, ProfilePhotoPath=$photo
            WHERE StudentId=$studentId;
            """, tx,
            ("$fullName", profile.FullName), ("$registerNumber", profile.RegisterNumber), ("$dob", profile.DateOfBirth),
            ("$gender", profile.Gender), ("$email", profile.Email), ("$mobile", profile.MobileNumber),
            ("$address", profile.Address), ("$photo", profile.ProfilePhotoPath), ("$studentId", profile.StudentId));

        Execute(connection, """
            UPDATE AcademicDetails
            SET Department=$department, Degree=$degree, Year=$year, CurrentSemester=$semester, Section=$section,
                Batch=$batch, RollNumber=$roll, AdmissionNumber=$admission, MentorName=$mentor,
                CGPA=$cgpa, Backlogs=$backlogs
            WHERE StudentId=$studentId;
            """, tx,
            ("$department", academic.Department), ("$degree", academic.Degree), ("$year", academic.Year),
            ("$semester", academic.CurrentSemester), ("$section", academic.Section), ("$batch", academic.Batch),
            ("$roll", academic.RollNumber), ("$admission", academic.AdmissionNumber), ("$mentor", academic.MentorName),
            ("$cgpa", academic.CGPA), ("$backlogs", academic.Backlogs), ("$studentId", academic.StudentId));

        Execute(connection, """
            INSERT INTO GuardianDetails (StudentId, FatherName, MotherName, ParentMobileNumber, ParentEmail, EmergencyContact)
            VALUES ($studentId, $father, $mother, $mobile, $email, $emergency)
            ON CONFLICT(StudentId) DO UPDATE SET
                FatherName=$father, MotherName=$mother, ParentMobileNumber=$mobile,
                ParentEmail=$email, EmergencyContact=$emergency;
            """, tx,
            ("$studentId", guardian.StudentId), ("$father", guardian.FatherName), ("$mother", guardian.MotherName),
            ("$mobile", guardian.ParentMobileNumber), ("$email", guardian.ParentEmail), ("$emergency", guardian.EmergencyContact));

        tx.Commit();
    }

    public DataTable GetDataTable(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        using var reader = command.ExecuteReader();
        return ReadLooseDataTable(reader);
    }

    public void AddActivity(int studentId, string type, string name, string description, string filePath)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Activity name is required.");
        Execute("""
            INSERT INTO ExtraCurricularActivities (StudentId, ActivityType, ActivityName, Description, DateFrom, CertificatePath)
            VALUES ($studentId, $type, $name, $description, date('now'), $filePath);
            """, ("$studentId", studentId), ("$type", type), ("$name", name.Trim()),
            ("$description", description.Trim()), ("$filePath", filePath.Trim()));
    }

    public void AddAchievement(int studentId, string type, string title, string description, string filePath)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Achievement title is required.");
        Execute("""
            INSERT INTO Achievements (StudentId, AchievementType, Title, Description, AchievementDate, FilePath)
            VALUES ($studentId, $type, $title, $description, date('now'), $filePath);
            """, ("$studentId", studentId), ("$type", type), ("$title", title.Trim()),
            ("$description", description.Trim()), ("$filePath", filePath.Trim()));
    }

    public void AddEvidence(int studentId, string title, string category, int semester, string subject, string description, string filePath)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Evidence title is required.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");
        Execute("""
            INSERT INTO LearningEvidence
                (StudentId, EvidenceTitle, Category, Semester, Subject, Description, UploadDate, FilePath, ValidationStatus, FacultyComments)
            VALUES
                ($studentId, $title, $category, $semester, $subject, $description, date('now'), $filePath, 'Pending', '');
            """, ("$studentId", studentId), ("$title", title.Trim()), ("$category", category), ("$semester", semester),
            ("$subject", subject), ("$description", description.Trim()), ("$filePath", filePath.Trim()));
    }

    public void AddAdvancedEvidence(
        int studentId,
        string title,
        string category,
        int semester,
        int subjectId,
        string academicYear,
        string description,
        string learningOutcome,
        string skillsGained,
        string filePath,
        string startDate,
        string endDate,
        string issuedOrganization,
        string certificateId,
        string verificationLink,
        string status,
        string visibility)
    {
        ValidateEvidence(title, semester, subjectId, filePath, status);
        var fileType = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
        if (!AllowedEvidenceFileTypes.Contains(fileType)) throw new ArgumentException("Allowed file types: PDF, DOCX, JPG, PNG, ZIP, PPT, PPTX.");

        Execute("""
            INSERT INTO LearningEvidence
                (StudentId, Title, EvidenceTitle, Category, Semester, SubjectId, Subject, AcademicYear, Description,
                 LearningOutcome, SkillsGained, FilePath, FileType, UploadDate, StartDate, EndDate, IssuedOrganization,
                 CertificateId, VerificationLink, Status, ValidationStatus, FacultyComments, Visibility, ResubmissionCount, LastUpdatedDate)
            SELECT
                $studentId, $title, $title, $category, $semester, $subjectId, s.SubjectName, $academicYear, $description,
                $learningOutcome, $skillsGained, $filePath, $fileType, date('now'), $startDate, $endDate, $issuedOrganization,
                $certificateId, $verificationLink, $status, $legacyStatus, '', $visibility, 0, date('now')
            FROM Subjects s WHERE s.SubjectId = $subjectId;
            """,
            ("$studentId", studentId),
            ("$title", title.Trim()),
            ("$category", category),
            ("$semester", semester),
            ("$subjectId", subjectId),
            ("$academicYear", academicYear.Trim()),
            ("$description", description.Trim()),
            ("$learningOutcome", learningOutcome.Trim()),
            ("$skillsGained", skillsGained.Trim()),
            ("$filePath", filePath.Trim()),
            ("$fileType", fileType),
            ("$startDate", startDate.Trim()),
            ("$endDate", endDate.Trim()),
            ("$issuedOrganization", issuedOrganization.Trim()),
            ("$certificateId", certificateId.Trim()),
            ("$verificationLink", verificationLink.Trim()),
            ("$status", status),
            ("$legacyStatus", ToLegacyEvidenceStatus(status)),
            ("$visibility", visibility));
    }

    public void UpdateAdvancedEvidence(
        int evidenceId,
        int studentId,
        string title,
        string category,
        int semester,
        int subjectId,
        string academicYear,
        string description,
        string learningOutcome,
        string skillsGained,
        string filePath,
        string startDate,
        string endDate,
        string issuedOrganization,
        string certificateId,
        string verificationLink,
        string status,
        string visibility,
        bool isResubmission)
    {
        ValidateEvidence(title, semester, subjectId, filePath, status);
        var fileType = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
        if (!AllowedEvidenceFileTypes.Contains(fileType)) throw new ArgumentException("Allowed file types: PDF, DOCX, JPG, PNG, ZIP, PPT, PPTX.");
        var nextStatus = isResubmission ? "Resubmitted" : status;

        Execute("""
            UPDATE LearningEvidence
            SET Title = $title,
                EvidenceTitle = $title,
                Category = $category,
                Semester = $semester,
                SubjectId = $subjectId,
                Subject = (SELECT SubjectName FROM Subjects WHERE SubjectId = $subjectId),
                AcademicYear = $academicYear,
                Description = $description,
                LearningOutcome = $learningOutcome,
                SkillsGained = $skillsGained,
                FilePath = $filePath,
                FileType = $fileType,
                StartDate = $startDate,
                EndDate = $endDate,
                IssuedOrganization = $issuedOrganization,
                CertificateId = $certificateId,
                VerificationLink = $verificationLink,
                Status = $status,
                ValidationStatus = $legacyStatus,
                Visibility = $visibility,
                ResubmissionCount = ResubmissionCount + $resubmitted,
                LastUpdatedDate = date('now')
            WHERE EvidenceId = $evidenceId AND StudentId = $studentId;
            """,
            ("$evidenceId", evidenceId),
            ("$studentId", studentId),
            ("$title", title.Trim()),
            ("$category", category),
            ("$semester", semester),
            ("$subjectId", subjectId),
            ("$academicYear", academicYear.Trim()),
            ("$description", description.Trim()),
            ("$learningOutcome", learningOutcome.Trim()),
            ("$skillsGained", skillsGained.Trim()),
            ("$filePath", filePath.Trim()),
            ("$fileType", fileType),
            ("$startDate", startDate.Trim()),
            ("$endDate", endDate.Trim()),
            ("$issuedOrganization", issuedOrganization.Trim()),
            ("$certificateId", certificateId.Trim()),
            ("$verificationLink", verificationLink.Trim()),
            ("$status", nextStatus),
            ("$legacyStatus", ToLegacyEvidenceStatus(nextStatus)),
            ("$visibility", visibility),
            ("$resubmitted", isResubmission ? 1 : 0));
    }

    public void AddDocument(int studentId, string documentType, string documentName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(documentName)) throw new ArgumentException("Document name is required.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");
        Execute("""
            INSERT INTO StudentDocuments (StudentId, DocumentType, DocumentName, FilePath, UploadDate)
            VALUES ($studentId, $type, $name, $filePath, date('now'));
            """, ("$studentId", studentId), ("$type", documentType), ("$name", documentName.Trim()), ("$filePath", filePath.Trim()));
    }

    public void RegisterForEvent(int studentId, int eventId)
    {
        Execute("""
            INSERT OR IGNORE INTO EventRegistrations (StudentId, EventId, RegistrationDate, Status)
            VALUES ($studentId, $eventId, date('now'), 'Interested');
            """, ("$studentId", studentId), ("$eventId", eventId));
    }

    public void MarkNotificationRead(int notificationId)
    {
        Execute("UPDATE Notifications SET IsRead = 1 WHERE NotificationId = $id;", ("$id", notificationId));
    }

    public void AddNote(int studentId, int subjectId, int semester, int unitNumber, string title, string description, string filePath)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Note title is required.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");
        if (semester is < 1 or > 8) throw new ArgumentException("Semester must be between 1 and 8.");
        if (unitNumber is < 1 or > 6) throw new ArgumentException("Unit/module number must be between 1 and 6.");

        Execute("""
            INSERT INTO Notes
                (StudentId, FacultyId, UploadedByRole, Title, SubjectId, Semester, UnitNumber, Description,
                 FilePath, UploadDate, ApprovalStatus, ApprovedBy, FacultyComments, LikesCount, IsBookmarked, ReportStatus)
            VALUES
                ($studentId, NULL, 'Student', $title, $subjectId, $semester, $unit, $description,
                 $filePath, date('now'), 'Pending', '', '', 0, 0, 'None');
            """,
            ("$studentId", studentId),
            ("$title", title.Trim()),
            ("$subjectId", subjectId),
            ("$semester", semester),
            ("$unit", unitNumber),
            ("$description", description.Trim()),
            ("$filePath", filePath.Trim()));
    }

    public void UpdateOwnNote(int noteId, int studentId, int subjectId, int semester, int unitNumber, string title, string description, string filePath)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Note title is required.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");

        Execute("""
            UPDATE Notes
            SET Title = $title,
                SubjectId = $subjectId,
                Semester = $semester,
                UnitNumber = $unit,
                Description = $description,
                FilePath = $filePath,
                UploadDate = date('now'),
                ApprovalStatus = 'Pending',
                ApprovedBy = '',
                FacultyComments = ''
            WHERE NoteId = $noteId AND StudentId = $studentId AND UploadedByRole = 'Student';
            """,
            ("$noteId", noteId),
            ("$studentId", studentId),
            ("$title", title.Trim()),
            ("$subjectId", subjectId),
            ("$semester", semester),
            ("$unit", unitNumber),
            ("$description", description.Trim()),
            ("$filePath", filePath.Trim()));
    }

    public void DeleteOwnNote(int noteId, int studentId)
    {
        Execute("DELETE FROM Notes WHERE NoteId = $noteId AND StudentId = $studentId AND UploadedByRole = 'Student';",
            ("$noteId", noteId), ("$studentId", studentId));
    }

    public void LikeNote(int noteId)
    {
        Execute("UPDATE Notes SET LikesCount = LikesCount + 1 WHERE NoteId = $noteId AND ApprovalStatus = 'Approved';", ("$noteId", noteId));
    }

    public void ToggleBookmarkNote(int noteId)
    {
        Execute("UPDATE Notes SET IsBookmarked = CASE WHEN IsBookmarked = 1 THEN 0 ELSE 1 END WHERE NoteId = $noteId AND ApprovalStatus = 'Approved';", ("$noteId", noteId));
    }

    public void ReportNote(int noteId)
    {
        Execute("UPDATE Notes SET ReportStatus = 'Reported' WHERE NoteId = $noteId AND ApprovalStatus = 'Approved';", ("$noteId", noteId));
    }

    public object Scalar(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteScalar() ?? 0;
    }

    public void Execute(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = CreateConnection();
        connection.Open();
        Execute(connection, sql, null, parameters);
    }

    public static string HashPassword(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? tx = null, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        AddParameters(command, parameters);
        command.ExecuteNonQuery();
    }

    private static object Scalar(SqliteConnection connection, string sql, SqliteTransaction? tx = null, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteScalar() ?? 0;
    }

    private static void AddParameters(SqliteCommand command, params (string Name, object? Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private static DataTable ReadLooseDataTable(SqliteDataReader reader)
    {
        var table = new DataTable();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < reader.FieldCount; index++)
        {
            var baseName = string.IsNullOrWhiteSpace(reader.GetName(index)) ? $"Column{index + 1}" : reader.GetName(index);
            var name = baseName;
            var suffix = 2;
            while (!names.Add(name))
            {
                name = $"{baseName}_{suffix++}";
            }
            table.Columns.Add(name, typeof(object));
        }

        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            table.Rows.Add(values);
        }

        return table;
    }

    private static void Seed(SqliteConnection connection)
    {
        using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM StudentUsers;";
        if (Convert.ToInt32(count.ExecuteScalar()) > 0) return;

        using var tx = connection.BeginTransaction();
        Execute(connection, SeedSql, tx,
            ("$passwordHash", HashPassword("student123")),
            ("$today", DateTime.Today.ToString("yyyy-MM-dd")));
        tx.Commit();
    }

    private static void EnsureSubjectCatalog(SqliteConnection connection)
    {
        var catalog = new[]
        {
            Subject(1, "23MAT101", "Calculus and Linear Algebra", "Mathematics Department", 4, "Theory", "Apply calculus and matrix methods to engineering problems."),
            Subject(1, "23PHY101", "Engineering Physics", "Physics Department", 3, "Theory", "Explain mechanics, optics, and semiconductor fundamentals."),
            Subject(1, "23CHE101", "Engineering Chemistry", "Chemistry Department", 3, "Theory", "Relate chemical principles to materials and environment."),
            Subject(1, "23CSE101", "Problem Solving Using C", "CSE Department", 4, "Theory/Lab", "Write structured programs using C."),
            Subject(1, "23EEE101", "Basic Electrical and Electronics Engineering", "EEE Department", 3, "Joint Course", "Understand circuits, machines, and electronic devices."),
            Subject(1, "23ENG101", "Technical English", "English Department", 2, "Joint Course", "Communicate technical ideas clearly."),
            Subject(1, "23CSE102P", "Engineering Exploration Project", "CSE Department", 1, "Project", "Build a small interdisciplinary engineering prototype."),
            Subject(1, "23ME101P", "Workshop Practice", "Mechanical Department", 1, "Lab", "Use basic workshop tools and fabrication methods."),
            Subject(1, "23NSS101", "Community Service", "Student Affairs", 0, "Audit", "Participate in service and civic responsibility activities."),

            Subject(2, "23MAT102", "Probability and Statistics", "Mathematics Department", 4, "Theory", "Apply probability and statistical methods to data."),
            Subject(2, "23CSE103", "Object Oriented Programming", "CSE Department", 4, "Theory/Lab", "Design programs using object-oriented concepts."),
            Subject(2, "23CSE104", "Digital Logic Design", "CSE Department", 3, "Theory", "Design combinational and sequential circuits."),
            Subject(2, "23ECE102", "Electronic Devices and Circuits", "ECE Department", 3, "Joint Course", "Analyze basic electronic devices and circuits."),
            Subject(2, "23HSS102", "Environmental Science", "Humanities Department", 2, "Joint Course", "Evaluate environmental issues and sustainability practices."),
            Subject(2, "23CSE105P", "Web Technology Lab", "CSE Department", 1, "Lab", "Create basic web applications."),
            Subject(2, "23CSE106P", "Design Thinking Project", "CSE Department", 1, "Project", "Solve user problems through design thinking."),
            Subject(2, "23ENG102", "Professional Communication", "English Department", 2, "Theory", "Prepare professional reports and presentations."),
            Subject(2, "23YOG102", "Yoga and Wellness", "Physical Education", 0, "Audit", "Practice wellness, fitness, and stress management."),

            Subject(3, "23MAT201", "Discrete Mathematics", "Mathematics Department", 4, "Theory", "Apply logic, graph theory, and combinatorics."),
            Subject(3, "23CSE201", "Data Structures", "CSE Department", 4, "Theory/Lab", "Implement linear and nonlinear data structures."),
            Subject(3, "23CSE202", "Computer Organization and Architecture", "CSE Department", 3, "Theory", "Explain processor, memory, and I/O organization."),
            Subject(3, "23CSE203", "Database Management Systems", "CSE Department", 4, "Theory/Lab", "Design databases and write SQL queries."),
            Subject(3, "23MGT201", "Engineering Economics", "Management Department", 2, "Joint Course", "Use economic analysis in engineering decisions."),
            Subject(3, "23ECE201", "Signals and Systems", "ECE Department", 3, "Joint Course", "Analyze signals in time and frequency domains."),
            Subject(3, "23CSE204P", "Data Structures Mini Project", "CSE Department", 1, "Project", "Build an application using data structures."),
            Subject(3, "23CSE205P", "Python Programming Lab", "CSE Department", 1, "Lab", "Develop scripts and small applications in Python."),
            Subject(3, "23NCC201", "NCC / Extension Activity", "Student Affairs", 0, "Audit", "Develop discipline, service, and leadership habits."),

            Subject(4, "23CSE206", "Design and Analysis of Algorithms", "CSE Department", 4, "Theory", "Analyze algorithms and choose efficient strategies."),
            Subject(4, "23CSE207", "Operating Systems", "CSE Department", 4, "Theory/Lab", "Understand processes, memory, files, and synchronization."),
            Subject(4, "23CSE208", "Software Engineering", "CSE Department", 3, "Theory", "Apply software lifecycle and design practices."),
            Subject(4, "23CSE209", "Computer Networks", "CSE Department", 4, "Theory/Lab", "Explain network protocols and layered architecture."),
            Subject(4, "23CSE210", "Java Programming", "CSE Department", 3, "Theory/Lab", "Develop object-oriented Java applications."),
            Subject(4, "23HSS204", "Social Engineering and Cyber Ethics", "Humanities Department", 2, "Joint Course", "Identify social engineering attacks and ethical safeguards."),
            Subject(4, "23MGT204", "Principles of Management", "Management Department", 2, "Joint Course", "Understand management functions and teamwork."),
            Subject(4, "23CSE211P", "Database Application Project", "CSE Department", 1, "Project", "Build a database-backed application."),
            Subject(4, "23VAC204", "Value Added Seminar", "CSE Department", 0, "Audit", "Attend seminars and prepare reflection evidence."),

            Subject(5, "23CSE301", "Theory of Computation", "CSE Department", 4, "Theory", "Analyze automata, grammars, and computability."),
            Subject(5, "23CSE302", "Compiler Design", "CSE Department", 4, "Theory/Lab", "Explain lexical analysis, parsing, and code generation."),
            Subject(5, "23CSE303", "Artificial Intelligence", "CSE Department", 3, "Theory", "Apply search, reasoning, and learning basics."),
            Subject(5, "23CSE304", "Cloud Computing", "CSE Department", 3, "Theory", "Use virtualization, cloud services, and deployment models."),
            Subject(5, "23CSE305", "Information Security", "CSE Department", 3, "Theory", "Apply cryptography and security principles."),
            Subject(5, "23ECE305", "Internet of Things", "ECE Department", 3, "Joint Course", "Build basic IoT systems using sensors and networks."),
            Subject(5, "23MGT305", "Entrepreneurship Development", "Management Department", 2, "Joint Course", "Create business models and startup plans."),
            Subject(5, "23CSE306P", "AI Mini Project", "CSE Department", 1, "Project", "Develop a small AI-enabled prototype."),
            Subject(5, "23VAC305", "Aptitude and Soft Skills", "Training Department", 0, "Audit", "Practice aptitude, communication, and interview skills."),

            Subject(6, "23CSE307", "Machine Learning", "CSE Department", 4, "Theory/Lab", "Train and evaluate machine learning models."),
            Subject(6, "23CSE308", "Data Mining and Warehousing", "CSE Department", 3, "Theory", "Extract patterns from large datasets."),
            Subject(6, "23CSE309", "Mobile Application Development", "CSE Department", 3, "Theory/Lab", "Build mobile applications and services."),
            Subject(6, "23CSE310", "Distributed Systems", "CSE Department", 3, "Theory", "Explain distributed coordination and fault tolerance."),
            Subject(6, "23CSE311", "Professional Elective I", "CSE Department", 3, "Theory", "Study an advanced computing elective."),
            Subject(6, "23HSS306", "Constitution of India", "Humanities Department", 2, "Joint Course", "Understand constitutional values and civic duties."),
            Subject(6, "23MGT306", "Project Management", "Management Department", 2, "Joint Course", "Plan, monitor, and deliver software projects."),
            Subject(6, "23CSE312P", "Full Stack Development Project", "CSE Department", 2, "Project", "Create a complete full-stack application."),
            Subject(6, "23VAC306", "Placement Readiness", "Training Department", 0, "Audit", "Prepare resume, portfolio, and placement evidence."),

            Subject(7, "23CSE401", "Professional Elective II", "CSE Department", 3, "Theory", "Study an advanced professional elective."),
            Subject(7, "23CSE402", "Professional Elective III", "CSE Department", 3, "Theory", "Study an advanced professional elective."),
            Subject(7, "23CSE403", "Open Elective", "Interdisciplinary Faculty", 3, "Joint Course", "Learn interdisciplinary concepts outside the major."),
            Subject(7, "23CSE404P", "Capstone Project Phase I", "CSE Department", 4, "Project", "Define, design, and prototype the capstone solution."),
            Subject(7, "23CSE405", "Industry Readiness Seminar", "Training Department", 0, "Audit", "Prepare industry portfolio and presentation evidence."),

            Subject(8, "23CSE499P", "Major Project / Internship", "CSE Department and Industry Mentor", 15, "Project/Internship", "Complete a major project or full-semester internship with final evidence.")
        };

        using var tx = connection.BeginTransaction();
        Execute(connection, "DELETE FROM SemesterSubjects;", tx);
        foreach (var subject in catalog)
        {
            Execute(connection, """
                INSERT INTO Subjects (SubjectCode, SubjectName, FacultyName, Credits, SubjectType, SyllabusPath, CourseOutcomes)
                VALUES ($code, $name, $faculty, $credits, $type, $syllabus, $outcomes)
                ON CONFLICT(SubjectCode) DO UPDATE SET
                    SubjectName = excluded.SubjectName,
                    FacultyName = excluded.FacultyName,
                    Credits = excluded.Credits,
                    SubjectType = excluded.SubjectType,
                    SyllabusPath = excluded.SyllabusPath,
                    CourseOutcomes = excluded.CourseOutcomes;
                """, tx,
                ("$code", subject.Code),
                ("$name", subject.Name),
                ("$faculty", subject.Faculty),
                ("$credits", subject.Credits),
                ("$type", subject.Type),
                ("$syllabus", subject.SyllabusPath),
                ("$outcomes", subject.Outcomes));

            Execute(connection, """
                INSERT INTO SemesterSubjects (Semester, SubjectId)
                SELECT $semester, SubjectId FROM Subjects WHERE SubjectCode = $code;
                """, tx, ("$semester", subject.Semester), ("$code", subject.Code));
        }
        tx.Commit();
    }

    private static SemesterSubjectSeed Subject(
        int semester,
        string code,
        string name,
        string faculty,
        int credits,
        string type,
        string outcomes) =>
        new(semester, code, name, FacultyNameFor(code, faculty), credits, type, $@"C:\Syllabus\{code}.pdf", outcomes);

    private static string FacultyNameFor(string subjectCode, string fallback)
    {
        var names = new[]
        {
            "Dr. Ananya Raman",
            "Prof. Vivek Narayanan",
            "Dr. Priya Menon",
            "Prof. Arjun Varma",
            "Dr. Kavitha Rao",
            "Prof. Rohan Iyer",
            "Dr. Meera Sharma",
            "Prof. Nandhini S",
            "Dr. Suresh Kumar",
            "Prof. Lakshmi Nair",
            "Dr. Harish Krishnan",
            "Prof. Sneha Reddy",
            "Dr. Karthik Subramanian",
            "Prof. Farah Ahmed",
            "Dr. Neha Kapoor",
            "Prof. Daniel Joseph"
        };

        if (!fallback.Contains("Department", StringComparison.OrdinalIgnoreCase) &&
            !fallback.Contains("Faculty", StringComparison.OrdinalIgnoreCase) &&
            !fallback.Contains("Training", StringComparison.OrdinalIgnoreCase) &&
            !fallback.Contains("Student Affairs", StringComparison.OrdinalIgnoreCase) &&
            !fallback.Contains("Physical Education", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        var index = Math.Abs(subjectCode.GetHashCode()) % names.Length;
        return names[index];
    }

    private sealed record SemesterSubjectSeed(
        int Semester,
        string Code,
        string Name,
        string Faculty,
        int Credits,
        string Type,
        string SyllabusPath,
        string Outcomes);

    private static void EnsureAttendanceCatalog(SqliteConnection connection)
    {
        var rows = new List<AttendanceSeed>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT sp.StudentId, ss.Semester, ss.SubjectId, s.Credits
                FROM StudentProfile sp
                CROSS JOIN SemesterSubjects ss
                JOIN Subjects s ON s.SubjectId = ss.SubjectId
                ORDER BY sp.StudentId, ss.Semester, s.SubjectName;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new AttendanceSeed(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)));
            }
        }

        using var tx = connection.BeginTransaction();
        Execute(connection, "DELETE FROM Attendance WHERE StudentId IN (SELECT StudentId FROM StudentProfile);", tx);

        foreach (var row in rows)
        {
            var total = row.Credits == 0 ? 12 + row.Semester : 32 + (row.Semester * 2) + row.Credits;
            var targetPercentage = 92 - ((row.Semester * 3 + row.SubjectId * 5) % 22);
            var present = Math.Clamp((int)Math.Round(total * targetPercentage / 100.0), 0, total);
            var absent = total - present;
            var percentage = total == 0 ? 0 : Math.Round(present * 100.0 / total, 2);

            Execute(connection, """
                INSERT INTO Attendance
                    (StudentId, SubjectId, Semester, PresentCount, AbsentCount, TotalClassesConducted, AttendancePercentage, MinimumRequired)
                VALUES
                    ($studentId, $subjectId, $semester, $present, $absent, $total, $percentage, 75);
                """, tx,
                ("$studentId", row.StudentId),
                ("$subjectId", row.SubjectId),
                ("$semester", row.Semester),
                ("$present", present),
                ("$absent", absent),
                ("$total", total),
                ("$percentage", percentage));
        }

        tx.Commit();
    }

    private sealed record AttendanceSeed(int StudentId, int Semester, int SubjectId, int Credits);

    private static void EnsureMarksCatalog(SqliteConnection connection)
    {
        var rows = new List<MarksSeed>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT sp.StudentId, ss.Semester, ss.SubjectId, s.Credits, s.SubjectType
                FROM StudentProfile sp
                CROSS JOIN SemesterSubjects ss
                JOIN Subjects s ON s.SubjectId = ss.SubjectId
                ORDER BY sp.StudentId, ss.Semester, s.SubjectName;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new MarksSeed(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.GetString(4)));
            }
        }

        using var tx = connection.BeginTransaction();
        Execute(connection, "DELETE FROM InternalMarks WHERE StudentId IN (SELECT StudentId FROM StudentProfile);", tx);
        Execute(connection, "DELETE FROM Marks WHERE StudentId IN (SELECT StudentId FROM StudentProfile);", tx);

        var generatedMarks = new List<GeneratedMark>();
        foreach (var row in rows)
        {
            var isProjectOrInternship = row.SubjectType.Contains("Project", StringComparison.OrdinalIgnoreCase) ||
                                        row.SubjectType.Contains("Internship", StringComparison.OrdinalIgnoreCase);
            var isAudit = row.Credits == 0;
            var assignment = isAudit ? 0 : 7 + ((row.Semester + row.SubjectId) % 4);
            var quiz = isAudit ? 0 : 6 + ((row.SubjectId * 2 + row.Semester) % 5);
            var mid = isAudit ? 0 : 20 + ((row.SubjectId + row.Semester * 3) % 9);
            var lab = isAudit ? 0 : (row.SubjectType.Contains("Lab", StringComparison.OrdinalIgnoreCase) || isProjectOrInternship ? 16 + (row.SubjectId % 5) : 0);
            var external = isAudit ? 0 : 34 + ((row.SubjectId * 3 + row.Semester) % 15);
            var total = isAudit ? 0 : Math.Min(100, assignment + quiz + mid + lab + external);
            var grade = isAudit ? "S" : GradeFor(total);
            var gpa = isAudit ? 0 : GpaFor(total);

            Execute(connection, """
                INSERT INTO InternalMarks
                    (StudentId, SubjectId, Semester, AssignmentMarks, QuizMarks, MidExamMarks, LabMarks, ExternalMarks, TotalMarks, Grade, GPA)
                VALUES
                    ($studentId, $subjectId, $semester, $assignment, $quiz, $mid, $lab, $external, $total, $grade, $gpa);
                """, tx,
                ("$studentId", row.StudentId),
                ("$subjectId", row.SubjectId),
                ("$semester", row.Semester),
                ("$assignment", assignment),
                ("$quiz", quiz),
                ("$mid", mid),
                ("$lab", lab),
                ("$external", external),
                ("$total", total),
                ("$grade", grade),
                ("$gpa", gpa));
            generatedMarks.Add(new GeneratedMark(row.StudentId, row.Semester, row.SubjectId, row.Credits, total));
        }

        var studentIds = rows.Select(row => row.StudentId).Distinct();
        foreach (var studentId in studentIds)
        {
            var cumulativePoints = 0.0;
            var cumulativeCredits = 0;
            foreach (var semester in rows.Where(row => row.StudentId == studentId).Select(row => row.Semester).Distinct().Order())
            {
                var semesterRows = generatedMarks.Where(row => row.StudentId == studentId && row.Semester == semester && row.Credits > 0).ToList();
                var semesterPoints = 0.0;
                var semesterCredits = 0;

                foreach (var row in semesterRows)
                {
                    semesterPoints += GpaFor(row.TotalMarks) * row.Credits;
                    semesterCredits += row.Credits;
                }

                cumulativePoints += semesterPoints;
                cumulativeCredits += semesterCredits;
                var gpa = semesterCredits == 0 ? 0 : Math.Round(semesterPoints / semesterCredits, 2);
                var cgpa = cumulativeCredits == 0 ? 0 : Math.Round(cumulativePoints / cumulativeCredits, 2);

                Execute(connection, """
                    INSERT INTO Marks (StudentId, Semester, GPA, CGPA, ResultStatus, PublishedDate)
                    VALUES ($studentId, $semester, $gpa, $cgpa, 'Published', date('now'));
                    """, tx,
                    ("$studentId", studentId),
                    ("$semester", semester),
                    ("$gpa", gpa),
                    ("$cgpa", cgpa));
            }
        }

        tx.Commit();
    }

    private static string GradeFor(double total) => total switch
    {
        >= 90 => "O",
        >= 80 => "A+",
        >= 70 => "A",
        >= 60 => "B+",
        >= 50 => "B",
        _ => "RA"
    };

    private static double GpaFor(double total) => total switch
    {
        >= 90 => 10,
        >= 80 => 9,
        >= 70 => 8,
        >= 60 => 7,
        >= 50 => 6,
        _ => 0
    };

    private sealed record MarksSeed(int StudentId, int Semester, int SubjectId, int Credits, string SubjectType);
    private sealed record GeneratedMark(int StudentId, int Semester, int SubjectId, int Credits, double TotalMarks);

    private static void EnsureFacultySampleData(SqliteConnection connection)
    {
        // Check if the full faculty sample data already exists. SeedSql creates the
        // FAC001 faculty row, so checking only that row would skip the section roster.
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT COUNT(*)
                FROM StudentSectionMapping ssm
                JOIN FacultySections fs ON fs.SectionId = ssm.SectionId
                WHERE fs.FacultyId = 1;
                """;
            var count = Convert.ToInt32(command.ExecuteScalar() ?? 0);
            if (count >= 275) return;
        }

        using var tx = connection.BeginTransaction();

        // Ensure sections exist
        var sections = new[]
        {
            (1, "Computer Science and Engineering", 2, 4, "CSE-SE-A", 55),
            (2, "Computer Science and Engineering", 2, 4, "CSE-SE-B", 55),
            (3, "Computer Science and Engineering", 2, 4, "CSE-SE-C", 55),
            (4, "Computer Science and Engineering", 2, 4, "CSE-SE-D", 55),
            (5, "Computer Science and Engineering", 2, 4, "CSE-SE-E", 55),
        };

        foreach (var (sectionId, dept, year, semester, name, total) in sections)
        {
            Execute(connection, """
                INSERT OR IGNORE INTO Sections (SectionId, Department, Year, Semester, SectionName, TotalStudents)
                VALUES ($sectionId, $dept, $year, $semester, $name, $total);
                """, tx,
                ("$sectionId", sectionId),
                ("$dept", dept),
                ("$year", year),
                ("$semester", semester),
                ("$name", name),
                ("$total", total));
        }

        // Generate students for each section
        int studentIdCounter = 2; // Starting from 2, as 1 is already taken by sample student
        foreach (var (sectionId, _, _, _, sectionName, _) in sections)
        {
            for (int i = 0; i < 55; i++)
            {
                var studentId = studentIdCounter++;
                var regNum = $"RA23110330{studentId:05d}";
                var studentName = $"Student {studentId}";
                var email = $"student{studentId}@university.edu";
                var mobile = $"98765{(studentId % 100000):05d}";

                // Insert into StudentProfile
                Execute(connection, """
                    INSERT OR IGNORE INTO StudentProfile
                        (StudentId, FullName, RegisterNumber, DateOfBirth, Gender, Email, MobileNumber, Address, ProfilePhotoPath)
                    VALUES
                        ($studentId, $name, $regNum, '2004-01-01', 'Male', $email, $mobile, 'Chennai', '');
                    """, tx,
                    ("$studentId", studentId),
                    ("$name", studentName),
                    ("$regNum", regNum),
                    ("$email", email),
                    ("$mobile", mobile));

                // Insert into AcademicDetails
                Execute(connection, """
                    INSERT OR IGNORE INTO AcademicDetails
                        (StudentId, Department, Degree, Year, CurrentSemester, Section, Batch, RollNumber,
                         AdmissionNumber, MentorName, CGPA, Backlogs)
                    VALUES
                        ($studentId, 'Computer Science and Engineering', 'B.Tech', 2, 4, $sectionName, '2023-2027',
                         $regNum, $regNum, 'Dr. Meera Sharma', 7.5, 0);
                    """, tx,
                    ("$studentId", studentId),
                    ("$sectionName", sectionName),
                    ("$regNum", regNum));

                // Insert into StudentSectionMapping
                Execute(connection, """
                    INSERT OR IGNORE INTO StudentSectionMapping (StudentId, SectionId)
                    VALUES ($studentId, $sectionId);
                    """, tx,
                    ("$studentId", studentId),
                    ("$sectionId", sectionId));
            }
        }

        // Create faculty sections mapping
        var subjects = new[] { 1, 2 };  // Subjects 1 (DAA) and 2 (DBMS) handled by Dr. Meera Sharma
        foreach (var (sectionId, _, _, _, _, _) in sections)
        {
            foreach (var subjectId in subjects)
            {
                Execute(connection, """
                    INSERT OR IGNORE INTO FacultySections (FacultyId, SectionId, SubjectId)
                    VALUES (1, $sectionId, $subjectId);
                    """, tx,
                    ("$sectionId", sectionId),
                    ("$subjectId", subjectId));
            }
        }

        // Generate sample attendance records
        var students = new List<int>();
        for (int i = 2; i < studentIdCounter; i++)
        {
            students.Add(i);
        }

        var attendanceDates = new[] { "2026-04-15", "2026-04-16", "2026-04-17" };
        foreach (var date in attendanceDates)
        {
            foreach (var sectionId in new[] { 1, 2, 3, 4, 5 })
            {
                var sectionStudents = students.Skip((sectionId - 1) * 55).Take(55).ToList();
                foreach (var studentId in sectionStudents.Take(30))  // Only mark first 30 students
                {
                    var status = ((studentId + sectionId) % 10) < 8 ? "Present" : "Absent";
                    Execute(connection, """
                        INSERT OR IGNORE INTO FacultyAttendanceDaily
                            (FacultyId, StudentId, SubjectId, SectionId, AttendanceDate, Status)
                        VALUES (1, $studentId, 1, $sectionId, $date, $status);
                        """, tx,
                        ("$studentId", studentId),
                        ("$sectionId", sectionId),
                        ("$date", date),
                        ("$status", status));
                }
            }
        }

        // Generate sample marks records
        foreach (var sectionId in new[] { 1, 2, 3, 4, 5 })
        {
            var sectionStudents = students.Skip((sectionId - 1) * 55).Take(55).ToList();
            foreach (var studentId in sectionStudents.Take(30))
            {
                var marks = 40 + ((studentId + sectionId * 7) % 60);
                var grade = marks >= 90 ? "O" : marks >= 80 ? "A+" : marks >= 70 ? "A" : marks >= 60 ? "B+" : "B";
                Execute(connection, """
                    INSERT OR IGNORE INTO FacultyMarksRecords
                        (FacultyId, StudentId, SubjectId, SectionId, ExamType, MaxMarks, MarksObtained, Grade, Semester)
                    VALUES (1, $studentId, 1, $sectionId, 'Internal', 50, $marks, $grade, 4);
                    """, tx,
                    ("$studentId", studentId),
                    ("$sectionId", sectionId),
                    ("$marks", marks),
                    ("$grade", grade));
            }
        }

        tx.Commit();
    }

    private static readonly HashSet<string> AllowedEvidenceFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PDF", "DOCX", "JPG", "JPEG", "PNG", "ZIP", "PPT", "PPTX"
    };

    private static void ValidateEvidence(string title, int semester, int subjectId, string filePath, string status)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Evidence title is required.");
        if (semester is < 1 or > 8) throw new ArgumentException("Semester must be between 1 and 8.");
        if (subjectId <= 0) throw new ArgumentException("Select a subject.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");
        if (!new[] { "Draft", "Submitted", "Pending", "Approved", "Rejected", "Resubmitted" }.Contains(status))
        {
            throw new ArgumentException("Select a valid evidence status.");
        }
    }

    private static string ToLegacyEvidenceStatus(string status) => status switch
    {
        "Approved" => "Approved",
        "Rejected" => "Rejected",
        _ => "Pending"
    };

    private static void EnsureLearningEvidenceSchema(SqliteConnection connection)
    {
        AddColumnIfMissing(connection, "LearningEvidence", "Title", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "SubjectId", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(connection, "LearningEvidence", "AcademicYear", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "LearningOutcome", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "SkillsGained", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "FileType", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "StartDate", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "EndDate", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "IssuedOrganization", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "CertificateId", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "VerificationLink", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(connection, "LearningEvidence", "Status", "TEXT NOT NULL DEFAULT 'Pending'");
        AddColumnIfMissing(connection, "LearningEvidence", "Visibility", "TEXT NOT NULL DEFAULT 'Visible to faculty'");
        AddColumnIfMissing(connection, "LearningEvidence", "ResubmissionCount", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(connection, "LearningEvidence", "LastUpdatedDate", "TEXT NOT NULL DEFAULT ''");

        Execute(connection, """
            UPDATE LearningEvidence
            SET Title = CASE WHEN Title = '' THEN EvidenceTitle ELSE Title END,
                Status = CASE
                    WHEN ValidationStatus IN ('Approved', 'Rejected') THEN ValidationStatus
                    WHEN Status = '' THEN ValidationStatus
                    WHEN Status IS NULL THEN ValidationStatus
                    ELSE Status
                END,
                SubjectId = COALESCE(NULLIF(SubjectId, 0), (
                    SELECT s.SubjectId FROM Subjects s
                    WHERE lower(s.SubjectName) = lower(LearningEvidence.Subject)
                       OR lower(s.SubjectCode) = lower(LearningEvidence.Subject)
                    LIMIT 1
                ), (
                    SELECT ss.SubjectId FROM SemesterSubjects ss
                    WHERE ss.Semester = LearningEvidence.Semester
                    LIMIT 1
                ), 1),
                AcademicYear = CASE WHEN AcademicYear = '' THEN '2023-2027' ELSE AcademicYear END,
                LearningOutcome = CASE WHEN LearningOutcome = '' THEN 'Evidence mapped to course outcomes and practical learning.' ELSE LearningOutcome END,
                SkillsGained = CASE WHEN SkillsGained = '' THEN 'Technical skills, Problem-solving skills' ELSE SkillsGained END,
                FileType = CASE
                    WHEN FileType <> '' THEN FileType
                    WHEN lower(FilePath) LIKE '%.pdf' THEN 'PDF'
                    WHEN lower(FilePath) LIKE '%.docx' THEN 'DOCX'
                    WHEN lower(FilePath) LIKE '%.jpg' THEN 'JPG'
                    WHEN lower(FilePath) LIKE '%.jpeg' THEN 'JPG'
                    WHEN lower(FilePath) LIKE '%.png' THEN 'PNG'
                    WHEN lower(FilePath) LIKE '%.zip' THEN 'ZIP'
                    WHEN lower(FilePath) LIKE '%.ppt' THEN 'PPT'
                    WHEN lower(FilePath) LIKE '%.pptx' THEN 'PPTX'
                    ELSE 'PDF'
                END,
                StartDate = CASE WHEN StartDate = '' THEN UploadDate ELSE StartDate END,
                EndDate = CASE WHEN EndDate = '' THEN UploadDate ELSE EndDate END,
                IssuedOrganization = CASE WHEN IssuedOrganization = '' THEN 'College / External Organization' ELSE IssuedOrganization END,
                Visibility = CASE WHEN Visibility = '' THEN 'Visible to faculty' ELSE Visibility END,
                LastUpdatedDate = CASE WHEN LastUpdatedDate = '' THEN UploadDate ELSE LastUpdatedDate END;
            """);
    }

    private static void AddColumnIfMissing(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        var exists = false;
        using (var check = connection.CreateCommand())
        {
            check.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists) return;
        Execute(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};");
    }

    private static void EnsureNotesCatalog(SqliteConnection connection)
    {
        using var tx = connection.BeginTransaction();
        Execute(connection, """
            INSERT OR IGNORE INTO StudentProfile
                (StudentId, FullName, RegisterNumber, DateOfBirth, Gender, Email, MobileNumber, Address, ProfilePhotoPath)
            VALUES
                (2, 'Anika Sharma', 'RA2311033010112', '2005-11-02', 'Female', 'anika.sharma@student.edu',
                 '9876543301', 'Bengaluru, Karnataka', '');
            """, tx);

        Execute(connection, """
            INSERT INTO AcademicDetails
                (StudentId, Department, Degree, Year, CurrentSemester, Section, Batch, RollNumber, AdmissionNumber, MentorName, CGPA, Backlogs)
            VALUES
                (2, 'Computer Science and Engineering', 'B.Tech', 2, 4, 'CSE-SE-A', '2023-2027',
                 '602241', 'ADM20230112', 'Dr. Priya Menon', 8.61, 0)
            ON CONFLICT(StudentId) DO NOTHING;
            """, tx);

        Execute(connection, """
            INSERT INTO GuardianDetails
                (StudentId, FatherName, MotherName, ParentMobileNumber, ParentEmail, EmergencyContact)
            VALUES
                (2, 'Mr. Rajesh Sharma', 'Mrs. Kavya Sharma', '9876543302', 'anika.parent@example.com', '9876543303')
            ON CONFLICT(StudentId) DO NOTHING;
            """, tx);

        Execute(connection, """
            INSERT OR IGNORE INTO Notes
                (NoteId, StudentId, FacultyId, UploadedByRole, Title, SubjectId, Semester, UnitNumber, Description,
                 FilePath, UploadDate, ApprovalStatus, ApprovedBy, FacultyComments, LikesCount, IsBookmarked, ReportStatus)
            VALUES
                (1, 1, NULL, 'Student', 'DAA Unit 1 Handwritten Notes',
                 (SELECT SubjectId FROM Subjects WHERE SubjectCode = '23CSE206'), 4, 1,
                 'Handwritten notes covering asymptotic notation and recurrence basics.',
                 'C:\Notes\DAA_Unit1_Sai.pdf', date('now', '-12 days'), 'Approved', 'Dr. Meera Sharma', 'Clear and readable.', 8, 1, 'None'),
                (2, 1, NULL, 'Student', 'Operating Systems Scheduling Notes',
                 (SELECT SubjectId FROM Subjects WHERE SubjectCode = '23CSE207'), 4, 2,
                 'Scanned notes for CPU scheduling algorithms and examples.',
                 'C:\Notes\OS_Scheduling_Sai.pdf', date('now', '-4 days'), 'Pending', '', '', 2, 0, 'None'),
                (3, 2, NULL, 'Student', 'Computer Networks Unit 3 Diagrams',
                 (SELECT SubjectId FROM Subjects WHERE SubjectCode = '23CSE209'), 4, 3,
                 'Layered architecture and routing diagrams from class notes.',
                 'C:\Notes\CN_Unit3_Anika.pdf', date('now', '-8 days'), 'Approved', 'Prof. Rohan Iyer', 'Good diagrams.', 11, 0, 'None'),
                (4, 2, NULL, 'Student', 'Java Collections Quick Notes',
                 (SELECT SubjectId FROM Subjects WHERE SubjectCode = '23CSE210'), 4, 4,
                 'Summary of Java collections framework with examples.',
                 'C:\Notes\Java_Collections_Anika.pdf', date('now', '-6 days'), 'Rejected', 'Prof. Nandhini S', 'Please upload the full page scans.', 1, 0, 'None'),
                (5, NULL, 101, 'Faculty', 'Software Engineering UML Notes',
                 (SELECT SubjectId FROM Subjects WHERE SubjectCode = '23CSE208'), 4, 2,
                 'Faculty notes on UML diagrams and software design documentation.',
                 'C:\FacultyNotes\SE_UML_Faculty.pdf', date('now', '-14 days'), 'Approved', 'Admin', 'Faculty uploaded material.', 21, 1, 'None'),
                (6, NULL, 102, 'Faculty', 'DBMS Revision Notes',
                 (SELECT SubjectId FROM Subjects WHERE SubjectCode = '23CSE203'), 3, 5,
                 'Faculty revision notes for normalization and transactions.',
                 'C:\FacultyNotes\DBMS_Revision_Faculty.pdf', date('now', '-22 days'), 'Approved', 'Admin', 'Faculty uploaded material.', 18, 0, 'None');
            """, tx);
        tx.Commit();
    }

    private const string SchemaSql = """
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS StudentUsers (
            StudentUserId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            LastLogin TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS StudentProfile (
            StudentId INTEGER PRIMARY KEY AUTOINCREMENT,
            FullName TEXT NOT NULL,
            RegisterNumber TEXT NOT NULL UNIQUE,
            DateOfBirth TEXT NOT NULL,
            Gender TEXT NOT NULL,
            Email TEXT NOT NULL UNIQUE,
            MobileNumber TEXT NOT NULL,
            Address TEXT NOT NULL,
            ProfilePhotoPath TEXT NOT NULL DEFAULT ''
        );

        CREATE TABLE IF NOT EXISTS AcademicDetails (
            AcademicId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL UNIQUE,
            Department TEXT NOT NULL,
            Degree TEXT NOT NULL,
            Year INTEGER NOT NULL,
            CurrentSemester INTEGER NOT NULL,
            Section TEXT NOT NULL,
            Batch TEXT NOT NULL,
            RollNumber TEXT NOT NULL,
            AdmissionNumber TEXT NOT NULL,
            MentorName TEXT NOT NULL,
            CGPA REAL NOT NULL,
            Backlogs INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS GuardianDetails (
            GuardianId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL UNIQUE,
            FatherName TEXT NOT NULL,
            MotherName TEXT NOT NULL,
            ParentMobileNumber TEXT NOT NULL,
            ParentEmail TEXT NOT NULL,
            EmergencyContact TEXT NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS ExtraCurricularActivities (
            ActivityId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            ActivityType TEXT NOT NULL,
            ActivityName TEXT NOT NULL,
            Description TEXT NOT NULL,
            DateFrom TEXT NOT NULL,
            DateTo TEXT NULL,
            CertificatePath TEXT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Achievements (
            AchievementId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            AchievementType TEXT NOT NULL,
            Title TEXT NOT NULL,
            Description TEXT NOT NULL,
            AchievementDate TEXT NOT NULL,
            Organization TEXT NULL,
            FilePath TEXT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Subjects (
            SubjectId INTEGER PRIMARY KEY AUTOINCREMENT,
            SubjectCode TEXT NOT NULL UNIQUE,
            SubjectName TEXT NOT NULL,
            FacultyName TEXT NOT NULL,
            Credits INTEGER NOT NULL,
            SubjectType TEXT NOT NULL,
            SyllabusPath TEXT NOT NULL,
            CourseOutcomes TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS SemesterSubjects (
            SemesterSubjectId INTEGER PRIMARY KEY AUTOINCREMENT,
            Semester INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Attendance (
            AttendanceId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL,
            Semester INTEGER NOT NULL,
            PresentCount INTEGER NOT NULL,
            AbsentCount INTEGER NOT NULL,
            TotalClassesConducted INTEGER NOT NULL,
            AttendancePercentage REAL NOT NULL,
            MinimumRequired REAL NOT NULL DEFAULT 75,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Marks (
            MarkId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            Semester INTEGER NOT NULL,
            GPA REAL NOT NULL,
            CGPA REAL NOT NULL,
            ResultStatus TEXT NOT NULL,
            PublishedDate TEXT NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS InternalMarks (
            InternalMarkId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL,
            Semester INTEGER NOT NULL,
            AssignmentMarks REAL NOT NULL,
            QuizMarks REAL NOT NULL,
            MidExamMarks REAL NOT NULL,
            LabMarks REAL NOT NULL,
            ExternalMarks REAL NOT NULL,
            TotalMarks REAL NOT NULL,
            Grade TEXT NOT NULL,
            GPA REAL NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS LearningEvidence (
            EvidenceId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            EvidenceTitle TEXT NOT NULL,
            Title TEXT NOT NULL DEFAULT '',
            Category TEXT NOT NULL,
            Semester INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL DEFAULT 0,
            Subject TEXT NOT NULL,
            AcademicYear TEXT NOT NULL DEFAULT '',
            Description TEXT NOT NULL,
            LearningOutcome TEXT NOT NULL DEFAULT '',
            SkillsGained TEXT NOT NULL DEFAULT '',
            UploadDate TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileType TEXT NOT NULL DEFAULT '',
            StartDate TEXT NOT NULL DEFAULT '',
            EndDate TEXT NOT NULL DEFAULT '',
            IssuedOrganization TEXT NOT NULL DEFAULT '',
            CertificateId TEXT NOT NULL DEFAULT '',
            VerificationLink TEXT NOT NULL DEFAULT '',
            Status TEXT NOT NULL DEFAULT 'Pending',
            ValidationStatus TEXT NOT NULL DEFAULT 'Pending',
            FacultyComments TEXT NOT NULL DEFAULT '',
            Visibility TEXT NOT NULL DEFAULT 'Visible to faculty',
            ResubmissionCount INTEGER NOT NULL DEFAULT 0,
            LastUpdatedDate TEXT NOT NULL DEFAULT '',
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS CampusEvents (
            EventId INTEGER PRIMARY KEY AUTOINCREMENT,
            EventTitle TEXT NOT NULL,
            EventDate TEXT NOT NULL,
            Venue TEXT NOT NULL,
            ConductedBy TEXT NOT NULL,
            EventDescription TEXT NOT NULL,
            RegistrationDeadline TEXT NOT NULL,
            EventType TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS EventRegistrations (
            RegistrationId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            EventId INTEGER NOT NULL,
            RegistrationDate TEXT NOT NULL,
            Status TEXT NOT NULL,
            UNIQUE(StudentId, EventId),
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(EventId) REFERENCES CampusEvents(EventId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Notifications (
            NotificationId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            NotificationType TEXT NOT NULL,
            Title TEXT NOT NULL,
            Message TEXT NOT NULL,
            IsRead INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS StudentDocuments (
            DocumentId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            DocumentType TEXT NOT NULL,
            DocumentName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            UploadDate TEXT NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Notes (
            NoteId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NULL,
            FacultyId INTEGER NULL,
            UploadedByRole TEXT NOT NULL,
            Title TEXT NOT NULL,
            SubjectId INTEGER NOT NULL,
            Semester INTEGER NOT NULL,
            UnitNumber INTEGER NOT NULL,
            Description TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            UploadDate TEXT NOT NULL,
            ApprovalStatus TEXT NOT NULL DEFAULT 'Pending',
            ApprovedBy TEXT NOT NULL DEFAULT '',
            FacultyComments TEXT NOT NULL DEFAULT '',
            LikesCount INTEGER NOT NULL DEFAULT 0,
            IsBookmarked INTEGER NOT NULL DEFAULT 0,
            ReportStatus TEXT NOT NULL DEFAULT 'None',
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Faculty (
            FacultyId INTEGER PRIMARY KEY AUTOINCREMENT,
            FacultyCode TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Department TEXT NOT NULL,
            Designation TEXT NOT NULL,
            Email TEXT NOT NULL UNIQUE,
            MobileNumber TEXT NOT NULL,
            ProfileImagePath TEXT NOT NULL DEFAULT '',
            Qualification TEXT NOT NULL,
            Experience TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Sections (
            SectionId INTEGER PRIMARY KEY AUTOINCREMENT,
            Department TEXT NOT NULL,
            Year INTEGER NOT NULL,
            Semester INTEGER NOT NULL,
            SectionName TEXT NOT NULL,
            TotalStudents INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS FacultySections (
            FacultySectionId INTEGER PRIMARY KEY AUTOINCREMENT,
            FacultyId INTEGER NOT NULL,
            SectionId INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL,
            UNIQUE(FacultyId, SectionId, SubjectId),
            FOREIGN KEY(FacultyId) REFERENCES Faculty(FacultyId) ON DELETE CASCADE,
            FOREIGN KEY(SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS FacultyAttendanceDaily (
            AttendanceRecordId INTEGER PRIMARY KEY AUTOINCREMENT,
            FacultyId INTEGER NOT NULL,
            StudentId INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL,
            SectionId INTEGER NOT NULL,
            AttendanceDate TEXT NOT NULL,
            Status TEXT NOT NULL,
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(FacultyId, StudentId, SubjectId, AttendanceDate),
            FOREIGN KEY(FacultyId) REFERENCES Faculty(FacultyId) ON DELETE CASCADE,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE,
            FOREIGN KEY(SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS FacultyMarksRecords (
            FacultyMarkId INTEGER PRIMARY KEY AUTOINCREMENT,
            FacultyId INTEGER NOT NULL,
            StudentId INTEGER NOT NULL,
            SubjectId INTEGER NOT NULL,
            SectionId INTEGER NOT NULL,
            ExamType TEXT NOT NULL,
            MaxMarks INTEGER NOT NULL,
            MarksObtained REAL NOT NULL,
            Grade TEXT NOT NULL,
            Semester INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY(FacultyId) REFERENCES Faculty(FacultyId) ON DELETE CASCADE,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(SubjectId) REFERENCES Subjects(SubjectId) ON DELETE CASCADE,
            FOREIGN KEY(SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS StudentSectionMapping (
            MappingId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL UNIQUE,
            SectionId INTEGER NOT NULL,
            FOREIGN KEY(StudentId) REFERENCES StudentProfile(StudentId) ON DELETE CASCADE,
            FOREIGN KEY(SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE
        );
        """;

    private const string SeedSql = """
        INSERT INTO StudentProfile
            (StudentId, FullName, RegisterNumber, DateOfBirth, Gender, Email, MobileNumber, Address, ProfilePhotoPath)
        VALUES
            (1, 'Sai Kumar', 'RA2311033010096', '2005-08-14', 'Male', 'sai.kumar@student.edu', '9876543210',
             'Chennai, Tamil Nadu', '');

        INSERT INTO StudentUsers (StudentId, PasswordHash, IsActive) VALUES (1, $passwordHash, 1);

        INSERT INTO AcademicDetails
            (StudentId, Department, Degree, Year, CurrentSemester, Section, Batch, RollNumber, AdmissionNumber, MentorName, CGPA, Backlogs)
        VALUES
            (1, 'Computer Science and Engineering', 'B.Tech', 2, 4, 'CSE-SE-A', '2023-2027',
             '602223', 'ADM20230096', 'Dr. Suresh Kumar', 8.32, 0);

        INSERT INTO GuardianDetails
            (StudentId, FatherName, MotherName, ParentMobileNumber, ParentEmail, EmergencyContact)
        VALUES
            (1, 'Mr. Ramesh Kumar', 'Mrs. Lakshmi Kumar', '9876500001', 'parent@example.com', '9876500002');

        INSERT INTO Subjects (SubjectId, SubjectCode, SubjectName, FacultyName, Credits, SubjectType, SyllabusPath, CourseOutcomes) VALUES
            (1, '21CSC204J', 'Design and Analysis of Algorithms', 'Dr. Meera Sharma', 4, 'Theory', 'C:\Syllabus\DAA.pdf', 'Analyze algorithm complexity; apply greedy and dynamic programming methods.'),
            (2, '21CSC205P', 'Database Management Systems', 'Prof. Aravind Nair', 4, 'Lab', 'C:\Syllabus\DBMS.pdf', 'Design normalized schemas; write SQL; implement transactions.'),
            (3, '21CSC206T', 'Artificial Intelligence', 'Dr. Kavitha Rao', 3, 'Theory', 'C:\Syllabus\AI.pdf', 'Apply search, reasoning, and machine learning fundamentals.'),
            (4, '21CSE271T', 'Programming in Java', 'Prof. Nandhini S', 3, 'Theory', 'C:\Syllabus\Java.pdf', 'Develop object-oriented Java applications.'),
            (5, '21PDH209T', 'Social Engineering', 'Dr. Priya Menon', 2, 'Theory', 'C:\Syllabus\SE.pdf', 'Identify social engineering attacks and defenses.');

        INSERT INTO SemesterSubjects (Semester, SubjectId) VALUES (4,1), (4,2), (4,3), (4,4), (4,5);

        INSERT INTO Attendance (StudentId, SubjectId, Semester, PresentCount, AbsentCount, TotalClassesConducted, AttendancePercentage, MinimumRequired) VALUES
            (1, 1, 4, 38, 4, 42, 90.48, 75),
            (1, 2, 4, 34, 8, 42, 80.95, 75),
            (1, 3, 4, 29, 10, 39, 74.36, 75),
            (1, 4, 4, 36, 5, 41, 87.80, 75),
            (1, 5, 4, 30, 7, 37, 81.08, 75);

        INSERT INTO InternalMarks (StudentId, SubjectId, Semester, AssignmentMarks, QuizMarks, MidExamMarks, LabMarks, ExternalMarks, TotalMarks, Grade, GPA) VALUES
            (1, 1, 4, 9, 8, 25, 0, 46, 88, 'A+', 9),
            (1, 2, 4, 10, 9, 24, 18, 35, 96, 'O', 10),
            (1, 3, 4, 8, 8, 22, 0, 40, 78, 'A', 8),
            (1, 4, 4, 9, 7, 23, 0, 42, 81, 'A+', 9),
            (1, 5, 4, 8, 8, 24, 0, 45, 85, 'A+', 9);

        INSERT INTO Marks (StudentId, Semester, GPA, CGPA, ResultStatus, PublishedDate) VALUES
            (1, 1, 7.73, 7.73, 'Pass', '2023-12-20'),
            (1, 2, 8.33, 8.04, 'Pass', '2024-05-25'),
            (1, 3, 8.35, 8.14, 'Pass', '2024-11-28'),
            (1, 4, 8.60, 8.32, 'Published', $today);

        INSERT INTO LearningEvidence
            (StudentId, EvidenceTitle, Category, Semester, Subject, Description, UploadDate, FilePath, ValidationStatus, FacultyComments)
        VALUES
            (1, 'DBMS Mini Project ER Diagram', 'Project', 4, 'Database Management Systems', 'ER model and SQL schema for student evidence system.', '2026-04-15', 'C:\Evidence\dbms_project.pdf', 'Approved', 'Good schema clarity.'),
            (1, 'AI Workshop Certificate', 'Workshop/Seminar', 4, 'Artificial Intelligence', 'One-day AI tools workshop certificate.', '2026-04-18', 'C:\Evidence\ai_workshop.pdf', 'Pending', ''),
            (1, 'Java Assignment 2', 'Assignments', 4, 'Programming in Java', 'OOP concepts assignment.', '2026-04-20', 'C:\Evidence\java_assignment.pdf', 'Rejected', 'Please upload signed copy.');

        INSERT INTO CampusEvents
            (EventTitle, EventDate, Venue, ConductedBy, EventDescription, RegistrationDeadline, EventType)
        VALUES
            ('CodeSprint 2026', date('now', '+6 days'), 'Main Auditorium', 'CSE Coding Club', 'Competitive programming and problem solving contest.', date('now', '+3 days'), 'Technical'),
            ('AI Research Seminar', date('now', '+10 days'), 'Seminar Hall B', 'AI Department Forum', 'Research trends in applied AI and student publication guidance.', date('now', '+7 days'), 'Seminar'),
            ('Interdepartment Football League', date('now', '+15 days'), 'Sports Ground', 'Sports Committee', 'Semester sports league registration.', date('now', '+9 days'), 'Sports');

        INSERT INTO EventRegistrations (StudentId, EventId, RegistrationDate, Status) VALUES
            (1, 2, date('now'), 'Interested');

        INSERT INTO Notifications (StudentId, NotificationType, Title, Message, IsRead, CreatedAt) VALUES
            (1, 'LowAttendance', 'Attendance shortage alert', 'Artificial Intelligence attendance is below 75%.', 0, datetime('now', '-1 day')),
            (1, 'PendingEvidence', 'Evidence pending validation', 'AI Workshop Certificate is awaiting faculty validation.', 0, datetime('now', '-2 hours')),
            (1, 'UpcomingEvent', 'Upcoming campus event', 'CodeSprint 2026 registration closes soon.', 0, datetime('now')),
            (1, 'MarksUpdated', 'Internal marks updated', 'Current semester internal marks have been updated.', 1, datetime('now', '-3 days'));

        INSERT INTO ExtraCurricularActivities
            (StudentId, ActivityType, ActivityName, Description, DateFrom, DateTo, CertificatePath)
        VALUES
            (1, 'Club participation', 'Coding Club Member', 'Participated in weekly coding practice sessions.', '2025-08-01', '', ''),
            (1, 'Volunteering', 'Tech Fest Volunteer', 'Managed registration desk for department tech fest.', '2026-02-12', '2026-02-13', '');

        INSERT INTO Achievements
            (StudentId, AchievementType, Title, Description, AchievementDate, Organization, FilePath)
        VALUES
            (1, 'Certification', 'Cloud Fundamentals', 'Completed external cloud fundamentals certification.', '2026-01-24', 'Online Academy', 'C:\Certificates\cloud.pdf'),
            (1, 'Hackathon', 'Smart Campus Hackathon Finalist', 'Built a campus utility prototype.', '2026-03-10', 'CSE Department', 'C:\Certificates\hackathon.pdf');

        INSERT INTO StudentDocuments (StudentId, DocumentType, DocumentName, FilePath, UploadDate) VALUES
            (1, 'Resume', 'Sai Kumar Resume', 'C:\Documents\Sai_Resume.pdf', date('now', '-20 days')),
            (1, 'ID Card Copy', 'College ID Card', 'C:\Documents\IDCard.jpg', date('now', '-30 days')),
            (1, 'Certificate', 'Cloud Certificate', 'C:\Certificates\cloud.pdf', date('now', '-10 days'));

        INSERT INTO Faculty (FacultyId, FacultyCode, Name, Department, Designation, Email, MobileNumber, ProfileImagePath, Qualification, Experience) VALUES
            (1, 'FAC001', 'Dr. Meera Sharma', 'Computer Science and Engineering', 'Assistant Professor', 'meera.sharma@college.edu', '9876543210', '', 'Ph.D. in Computer Science', '12 years');

        INSERT INTO Sections (SectionId, Department, Year, Semester, SectionName, TotalStudents) VALUES
            (1, 'Computer Science and Engineering', 2, 4, 'CSE-SE-A', 55),
            (2, 'Computer Science and Engineering', 2, 4, 'CSE-SE-B', 55),
            (3, 'Computer Science and Engineering', 2, 4, 'CSE-SE-C', 55),
            (4, 'Computer Science and Engineering', 2, 4, 'CSE-SE-D', 55),
            (5, 'Computer Science and Engineering', 2, 4, 'CSE-SE-E', 55);

        INSERT INTO FacultySections (FacultyId, SectionId, SubjectId) VALUES
            (1, 1, 1), (1, 2, 1), (1, 3, 1), (1, 4, 1), (1, 5, 1),
            (1, 1, 2), (1, 2, 2), (1, 3, 2), (1, 4, 2), (1, 5, 2);

        INSERT INTO StudentSectionMapping (StudentId, SectionId) VALUES (1, 1);
        """;
}
