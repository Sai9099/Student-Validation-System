namespace StudentValidationSystem;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        InstallExceptionLogging();

        try
        {
            ApplicationConfiguration.Initialize();
            Run(args);
        }
        catch (Exception ex)
        {
            WriteExceptionLog("app_startup_exception.txt", ex);
            MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Run(string[] args)
    {
        var database = new Database.StudentDatabase();
        var authDb = new Database.DBHelper();
        using (var initMutex = new Mutex(false, "StudentValidationSystem.DatabaseInitialize"))
        {
            if (!initMutex.WaitOne(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException("Database initialization is already running in another app instance. Please try again in a few seconds.");
            }

            try
            {
                database.Initialize();
                authDb.Initialize();
            }
            finally
            {
                initMutex.ReleaseMutex();
            }
        }
        if (args.Contains("--init-db-check"))
        {
            var count = database.Scalar("SELECT COUNT(*) FROM StudentProfile;");
            Console.WriteLine($"Student module database initialized. Student records: {count}");
            return;
        }

        if (args.Contains("--reset-sample-login"))
        {
            database.Execute("""
                UPDATE StudentUsers
                SET PasswordHash = $passwordHash, IsActive = 1
                WHERE StudentId = 1;
                """, ("$passwordHash", Database.StudentDatabase.HashPassword("student123")));

            var login = database.GetDataTable("""
                SELECT sp.RegisterNumber, sp.Email, sp.FullName
                FROM StudentProfile sp
                JOIN StudentUsers su ON su.StudentId = sp.StudentId
                WHERE sp.StudentId = 1;
                """);

            if (login.Rows.Count == 0)
            {
                Console.WriteLine("Sample student account was not found.");
                return;
            }

            var row = login.Rows[0];
            Console.WriteLine("Sample student login reset.");
            Console.WriteLine($"Register number: {row["RegisterNumber"]}");
            Console.WriteLine($"Email: {row["Email"]}");
            Console.WriteLine("Password: student123");
            return;
        }

        if (args.Contains("--login-check"))
        {
            var loginArg = GetArgValue(args, "--login") ?? "RA2311033010096";
            var passwordArg = GetArgValue(args, "--password") ?? "student123";
            var user = database.Authenticate(loginArg, passwordArg);
            Console.WriteLine(user == null
                ? "Login check failed."
                : $"Login check succeeded for {user.FullName} ({user.RegisterNumber}).");
            return;
        }

        if (args.Contains("--faculty-login-check"))
        {
            var loginArg = GetArgValue(args, "--login") ?? "faculty1@college.edu";
            var passwordArg = GetArgValue(args, "--password") ?? "1234";
            var result = new Services.AuthService(authDb).LoginFaculty(loginArg, passwordArg);
            Console.WriteLine(result.Success
                ? $"Faculty login succeeded for {result.FacultyName} ({result.Department})."
                : $"Faculty login failed: {result.Message}");
            return;
        }

        if (args.Contains("--admin-login-check"))
        {
            var loginArg = GetArgValue(args, "--login") ?? "admin1";
            var passwordArg = GetArgValue(args, "--password") ?? "admin123";
            var result = new Services.AuthService(authDb).LoginAdmin(loginArg, passwordArg);
            Console.WriteLine(result.Success
                ? $"Admin login succeeded for {result.User?.Username}."
                : $"Admin login failed: {result.Message}");
            return;
        }

        if (args.Contains("--faculty-dashboard-check"))
        {
            Console.WriteLine($"Faculty rows: {authDb.Scalar("SELECT COUNT(*) FROM Faculty;")}");
            Console.WriteLine($"Assigned students: {authDb.Scalar("SELECT COUNT(*) FROM Students;")}");
            Console.WriteLine($"Faculty subjects: {authDb.Scalar("SELECT COUNT(*) FROM FacultySubjects WHERE FacultyId = 1;")}");
            Console.WriteLine($"Daily attendance rows: {authDb.Scalar("SELECT COUNT(*) FROM FacultyAttendanceDaily WHERE FacultyId = 1;")}");
            Console.WriteLine($"Marks rows: {authDb.Scalar("SELECT COUNT(*) FROM FacultyMarksRecords WHERE FacultyId = 1;")}");
            Console.WriteLine($"Events: {authDb.Scalar("SELECT COUNT(*) FROM CampusEvents;")}");
            return;
        }

        if (args.Contains("--faculty-section-counts"))
        {
            var rows = authDb.GetDataTable("""
                SELECT sec.SectionName, sec.TotalStudents AS DeclaredStudents, COUNT(st.StudentId) AS ActualStudents
                FROM Sections sec
                LEFT JOIN Students st ON st.SectionId = sec.SectionId
                WHERE sec.SectionId IN (SELECT SectionId FROM FacultySections WHERE FacultyId = 1)
                GROUP BY sec.SectionId, sec.SectionName, sec.TotalStudents
                ORDER BY sec.SectionId;
                """);

            foreach (System.Data.DataRow row in rows.Rows)
            {
                Console.WriteLine($"{row["SectionName"]}: declared {row["DeclaredStudents"]}, actual {row["ActualStudents"]}");
            }
            return;
        }

        if (args.Contains("--faculty-ui-check"))
        {
            Helpers.FacultySessionManager.Current = new Models.FacultySession
            {
                FacultyId = 1,
                Username = "faculty1",
                Name = "Dr. Meera Sharma",
                Department = "Computer Science and Engineering",
                Email = "faculty1@college.edu",
                MobileNumber = "9876501111"
            };

            using var loginSelection = new Views.LoginSelectionForm();
            using var facultyDashboard = new Views.FacultyDashboardForm();
            var loadPage = typeof(Views.FacultyDashboardForm).GetMethod("LoadPage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingMethodException("FacultyDashboardForm.LoadPage was not found.");
            foreach (var page in new[]
            {
                "Dashboard", "Profile", "My Sections", "Student Management", "Attendance", "Marks",
                "Evidence Validation", "Notes Management", "Campus Events"
            })
            {
                loadPage.Invoke(facultyDashboard, [page]);
                Console.WriteLine($"Faculty page loaded: {page}");
            }
            Console.WriteLine("Faculty UI smoke check succeeded.");
            return;
        }

        if (args.Contains("--admin-ui-check"))
        {
            Helpers.AdminSessionManager.Current = new Models.User
            {
                UserId = 1,
                Username = "admin1",
                Role = "Admin"
            };

            using var adminDashboard = new Views.AdminDashboardForm();
            var loadPage = typeof(Views.AdminDashboardForm).GetMethod("LoadPage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new MissingMethodException("AdminDashboardForm.LoadPage was not found.");
            foreach (var page in new[]
            {
                "Dashboard", "Students", "Faculty", "Departments", "Sections", "Subjects", "Attendance",
                "Marks", "Evidence", "Notes", "Campus Events", "Users", "Reports", "Backup & Restore"
            })
            {
                loadPage.Invoke(adminDashboard, [page]);
                Console.WriteLine($"Admin page loaded: {page}");
            }
            Console.WriteLine("Admin UI smoke check succeeded.");
            return;
        }

        if (args.Contains("--subject-counts"))
        {
            var counts = database.GetDataTable("""
                SELECT Semester, COUNT(*) AS SubjectCount, SUM(Credits) AS TotalCredits
                FROM SemesterSubjects ss
                JOIN Subjects s ON s.SubjectId = ss.SubjectId
                GROUP BY Semester
                ORDER BY Semester;
                """);

            foreach (System.Data.DataRow row in counts.Rows)
            {
                Console.WriteLine($"Semester {row["Semester"]}: {row["SubjectCount"]} subjects, {row["TotalCredits"]} credits");
            }
            return;
        }

        if (args.Contains("--attendance-counts"))
        {
            var counts = database.GetDataTable("""
                SELECT ss.Semester,
                       COUNT(DISTINCT ss.SubjectId) AS SubjectCount,
                       COUNT(a.AttendanceId) AS AttendanceRows,
                       ROUND(SUM(a.PresentCount) * 100.0 / NULLIF(SUM(a.TotalClassesConducted), 0), 2) AS OverallAttendance
                FROM SemesterSubjects ss
                LEFT JOIN Attendance a ON a.SubjectId = ss.SubjectId
                    AND a.Semester = ss.Semester
                    AND a.StudentId = 1
                GROUP BY ss.Semester
                ORDER BY ss.Semester;
                """);

            foreach (System.Data.DataRow row in counts.Rows)
            {
                Console.WriteLine($"Semester {row["Semester"]}: {row["SubjectCount"]} subjects, {row["AttendanceRows"]} attendance rows, {row["OverallAttendance"]}% overall");
            }
            return;
        }

        if (args.Contains("--current-view-check"))
        {
            var currentSemester = Convert.ToInt32(database.Scalar("SELECT CurrentSemester FROM AcademicDetails WHERE StudentId = 1;"));
            var subjects = Convert.ToInt32(database.Scalar(
                "SELECT COUNT(*) FROM SemesterSubjects WHERE Semester = $semester;",
                ("$semester", currentSemester)));
            var attendance = Convert.ToInt32(database.Scalar(
                "SELECT COUNT(*) FROM Attendance WHERE StudentId = 1 AND Semester = $semester;",
                ("$semester", currentSemester)));
            var marks = Convert.ToInt32(database.Scalar(
                "SELECT COUNT(*) FROM InternalMarks WHERE StudentId = 1 AND Semester = $semester;",
                ("$semester", currentSemester)));
            var overall = Convert.ToDouble(database.Scalar("""
                SELECT IFNULL(ROUND(SUM(PresentCount) * 100.0 / NULLIF(SUM(TotalClassesConducted), 0), 2), 0)
                FROM Attendance WHERE StudentId = 1 AND Semester = $semester;
                """, ("$semester", currentSemester)));

            Console.WriteLine($"Profile current semester: {currentSemester}");
            Console.WriteLine($"Subjects visible in Current view: {subjects}");
            Console.WriteLine($"Attendance rows visible in Current view: {attendance}");
            Console.WriteLine($"Marks rows visible in Current view: {marks}");
            Console.WriteLine($"Overall attendance for semester {currentSemester}: {overall:0.00}%");
            return;
        }

        if (args.Contains("--semester-view-check"))
        {
            var rows = database.GetDataTable("""
                SELECT ss.Semester,
                       COUNT(DISTINCT ss.SubjectId) AS SubjectRows,
                       COUNT(a.AttendanceId) AS AttendanceRows
                FROM SemesterSubjects ss
                LEFT JOIN Attendance a ON a.StudentId = 1
                    AND a.Semester = ss.Semester
                    AND a.SubjectId = ss.SubjectId
                GROUP BY ss.Semester
                ORDER BY ss.Semester;
                """);

            foreach (System.Data.DataRow row in rows.Rows)
            {
                Console.WriteLine($"Semester {row["Semester"]}: subjects page {row["SubjectRows"]}, attendance page {row["AttendanceRows"]}");
            }
            return;
        }

        if (args.Contains("--marks-counts"))
        {
            var rows = database.GetDataTable("""
                SELECT ss.Semester,
                       COUNT(DISTINCT ss.SubjectId) AS SubjectRows,
                       COUNT(im.InternalMarkId) AS MarksRows,
                       ROUND(AVG(im.TotalMarks), 2) AS AverageMarks
                FROM SemesterSubjects ss
                LEFT JOIN InternalMarks im ON im.StudentId = 1
                    AND im.Semester = ss.Semester
                    AND im.SubjectId = ss.SubjectId
                GROUP BY ss.Semester
                ORDER BY ss.Semester;
                """);

            foreach (System.Data.DataRow row in rows.Rows)
            {
                Console.WriteLine($"Semester {row["Semester"]}: subjects page {row["SubjectRows"]}, marks page {row["MarksRows"]}, average {row["AverageMarks"]}/100");
            }
            return;
        }

        if (args.Contains("--notes-check"))
        {
            var rows = database.GetDataTable("""
                SELECT UploadedByRole, ApprovalStatus, COUNT(*) AS Count
                FROM Notes
                GROUP BY UploadedByRole, ApprovalStatus
                ORDER BY UploadedByRole, ApprovalStatus;
                """);

            foreach (System.Data.DataRow row in rows.Rows)
            {
                Console.WriteLine($"{row["UploadedByRole"]} {row["ApprovalStatus"]}: {row["Count"]}");
            }
            return;
        }

        if (args.Contains("--evidence-check"))
        {
            var rows = database.GetDataTable("""
                SELECT Status, COUNT(*) AS Count
                FROM LearningEvidence
                WHERE StudentId = 1
                GROUP BY Status
                ORDER BY Status;
                """);

            foreach (System.Data.DataRow row in rows.Rows)
            {
                Console.WriteLine($"{row["Status"]}: {row["Count"]}");
            }

            var advancedColumns = database.Scalar("""
                SELECT COUNT(*) FROM pragma_table_info('LearningEvidence')
                WHERE name IN ('Title', 'SubjectId', 'AcademicYear', 'LearningOutcome', 'SkillsGained', 'FileType',
                               'StartDate', 'EndDate', 'IssuedOrganization', 'CertificateId', 'VerificationLink',
                               'Status', 'Visibility', 'ResubmissionCount', 'LastUpdatedDate');
                """);
            Console.WriteLine($"Advanced evidence columns present: {advancedColumns}/15");
            return;
        }

        Application.Run(new Views.LoginSelectionForm());
    }

    private static void InstallExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            WriteExceptionLog("app_unhandled_exception.txt", e.ExceptionObject);
        };

        Application.ThreadException += (_, e) =>
        {
            WriteExceptionLog("app_thread_exception.txt", e.Exception);
            MessageBox.Show(e.Exception.ToString(), "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
    }

    private static void WriteExceptionLog(string fileName, object? exception)
    {
        try
        {
            var text = exception?.ToString() ?? "Unknown exception";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, fileName), text);
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, fileName), text);
        }
        catch { }
    }

    private static string? GetArgValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
