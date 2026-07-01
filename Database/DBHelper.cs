using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace StudentValidationSystem.Database;

public class DBHelper
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = StudentDatabase.DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        Pooling = false,
        DefaultTimeout = 10
    }.ToString();

    public void Initialize()
    {
        Directory.CreateDirectory(StudentDatabase.DataDirectory);
        using var connection = CreateConnection();
        connection.Open();
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS Users (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                Role TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS FacultyProfiles (
                UserId INTEGER PRIMARY KEY,
                FacultyName TEXT NOT NULL,
                Department TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Faculty (
                FacultyId INTEGER PRIMARY KEY,
                UserId INTEGER NOT NULL,
                FacultyCode TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Department TEXT NOT NULL,
                Designation TEXT NOT NULL,
                Email TEXT NOT NULL,
                MobileNumber TEXT NOT NULL,
                AssignedClasses TEXT NOT NULL,
                OfficeRoomNumber TEXT NOT NULL,
                Experience TEXT NOT NULL,
                Qualification TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Sections (
                SectionId INTEGER PRIMARY KEY,
                Department TEXT NOT NULL,
                Year INTEGER NOT NULL,
                Semester INTEGER NOT NULL,
                SectionName TEXT NOT NULL,
                TotalStudents INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Subjects (
                SubjectId INTEGER PRIMARY KEY AUTOINCREMENT,
                SubjectCode TEXT NOT NULL UNIQUE,
                SubjectName TEXT NOT NULL,
                FacultyName TEXT NOT NULL DEFAULT '',
                Credits INTEGER NOT NULL DEFAULT 3,
                SubjectType TEXT NOT NULL DEFAULT 'Theory',
                SyllabusPath TEXT NOT NULL DEFAULT '',
                CourseOutcomes TEXT NOT NULL DEFAULT '',
                Department TEXT NOT NULL DEFAULT 'Computer Science and Engineering',
                Semester INTEGER NOT NULL DEFAULT 4,
                FacultyAssigned TEXT NOT NULL DEFAULT ''
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

            CREATE TABLE IF NOT EXISTS Students (
                StudentId INTEGER PRIMARY KEY,
                RegisterNumber TEXT NOT NULL UNIQUE,
                StudentName TEXT NOT NULL,
                Department TEXT NOT NULL,
                Semester INTEGER NOT NULL,
                Section TEXT NOT NULL,
                AttendancePercentage REAL NOT NULL,
                CGPA REAL NOT NULL,
                EvidenceStatus TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS FacultySubjects (
                FacultySubjectId INTEGER PRIMARY KEY AUTOINCREMENT,
                FacultyId INTEGER NOT NULL,
                SubjectCode TEXT NOT NULL,
                SubjectName TEXT NOT NULL,
                Semester INTEGER NOT NULL,
                Section TEXT NOT NULL,
                Credits INTEGER NOT NULL,
                FOREIGN KEY(FacultyId) REFERENCES Faculty(FacultyId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS FacultyAttendanceDaily (
                AttendanceRecordId INTEGER PRIMARY KEY AUTOINCREMENT,
                FacultyId INTEGER NOT NULL,
                StudentId INTEGER NOT NULL,
                SubjectCode TEXT NOT NULL,
                Semester INTEGER NOT NULL,
                AttendanceDate TEXT NOT NULL,
                Status TEXT NOT NULL,
                UNIQUE(StudentId, SubjectCode, AttendanceDate),
                FOREIGN KEY(FacultyId) REFERENCES Faculty(FacultyId) ON DELETE CASCADE,
                FOREIGN KEY(StudentId) REFERENCES Students(StudentId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS FacultyMarksRecords (
                FacultyMarkId INTEGER PRIMARY KEY AUTOINCREMENT,
                FacultyId INTEGER NOT NULL,
                StudentId INTEGER NOT NULL,
                SubjectCode TEXT NOT NULL,
                Semester INTEGER NOT NULL,
                ExamType TEXT NOT NULL,
                MaxMarks INTEGER NOT NULL,
                MarksObtained REAL NOT NULL,
                Grade TEXT NOT NULL,
                Remarks TEXT NOT NULL,
                UNIQUE(StudentId, SubjectCode, Semester, ExamType),
                FOREIGN KEY(FacultyId) REFERENCES Faculty(FacultyId) ON DELETE CASCADE,
                FOREIGN KEY(StudentId) REFERENCES Students(StudentId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Departments (
                DepartmentId INTEGER PRIMARY KEY AUTOINCREMENT,
                DepartmentName TEXT NOT NULL UNIQUE,
                HodName TEXT NOT NULL DEFAULT '',
                Status TEXT NOT NULL DEFAULT 'Active'
            );

            CREATE TABLE IF NOT EXISTS Reports (
                ReportId INTEGER PRIMARY KEY AUTOINCREMENT,
                ReportName TEXT NOT NULL,
                ReportType TEXT NOT NULL,
                GeneratedBy TEXT NOT NULL,
                GeneratedAt TEXT NOT NULL,
                FilePath TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS ActivityLogs (
                ActivityId INTEGER PRIMARY KEY AUTOINCREMENT,
                ActorRole TEXT NOT NULL,
                ActorName TEXT NOT NULL,
                Action TEXT NOT NULL,
                EntityName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
        EnsureUserColumn(connection, "Email", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "IsActive", "INTEGER NOT NULL DEFAULT 1");
        EnsureUserColumn(connection, "LastLogin", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "RegisterNumber", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "FacultyId", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "FullName", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "Department", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "Semester", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "Section", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "Designation", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "Mobile", "TEXT NOT NULL DEFAULT ''");
        EnsureUserColumn(connection, "CreatedDate", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyColumn(connection, "Mobile", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyColumn(connection, "OfficeRoom", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyColumn(connection, "ProfileImagePath", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyColumn(connection, "UserId", "INTEGER NOT NULL DEFAULT 1");
        EnsureFacultyColumn(connection, "AssignedClasses", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyColumn(connection, "OfficeRoomNumber", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyColumn(connection, "Status", "TEXT NOT NULL DEFAULT 'Active'");
        EnsureStudentColumn(connection, "Name", "TEXT NOT NULL DEFAULT ''");
        EnsureStudentColumn(connection, "Year", "INTEGER NOT NULL DEFAULT 2");
        EnsureStudentColumn(connection, "SectionId", "INTEGER NOT NULL DEFAULT 1");
        EnsureStudentColumn(connection, "Email", "TEXT NOT NULL DEFAULT ''");
        EnsureStudentColumn(connection, "Mobile", "TEXT NOT NULL DEFAULT ''");
        EnsureStudentColumn(connection, "ProfileImagePath", "TEXT NOT NULL DEFAULT ''");
        EnsureStudentColumn(connection, "Status", "TEXT NOT NULL DEFAULT 'Active'");
        EnsureSectionColumn(connection, "ClassInCharge", "TEXT NOT NULL DEFAULT ''");
        EnsureSubjectColumn(connection, "Department", "TEXT NOT NULL DEFAULT 'Computer Science and Engineering'");
        EnsureSubjectColumn(connection, "Semester", "INTEGER NOT NULL DEFAULT 4");
        EnsureSubjectColumn(connection, "FacultyAssigned", "TEXT NOT NULL DEFAULT ''");
        EnsureAttendanceDailyColumn(connection, "SubjectCode", "TEXT NOT NULL DEFAULT ''");
        EnsureAttendanceDailyColumn(connection, "Semester", "INTEGER NOT NULL DEFAULT 4");
        EnsureAttendanceDailyColumn(connection, "SubjectId", "INTEGER NOT NULL DEFAULT 1");
        EnsureAttendanceDailyColumn(connection, "SectionId", "INTEGER NOT NULL DEFAULT 1");
        EnsureAttendanceDailyColumn(connection, "AttendancePercentage", "REAL NOT NULL DEFAULT 0");
        EnsureMarksRecordColumn(connection, "SubjectCode", "TEXT NOT NULL DEFAULT ''");
        EnsureMarksRecordColumn(connection, "Semester", "INTEGER NOT NULL DEFAULT 4");
        EnsureMarksRecordColumn(connection, "SubjectId", "INTEGER NOT NULL DEFAULT 1");
        EnsureMarksRecordColumn(connection, "SectionId", "INTEGER NOT NULL DEFAULT 1");
        EnsureMarksRecordColumn(connection, "Remarks", "TEXT NOT NULL DEFAULT ''");
        EnsureFacultyIndexes(connection);
        EnsureOptionalTableColumn(connection, "LearningEvidence", "FacultyId", "INTEGER NOT NULL DEFAULT 1");
        EnsureOptionalTableColumn(connection, "Notes", "UploadedBy", "TEXT NOT NULL DEFAULT ''");
        SeedUsers(connection);
        SeedFacultyDashboardData(connection);
        SeedAdminModuleData(connection);
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

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
        Execute(connection, sql, parameters);
    }

    public static string HashPassword(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    private static void SeedUsers(SqliteConnection connection)
    {
        Execute(connection, """
            INSERT INTO Users (Username, Password, Role)
            VALUES
                ('faculty1', $facultyPassword, 'Faculty'),
                ('faculty1@college.edu', $facultyPassword, 'Faculty'),
                ('admin1', $adminPassword, 'Admin'),
                ('student1', $studentPassword, 'Student')
            ON CONFLICT(Username) DO UPDATE SET
                Password = excluded.Password,
                Role = excluded.Role;

            UPDATE Users SET Email = 'admin@college.edu', IsActive = 1 WHERE Username = 'admin1';
            UPDATE Users SET Email = 'faculty1@college.edu', IsActive = 1 WHERE Username = 'faculty1';
            UPDATE Users SET Email = 'faculty1@college.edu', IsActive = 1 WHERE Username = 'faculty1@college.edu';
            UPDATE Users
            SET Email = 'sai.kumar@student.edu',
                RegisterNumber = 'RA2311033010096',
                FullName = 'Sai Kumar',
                Department = 'Computer Science and Engineering',
                Semester = '4',
                Section = 'CSE-SE-A',
                IsActive = 1
            WHERE Username = 'student1';
            """,
            ("$facultyPassword", HashPassword("1234")),
            ("$adminPassword", HashPassword("admin123")),
            ("$studentPassword", HashPassword("student123")));

        Execute(connection, """
            INSERT INTO FacultyProfiles (UserId, FacultyName, Department)
            SELECT UserId, 'Dr. Meera Sharma', 'Computer Science and Engineering'
            FROM Users WHERE Username = 'faculty1'
            ON CONFLICT(UserId) DO UPDATE SET
                FacultyName = excluded.FacultyName,
                Department = excluded.Department;

            INSERT INTO FacultyProfiles (UserId, FacultyName, Department)
            SELECT UserId, 'Dr. Meera Sharma', 'Computer Science and Engineering'
            FROM Users WHERE Username = 'faculty1@college.edu'
            ON CONFLICT(UserId) DO UPDATE SET
                FacultyName = excluded.FacultyName,
                Department = excluded.Department;
            """);
    }

    private static void SeedFacultyDashboardData(SqliteConnection connection)
    {
        var facultyImagePath = EnsurePlaceholderImage("Faculty_1_Profile.jpg", "MS", Color.FromArgb(37, 99, 145));
        var studentImagePath = EnsurePlaceholderImage("Student_Placeholder.jpg", "ST", Color.FromArgb(54, 116, 92));

        Execute(connection, """
            INSERT INTO Faculty
                (FacultyId, UserId, FacultyCode, Name, Department, Designation, Email, MobileNumber,
                 AssignedClasses, OfficeRoomNumber, Experience, Qualification, Mobile, ProfileImagePath)
            SELECT 1, UserId, 'FAC-CSE-001', 'Dr. Meera Sharma', 'Computer Science and Engineering',
                   'Associate Professor', 'faculty1@college.edu', '9876501111',
                   'CSE-SE-A Semester 4; CSE-SE-B Semester 4', 'CSE Block - 214', '12 years',
                   'Ph.D. Computer Science, M.Tech Software Engineering', '9876501111', $profileImagePath
            FROM Users WHERE Username = 'faculty1'
            ON CONFLICT(FacultyId) DO UPDATE SET
                UserId = excluded.UserId,
                FacultyCode = excluded.FacultyCode,
                Name = excluded.Name,
                Department = excluded.Department,
                Designation = excluded.Designation,
                Email = excluded.Email,
                MobileNumber = excluded.MobileNumber,
                AssignedClasses = excluded.AssignedClasses,
                OfficeRoomNumber = excluded.OfficeRoomNumber,
                OfficeRoom = excluded.OfficeRoomNumber,
                Experience = excluded.Experience,
                Qualification = excluded.Qualification,
                Mobile = excluded.Mobile,
                ProfileImagePath = CASE WHEN Faculty.ProfileImagePath = '' THEN excluded.ProfileImagePath ELSE Faculty.ProfileImagePath END;

            DELETE FROM FacultySubjects WHERE FacultyId = 1;
            INSERT INTO FacultySubjects (FacultyId, SubjectCode, SubjectName, Semester, Section, Credits) VALUES
                (1, '23CSE206', 'Design and Analysis of Algorithms', 4, 'CSE-SE-A', 4),
                (1, '23CSE208', 'Software Engineering', 4, 'CSE-SE-A', 3),
                (1, '23CSE303', 'Artificial Intelligence', 5, 'CSE-AI-A', 3),
                (1, '23CSE307', 'Machine Learning', 6, 'CSE-DS-A', 4);
            """, ("$profileImagePath", facultyImagePath));

        Execute(connection, """
            INSERT INTO Sections (SectionId, Department, Year, Semester, SectionName, TotalStudents) VALUES
                (1, 'Computer Science and Engineering', 2, 4, 'CSE-SE-A', 55),
                (2, 'Computer Science and Engineering', 2, 4, 'CSE-SE-B', 55),
                (3, 'Computer Science and Engineering', 3, 5, 'CSE-AI-A', 55),
                (4, 'Computer Science and Engineering', 3, 6, 'CSE-DS-A', 55),
                (5, 'Computer Science and Engineering', 4, 7, 'CSE-FS-A', 55)
            ON CONFLICT(SectionId) DO UPDATE SET
                Department = excluded.Department,
                Year = excluded.Year,
                Semester = excluded.Semester,
                SectionName = excluded.SectionName,
                TotalStudents = excluded.TotalStudents;
            """);

        EnsureCoreFacultySubjects(connection);

        Execute(connection, "DELETE FROM FacultySections WHERE FacultyId = 1;");
        Execute(connection, """
            INSERT INTO FacultySections (FacultyId, SectionId, SubjectId)
            SELECT 1, 1, SubjectId FROM Subjects WHERE SubjectCode = '23CSE206'
            UNION ALL SELECT 1, 2, SubjectId FROM Subjects WHERE SubjectCode = '23CSE208'
            UNION ALL SELECT 1, 3, SubjectId FROM Subjects WHERE SubjectCode = '23CSE303'
            UNION ALL SELECT 1, 4, SubjectId FROM Subjects WHERE SubjectCode = '23CSE307'
            UNION ALL SELECT 1, 5, SubjectId FROM Subjects WHERE SubjectCode = '23CSE206';
            """);

        if (FacultySampleDataReady(connection))
        {
            return;
        }

        Execute(connection, "DELETE FROM Students;");
        var firstNames = new[] { "Aarav", "Isha", "Rohan", "Meera", "Kavin", "Diya", "Nikhil", "Sneha", "Aditya", "Nisha", "Rahul", "Anika", "Vikram", "Divya", "Arjun", "Priya", "Karthik", "Megha", "Harish", "Farah" };
        var lastNames = new[] { "Sharma", "Nair", "Reddy", "Iyer", "Khan", "Menon", "Varma", "Joshi", "Kapoor", "Raman" };
        var sections = new[]
        {
            (Id: 1, Year: 2, Sem: 4, Name: "CSE-SE-A", Count: 55),
            (Id: 2, Year: 2, Sem: 4, Name: "CSE-SE-B", Count: 55),
            (Id: 3, Year: 3, Sem: 5, Name: "CSE-AI-A", Count: 55),
            (Id: 4, Year: 3, Sem: 6, Name: "CSE-DS-A", Count: 55),
            (Id: 5, Year: 4, Sem: 7, Name: "CSE-FS-A", Count: 55)
        };
        var studentId = 1;
        foreach (var section in sections)
        {
            for (var index = 1; index <= section.Count; index++)
            {
                var name = $"{firstNames[(studentId + index) % firstNames.Length]} {lastNames[(studentId + section.Id) % lastNames.Length]}";
                var attendance = Math.Round(68 + ((studentId * 7) % 29) + (index % 3) * 0.25, 2);
                var cgpa = Math.Round(6.8 + ((studentId * 11) % 28) / 10.0, 2);
                var evidence = studentId % 6 == 0 ? "Rejected" : studentId % 4 == 0 ? "Pending" : studentId % 3 == 0 ? "Submitted" : "Approved";
                Execute(connection, """
                    INSERT INTO Students
                        (StudentId, RegisterNumber, StudentName, Name, Department, Year, Semester, SectionId, Section,
                         Email, Mobile, ProfileImagePath, AttendancePercentage, CGPA, EvidenceStatus)
                    VALUES
                        ($studentId, $register, $name, $name, 'Computer Science and Engineering', $year, $semester, $sectionId, $section,
                         $email, $mobile, $profileImagePath, $attendance, $cgpa, $evidence)
                    ON CONFLICT(StudentId) DO UPDATE SET
                        RegisterNumber = excluded.RegisterNumber,
                        StudentName = excluded.StudentName,
                        Name = excluded.Name,
                        Department = excluded.Department,
                        Year = excluded.Year,
                        Semester = excluded.Semester,
                        SectionId = excluded.SectionId,
                        Section = excluded.Section,
                        Email = excluded.Email,
                        Mobile = excluded.Mobile,
                        ProfileImagePath = CASE WHEN Students.ProfileImagePath = '' THEN excluded.ProfileImagePath ELSE Students.ProfileImagePath END,
                        AttendancePercentage = excluded.AttendancePercentage,
                        CGPA = excluded.CGPA,
                        EvidenceStatus = excluded.EvidenceStatus;
                    """,
                    ("$studentId", studentId),
                    ("$register", $"RA2311033{studentId:00000}"),
                    ("$name", name),
                    ("$year", section.Year),
                    ("$semester", section.Sem),
                    ("$sectionId", section.Id),
                    ("$section", section.Name),
                    ("$email", $"student{studentId:000}@college.edu"),
                    ("$mobile", $"98765{studentId:00000}"),
                    ("$profileImagePath", studentImagePath),
                    ("$attendance", attendance),
                    ("$cgpa", cgpa),
                    ("$evidence", evidence));
                studentId++;
            }
        }

        EnsureStudentModuleRows(connection);

        // Keep the sample student login mapped to a recognizable profile row.
        Execute(connection, """
            UPDATE Students
            SET RegisterNumber = 'RA2311033010096',
                StudentName = 'Sai Kumar',
                Name = 'Sai Kumar',
                Email = 'sai.kumar@student.edu',
                Mobile = '9876543210',
                ProfileImagePath = $profileImagePath,
                AttendancePercentage = 81.62,
                CGPA = 8.32,
                EvidenceStatus = 'Pending'
            WHERE StudentId = 1;
            """, ("$profileImagePath", studentImagePath));

        Execute(connection, "DELETE FROM FacultyAttendanceDaily WHERE FacultyId = 1;");
        var assigned = GetAssignedSections(connection);
        foreach (var assignment in assigned)
        {
            var students = StudentIdsForSection(connection, assignment.SectionId);
            foreach (var sid in students)
            {
                var status = (sid + assignment.SubjectCode.Length) % 5 == 0 ? "Absent" : "Present";
                Execute(connection, """
                    INSERT INTO FacultyAttendanceDaily
                        (FacultyId, StudentId, SubjectCode, SubjectId, SectionId, Semester, AttendanceDate, Status)
                    VALUES
                        (1, $studentId, $subjectCode, $subjectId, $sectionId, $semester, date('now'), $status)
                    ON CONFLICT(StudentId, SubjectCode, AttendanceDate) DO UPDATE SET
                        Status = excluded.Status;
                    """, ("$studentId", sid), ("$subjectCode", assignment.SubjectCode), ("$subjectId", assignment.SubjectId),
                    ("$sectionId", assignment.SectionId), ("$semester", assignment.Semester), ("$status", status));
            }
        }

        Execute(connection, "DELETE FROM FacultyMarksRecords WHERE FacultyId = 1;");
        foreach (var assignment in assigned)
        {
            foreach (var examType in new[] { "Internal 1", "Assignment", "Quiz" })
            {
                foreach (var sid in StudentIdsForSection(connection, assignment.SectionId))
                {
                    var marks = 62 + ((sid * 4 + assignment.SubjectCode.Length + examType.Length) % 34);
                    Execute(connection, """
                        INSERT INTO FacultyMarksRecords
                            (FacultyId, StudentId, SubjectCode, SubjectId, SectionId, Semester, ExamType, MaxMarks, MarksObtained, Grade, Remarks)
                        VALUES
                            (1, $studentId, $subjectCode, $subjectId, $sectionId, $semester, $examType, 100, $marks, $grade, $remarks)
                        ON CONFLICT(StudentId, SubjectCode, Semester, ExamType) DO UPDATE SET
                            MarksObtained = excluded.MarksObtained,
                            Grade = excluded.Grade,
                            Remarks = excluded.Remarks;
                        """,
                        ("$studentId", sid),
                        ("$subjectCode", assignment.SubjectCode),
                        ("$subjectId", assignment.SubjectId),
                        ("$sectionId", assignment.SectionId),
                        ("$semester", assignment.Semester),
                        ("$examType", examType),
                        ("$marks", marks),
                        ("$grade", marks >= 90 ? "O" : marks >= 80 ? "A+" : marks >= 70 ? "A" : "B+"),
                        ("$remarks", marks >= 75 ? "Good progress" : "Needs improvement"));
                }
            }
        }

        SeedFacultyEvidenceNotesAndEvents(connection);
    }

    private static void EnsureCoreFacultySubjects(SqliteConnection connection)
    {
        var subjects = new[]
        {
            ("23CSE206", "Design and Analysis of Algorithms", "Computer Science and Engineering", 4, 4, "Theory", "Dr. Meera Sharma"),
            ("23CSE208", "Software Engineering", "Computer Science and Engineering", 4, 3, "Theory", "Dr. Meera Sharma"),
            ("23CSE303", "Artificial Intelligence", "Computer Science and Engineering", 5, 3, "Theory", "Dr. Kavitha Rao"),
            ("23CSE307", "Machine Learning", "Computer Science and Engineering", 6, 4, "Theory", "Dr. Naveen Raj")
        };

        foreach (var subject in subjects)
        {
            Execute(connection, """
                INSERT INTO Subjects
                    (SubjectCode, SubjectName, FacultyName, Credits, SubjectType, SyllabusPath,
                     CourseOutcomes, Department, Semester, FacultyAssigned)
                VALUES
                    ($code, $name, $faculty, $credits, $type, '', 'Course outcomes mapped by faculty module.',
                     $department, $semester, $faculty)
                ON CONFLICT(SubjectCode) DO UPDATE SET
                    SubjectName = excluded.SubjectName,
                    FacultyName = excluded.FacultyName,
                    Credits = excluded.Credits,
                    SubjectType = excluded.SubjectType,
                    Department = excluded.Department,
                    Semester = excluded.Semester,
                    FacultyAssigned = excluded.FacultyAssigned;
                """,
                ("$code", subject.Item1),
                ("$name", subject.Item2),
                ("$department", subject.Item3),
                ("$semester", subject.Item4),
                ("$credits", subject.Item5),
                ("$type", subject.Item6),
                ("$faculty", subject.Item7));
        }
    }

    private static void EnsureStudentModuleRows(SqliteConnection connection)
    {
        if (TableExists(connection, "StudentProfile"))
        {
            Execute(connection, """
                INSERT INTO StudentProfile
                    (StudentId, FullName, RegisterNumber, DateOfBirth, Gender, Email, MobileNumber, Address, ProfilePhotoPath)
                SELECT st.StudentId, st.Name, st.RegisterNumber, '2004-01-01', 'Not specified',
                       st.Email, st.Mobile, 'Chennai, Tamil Nadu', st.ProfileImagePath
                FROM Students st
                WHERE true
                ON CONFLICT(StudentId) DO UPDATE SET
                    FullName = excluded.FullName,
                    RegisterNumber = excluded.RegisterNumber,
                    Email = excluded.Email,
                    MobileNumber = excluded.MobileNumber,
                    ProfilePhotoPath = CASE
                        WHEN StudentProfile.ProfilePhotoPath = '' THEN excluded.ProfilePhotoPath
                        ELSE StudentProfile.ProfilePhotoPath
                    END;
                """);
        }

        if (TableExists(connection, "AcademicDetails"))
        {
            Execute(connection, """
                INSERT INTO AcademicDetails
                    (StudentId, Department, Degree, Year, CurrentSemester, Section, Batch, RollNumber,
                     AdmissionNumber, MentorName, CGPA, Backlogs)
                SELECT st.StudentId, st.Department, 'B.Tech', st.Year, st.Semester, st.Section,
                       '2023-2027', st.RegisterNumber, st.RegisterNumber, 'Dr. Meera Sharma', st.CGPA, 0
                FROM Students st
                WHERE true
                ON CONFLICT(StudentId) DO UPDATE SET
                    Department = excluded.Department,
                    Year = excluded.Year,
                    CurrentSemester = excluded.CurrentSemester,
                    Section = excluded.Section,
                    CGPA = excluded.CGPA;
                """);
        }

        if (TableExists(connection, "StudentSectionMapping"))
        {
            Execute(connection, """
                INSERT INTO StudentSectionMapping (StudentId, SectionId)
                SELECT StudentId, SectionId
                FROM Students
                WHERE true
                ON CONFLICT(StudentId) DO UPDATE SET
                    SectionId = excluded.SectionId;
                """);
        }
    }

    private static void SeedFacultyEvidenceNotesAndEvents(SqliteConnection connection)
    {
        Execute(connection, "DELETE FROM LearningEvidence WHERE FacultyId = 1;");
        foreach (var studentId in StudentIdsForSection(connection, 1).Take(18)
                     .Concat(StudentIdsForSection(connection, 2).Take(14))
                     .Concat(StudentIdsForSection(connection, 3).Take(12)))
        {
            var status = studentId % 5 == 0 ? "Rejected" : studentId % 3 == 0 ? "Pending" : "Approved";
            Execute(connection, """
                INSERT INTO LearningEvidence
                    (StudentId, FacultyId, EvidenceTitle, Title, Category, Semester, SubjectId, Subject, Description,
                     UploadDate, FilePath, ValidationStatus, Status, FacultyComments)
                SELECT $studentId, 1, $title, $title, $category, sec.Semester, fs.SubjectId, sub.SubjectName,
                       'Faculty dashboard sample evidence for progress validation.',
                       date('now', '-' || ($studentId % 12) || ' days'), 'sample-evidence.pdf',
                       $status, $status, $comments
                FROM Students st
                JOIN Sections sec ON sec.SectionId = st.SectionId
                JOIN FacultySections fs ON fs.SectionId = sec.SectionId AND fs.FacultyId = 1
                JOIN Subjects sub ON sub.SubjectId = fs.SubjectId
                WHERE st.StudentId = $studentId
                LIMIT 1;
                """,
                ("$studentId", studentId),
                ("$title", $"Learning Evidence {studentId:000}"),
                ("$category", studentId % 2 == 0 ? "Project" : "Assignment"),
                ("$status", status),
                ("$comments", status == "Approved" ? "Good work." : status == "Rejected" ? "Needs correction." : ""));
        }

        Execute(connection, "DELETE FROM Notes WHERE FacultyId = 1;");
        Execute(connection, """
            INSERT INTO Notes
                (StudentId, FacultyId, UploadedByRole, UploadedBy, Title, SubjectId, Semester, UnitNumber, Description,
                 FilePath, UploadDate, ApprovalStatus, ApprovedBy, FacultyComments, LikesCount, IsBookmarked, ReportStatus)
            SELECT NULL, 1, 'Faculty', 'Dr. Meera Sharma', 'Algorithm Design Notes', SubjectId, 4, 1,
                   'Greedy, divide and conquer, and dynamic programming notes.', 'algorithm-notes.pdf',
                   date('now', '-4 days'), 'Approved', 'Faculty', 'Faculty uploaded material', 0, 0, 'None'
            FROM Subjects WHERE SubjectCode = '23CSE206'
            UNION ALL
            SELECT NULL, 1, 'Faculty', 'Dr. Meera Sharma', 'Software Engineering Case Study', SubjectId, 4, 2,
                   'SRS and lifecycle case-study handout.', 'se-case-study.pdf',
                   date('now', '-2 days'), 'Approved', 'Faculty', 'Faculty uploaded material', 0, 0, 'None'
            FROM Subjects WHERE SubjectCode = '23CSE208'
            UNION ALL
            SELECT NULL, 1, 'Faculty', 'Dr. Meera Sharma', 'AI Search Quick Reference', SubjectId, 5, 3,
                   'Search strategies and heuristics reference.', 'ai-search.pdf',
                   date('now', '-1 day'), 'Approved', 'Faculty', 'Faculty uploaded material', 0, 0, 'None'
            FROM Subjects WHERE SubjectCode = '23CSE303';
            """);

        Execute(connection, """
            DELETE FROM CampusEvents
            WHERE EventTitle IN ('Faculty Mentoring Review', 'Evidence Portfolio Workshop', 'Internal Assessment Briefing');

            INSERT INTO CampusEvents (EventTitle, EventDate, Venue, ConductedBy, EventDescription, RegistrationDeadline, EventType)
            VALUES
                ('Faculty Mentoring Review', date('now', '+3 days'), 'CSE Seminar Hall', 'CSE Department', 'Section-wise mentoring and progress review.', date('now', '+2 days'), 'Academic'),
                ('Evidence Portfolio Workshop', date('now', '+7 days'), 'Lab 4', 'Dr. Meera Sharma', 'Guidance on preparing learning evidence.', date('now', '+5 days'), 'Workshop'),
                ('Internal Assessment Briefing', date('now', '+10 days'), 'Main Auditorium', 'Exam Cell', 'Assessment instructions for faculty and students.', date('now', '+8 days'), 'Academic');
            """);
    }

    private static void SeedAdminModuleData(SqliteConnection connection)
    {
        if (AdminSampleDataReady(connection))
        {
            return;
        }

        Execute(connection, """
            INSERT INTO Departments (DepartmentName, HodName, Status) VALUES
                ('Computer Science and Engineering', 'Dr. Anand Kumar', 'Active'),
                ('Artificial Intelligence and Data Science', 'Dr. Kavitha Rao', 'Active'),
                ('Information Technology', 'Dr. Naveen Raj', 'Active'),
                ('Electronics and Communication Engineering', 'Dr. Priya Menon', 'Active'),
                ('Electrical and Electronics Engineering', 'Dr. S. Gopal', 'Active'),
                ('Mechanical Engineering', 'Dr. Arvind Iyer', 'Active')
            ON CONFLICT(DepartmentName) DO UPDATE SET
                HodName = excluded.HodName,
                Status = excluded.Status;

            UPDATE Sections SET ClassInCharge = CASE SectionName
                WHEN 'CSE-SE-A' THEN 'Dr. Meera Sharma'
                WHEN 'CSE-SE-B' THEN 'Prof. Arjun Nair'
                WHEN 'CSE-AI-A' THEN 'Dr. Kavitha Rao'
                WHEN 'CSE-DS-A' THEN 'Dr. Naveen Raj'
                WHEN 'CSE-FS-A' THEN 'Prof. Lakshmi Iyer'
                ELSE ClassInCharge
            END;
            """);

        var subjects = new[]
        {
            ("23CSE206", "Design and Analysis of Algorithms", "Computer Science and Engineering", 4, 4, "Theory", "Dr. Meera Sharma"),
            ("23CSE208", "Software Engineering", "Computer Science and Engineering", 4, 3, "Theory", "Dr. Meera Sharma"),
            ("23CSE303", "Artificial Intelligence", "Computer Science and Engineering", 5, 3, "Theory", "Dr. Kavitha Rao"),
            ("23CSE307", "Machine Learning", "Computer Science and Engineering", 6, 4, "Theory", "Dr. Naveen Raj"),
            ("23CSE401", "Full Stack Development", "Computer Science and Engineering", 7, 3, "Lab", "Prof. Lakshmi Iyer"),
            ("23CSE211", "Database Management Systems", "Computer Science and Engineering", 4, 4, "Theory", "Prof. Arjun Nair"),
            ("23CSE214", "Operating Systems", "Computer Science and Engineering", 4, 3, "Theory", "Dr. Sanjay Gupta"),
            ("23CSE301", "Computer Networks", "Computer Science and Engineering", 5, 3, "Theory", "Prof. Anita Bose"),
            ("23CSE305", "Data Warehousing", "Computer Science and Engineering", 5, 3, "Theory", "Dr. Farah Khan"),
            ("23CSE309", "Cloud Computing", "Computer Science and Engineering", 6, 3, "Theory", "Prof. Joseph Mathew"),
            ("23CSE312", "Cyber Security", "Computer Science and Engineering", 6, 3, "Theory", "Dr. Rakesh Verma"),
            ("23CSE410", "Project Phase 1", "Computer Science and Engineering", 7, 4, "Project", "Dr. Meera Sharma"),
            ("23CSE411", "DevOps Laboratory", "Computer Science and Engineering", 7, 2, "Lab", "Prof. Lakshmi Iyer"),
            ("23CSE412", "Mobile Application Development", "Computer Science and Engineering", 7, 3, "Lab", "Prof. Joseph Mathew"),
            ("23CSE501", "Big Data Analytics", "Artificial Intelligence and Data Science", 5, 3, "Theory", "Dr. Kavitha Rao"),
            ("23CSE502", "Natural Language Processing", "Artificial Intelligence and Data Science", 6, 3, "Theory", "Dr. Farah Khan"),
            ("23IT201", "Web Technologies", "Information Technology", 4, 3, "Lab", "Prof. Anita Bose"),
            ("23ECE202", "Digital Electronics", "Electronics and Communication Engineering", 4, 3, "Theory", "Dr. Priya Menon"),
            ("23EEE203", "Control Systems", "Electrical and Electronics Engineering", 5, 3, "Theory", "Dr. S. Gopal"),
            ("23ME204", "Engineering Design", "Mechanical Engineering", 4, 3, "Project", "Dr. Arvind Iyer")
        };

        foreach (var subject in subjects)
        {
            Execute(connection, """
                INSERT INTO Subjects
                    (SubjectCode, SubjectName, FacultyName, Credits, SubjectType, SyllabusPath,
                     CourseOutcomes, Department, Semester, FacultyAssigned)
                VALUES
                    ($code, $name, $faculty, $credits, $type, '', 'Course outcomes mapped by admin module.',
                     $department, $semester, $faculty)
                ON CONFLICT(SubjectCode) DO UPDATE SET
                    SubjectName = excluded.SubjectName,
                    FacultyName = excluded.FacultyName,
                    Credits = excluded.Credits,
                    SubjectType = excluded.SubjectType,
                    Department = excluded.Department,
                    Semester = excluded.Semester,
                    FacultyAssigned = excluded.FacultyAssigned;
                """,
                ("$code", subject.Item1),
                ("$name", subject.Item2),
                ("$department", subject.Item3),
                ("$semester", subject.Item4),
                ("$credits", subject.Item5),
                ("$type", subject.Item6),
                ("$faculty", subject.Item7));
        }

        var facultyRows = new[]
        {
            (2, "FAC-CSE-002", "Prof. Arjun Nair", "Computer Science and Engineering", "Assistant Professor", "faculty2@college.edu", "9876502222", "CSE Block - 215", "9 years", "M.Tech Computer Science"),
            (3, "FAC-AI-003", "Dr. Kavitha Rao", "Artificial Intelligence and Data Science", "Professor", "faculty3@college.edu", "9876503333", "AI Block - 102", "15 years", "Ph.D. Artificial Intelligence"),
            (4, "FAC-DS-004", "Dr. Naveen Raj", "Computer Science and Engineering", "Associate Professor", "faculty4@college.edu", "9876504444", "CSE Block - 216", "11 years", "Ph.D. Data Science"),
            (5, "FAC-FS-005", "Prof. Lakshmi Iyer", "Computer Science and Engineering", "Assistant Professor", "faculty5@college.edu", "9876505555", "CSE Block - 217", "8 years", "M.Tech Software Systems"),
            (6, "FAC-CSE-006", "Dr. Sanjay Gupta", "Computer Science and Engineering", "Professor", "faculty6@college.edu", "9876506666", "CSE Block - 218", "18 years", "Ph.D. Systems"),
            (7, "FAC-CSE-007", "Prof. Anita Bose", "Information Technology", "Assistant Professor", "faculty7@college.edu", "9876507777", "IT Block - 104", "7 years", "M.Tech Information Technology"),
            (8, "FAC-AI-008", "Dr. Farah Khan", "Artificial Intelligence and Data Science", "Associate Professor", "faculty8@college.edu", "9876508888", "AI Block - 106", "10 years", "Ph.D. Machine Learning"),
            (9, "FAC-CSE-009", "Prof. Joseph Mathew", "Computer Science and Engineering", "Assistant Professor", "faculty9@college.edu", "9876509999", "CSE Block - 219", "6 years", "M.Tech Cloud Computing"),
            (10, "FAC-CSE-010", "Dr. Rakesh Verma", "Computer Science and Engineering", "Professor", "faculty10@college.edu", "9876510000", "CSE Block - 220", "16 years", "Ph.D. Cyber Security")
        };

        foreach (var faculty in facultyRows)
        {
            Execute(connection, """
                INSERT INTO Users (Username, Password, Role, Email, IsActive)
                VALUES ($email, $password, 'Faculty', $email, 1)
                ON CONFLICT(Username) DO UPDATE SET
                    Role = 'Faculty',
                    Email = excluded.Email,
                    IsActive = 1;

                INSERT INTO Faculty
                    (FacultyId, UserId, FacultyCode, Name, Department, Designation, Email, MobileNumber,
                     AssignedClasses, OfficeRoomNumber, OfficeRoom, Experience, Qualification, Mobile, ProfileImagePath, Status)
                SELECT $facultyId, UserId, $code, $name, $department, $designation, $email, $mobile,
                       'Assigned by admin module', $room, $room, $experience, $qualification, $mobile, '', 'Active'
                FROM Users WHERE Username = $email
                ON CONFLICT(FacultyId) DO UPDATE SET
                    UserId = excluded.UserId,
                    FacultyCode = excluded.FacultyCode,
                    Name = excluded.Name,
                    Department = excluded.Department,
                    Designation = excluded.Designation,
                    Email = excluded.Email,
                    MobileNumber = excluded.MobileNumber,
                    OfficeRoomNumber = excluded.OfficeRoomNumber,
                    OfficeRoom = excluded.OfficeRoom,
                    Experience = excluded.Experience,
                    Qualification = excluded.Qualification,
                    Mobile = excluded.Mobile,
                    Status = excluded.Status;
                """,
                ("$facultyId", faculty.Item1),
                ("$code", faculty.Item2),
                ("$name", faculty.Item3),
                ("$department", faculty.Item4),
                ("$designation", faculty.Item5),
                ("$email", faculty.Item6),
                ("$mobile", faculty.Item7),
                ("$room", faculty.Item8),
                ("$experience", faculty.Item9),
                ("$qualification", faculty.Item10),
                ("$password", HashPassword("1234")));
        }

        Execute(connection, """
            INSERT OR IGNORE INTO ActivityLogs (ActorRole, ActorName, Action, EntityName, CreatedAt) VALUES
                ('Admin', 'admin1', 'New student added', 'Students', datetime('now', '-5 hours')),
                ('Admin', 'admin1', 'Faculty profile updated', 'Faculty', datetime('now', '-4 hours')),
                ('Faculty', 'Dr. Meera Sharma', 'Evidence approved', 'LearningEvidence', datetime('now', '-3 hours')),
                ('Faculty', 'Dr. Meera Sharma', 'Marks updated', 'Marks', datetime('now', '-2 hours')),
                ('Faculty', 'Dr. Meera Sharma', 'Attendance updated', 'Attendance', datetime('now', '-90 minutes')),
                ('Admin', 'admin1', 'Event created', 'CampusEvents', datetime('now', '-30 minutes'));

            INSERT OR IGNORE INTO Reports (ReportName, ReportType, GeneratedBy, GeneratedAt, FilePath) VALUES
                ('Student Progress Snapshot', 'Student progress report', 'admin1', datetime('now', '-2 days'), ''),
                ('Faculty Workload Snapshot', 'Faculty workload report', 'admin1', datetime('now', '-1 day'), ''),
                ('Attendance Shortage Snapshot', 'Attendance report', 'admin1', datetime('now'), '');
            """);
    }

    private static List<(int SectionId, int SubjectId, string SubjectCode, int Semester)> GetAssignedSections(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT fs.SectionId, fs.SubjectId, s.SubjectCode, sec.Semester
            FROM FacultySections fs
            JOIN Subjects s ON s.SubjectId = fs.SubjectId
            JOIN Sections sec ON sec.SectionId = fs.SectionId
            WHERE fs.FacultyId = 1;
            """;
        using var reader = command.ExecuteReader();
        var rows = new List<(int, int, string, int)>();
        while (reader.Read()) rows.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3)));
        return rows;
    }

    private static List<int> StudentIdsForSection(SqliteConnection connection, int sectionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT StudentId FROM Students WHERE SectionId = $sectionId ORDER BY StudentId;";
        command.Parameters.AddWithValue("$sectionId", sectionId);
        using var reader = command.ExecuteReader();
        var ids = new List<int>();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        return ids;
    }

    private static bool FacultySampleDataReady(SqliteConnection connection)
    {
        return ScalarInt(connection, "SELECT COUNT(*) FROM Students;") >= 275
            && ScalarInt(connection, "SELECT COUNT(*) FROM Sections WHERE SectionId BETWEEN 1 AND 5 AND TotalStudents = 55;") >= 5
            && ScalarInt(connection, "SELECT COUNT(*) FROM FacultySections WHERE FacultyId = 1;") >= 5
            && ScalarInt(connection, "SELECT COUNT(*) FROM FacultyAttendanceDaily WHERE FacultyId = 1;") >= 275
            && ScalarInt(connection, "SELECT COUNT(*) FROM FacultyMarksRecords WHERE FacultyId = 1;") >= 825
            && ScalarInt(connection, "SELECT COUNT(*) FROM LearningEvidence WHERE FacultyId = 1;") >= 40
            && ScalarInt(connection, "SELECT COUNT(*) FROM Notes WHERE FacultyId = 1;") >= 3;
    }

    private static bool AdminSampleDataReady(SqliteConnection connection)
    {
        return ScalarInt(connection, "SELECT COUNT(*) FROM Departments;") >= 6
            && ScalarInt(connection, "SELECT COUNT(*) FROM Faculty;") >= 10
            && ScalarInt(connection, "SELECT COUNT(*) FROM Subjects;") >= 20
            && ScalarInt(connection, "SELECT COUNT(*) FROM Users WHERE Role = 'Admin';") >= 1
            && ScalarInt(connection, "SELECT COUNT(*) FROM ActivityLogs;") >= 6
            && ScalarInt(connection, "SELECT COUNT(*) FROM Reports;") >= 3;
    }

    private static int ScalarInt(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar() ?? 0);
    }

    private static void EnsureFacultyColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "Faculty", name, definition);
    private static void EnsureUserColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "Users", name, definition);
    private static void EnsureStudentColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "Students", name, definition);
    private static void EnsureSectionColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "Sections", name, definition);
    private static void EnsureSubjectColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "Subjects", name, definition);
    private static void EnsureAttendanceDailyColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "FacultyAttendanceDaily", name, definition);
    private static void EnsureMarksRecordColumn(SqliteConnection connection, string name, string definition) => AddColumnIfMissing(connection, "FacultyMarksRecords", name, definition);

    private static void EnsureOptionalTableColumn(SqliteConnection connection, string table, string name, string definition)
    {
        if (!TableExists(connection, table)) return;
        AddColumnIfMissing(connection, table, name, definition);
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $table;";
        command.Parameters.AddWithValue("$table", table);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) > 0;
    }

    private static void EnsureFacultyIndexes(SqliteConnection connection)
    {
        Execute(connection, """
            UPDATE FacultyAttendanceDaily
            SET SubjectCode = COALESCE(NULLIF(SubjectCode, ''), (
                    SELECT SubjectCode FROM Subjects WHERE SubjectId = FacultyAttendanceDaily.SubjectId
                ), 'UNKNOWN'),
                Semester = COALESCE(NULLIF(Semester, 0), (
                    SELECT Semester FROM Sections WHERE SectionId = FacultyAttendanceDaily.SectionId
                ), 4);

            UPDATE FacultyMarksRecords
            SET SubjectCode = COALESCE(NULLIF(SubjectCode, ''), (
                    SELECT SubjectCode FROM Subjects WHERE SubjectId = FacultyMarksRecords.SubjectId
                ), 'UNKNOWN'),
                Semester = COALESCE(NULLIF(Semester, 0), (
                    SELECT Semester FROM Sections WHERE SectionId = FacultyMarksRecords.SectionId
                ), 4),
                Remarks = COALESCE(Remarks, '');

            DELETE FROM FacultyAttendanceDaily
            WHERE rowid NOT IN (
                SELECT MAX(rowid)
                FROM FacultyAttendanceDaily
                GROUP BY StudentId, SubjectId, SectionId, AttendanceDate
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_FacultyAttendanceDaily_Save
                ON FacultyAttendanceDaily(StudentId, SubjectCode, AttendanceDate);

            CREATE UNIQUE INDEX IF NOT EXISTS IX_FacultyAttendanceDaily_StudentSubjectSectionDate
                ON FacultyAttendanceDaily(StudentId, SubjectId, SectionId, AttendanceDate);

            CREATE UNIQUE INDEX IF NOT EXISTS IX_FacultyMarksRecords_Save
                ON FacultyMarksRecords(StudentId, SubjectCode, Semester, ExamType);
            """);
    }

    private static void AddColumnIfMissing(SqliteConnection connection, string table, string column, string definition)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase)) return;
        }
        reader.Close();
        Execute(connection, $"ALTER TABLE {table} ADD COLUMN {column} {definition};");
    }

    private static void Execute(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        command.ExecuteNonQuery();
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

    private static string EnsurePlaceholderImage(string fileName, string initials, Color color)
    {
        var directory = Path.Combine(StudentDatabase.DataDirectory, "ProfileImages");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        if (File.Exists(path)) return path;

        var tempPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}.jpg");
        try
        {
            using var bitmap = new Bitmap(240, 240);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(color);
            using var font = new Font("Segoe UI", 52, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(initials, font, brush, new RectangleF(0, 0, 240, 240), format);
            bitmap.Save(tempPath, ImageFormat.Jpeg);
            if (!File.Exists(path)) File.Move(tempPath, path);
        }
        catch
        {
            // A placeholder is a convenience for sample data; startup should not fail if GDI+ is busy.
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
        return path;
    }
}
