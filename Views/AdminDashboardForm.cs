using System.Data;
using System.Text;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class AdminDashboardForm : Form
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly DBHelper _db = new();
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private string _currentPage = "Dashboard";

    public AdminDashboardForm()
    {
        Text = "Admin Dashboard - Student Learning Evidence & Progress Validation System";
        Size = new Size(1360, 820);
        MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.BodyFont;
        _db.Initialize();
        BuildShell();
        LoadPage("Dashboard");
    }

    private void BuildShell()
    {
        Controls.Clear();
        _navButtons.Clear();
        BackColor = _theme.Background;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Sidebar, Padding = new Padding(18), AutoScroll = true };
        root.Controls.Add(sidebar, 0, 0);
        sidebar.Controls.Add(new Label
        {
            Text = "Admin\nModule",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(18, 24),
            Size = new Size(200, 72)
        });

        var y = 118;
        foreach (var item in new[]
        {
            "Dashboard", "Students", "Faculty", "Departments", "Sections", "Subjects", "Attendance",
            "Marks", "Evidence", "Notes", "Campus Events", "Users", "Reports", "Backup & Restore", "Logout"
        })
        {
            var button = new Button
            {
                Text = item,
                Location = new Point(18, y),
                Size = new Size(192, 36),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(226, 236, 245),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) =>
            {
                if (item == "Logout") Close();
                else LoadPage(item);
            };
            sidebar.Controls.Add(button);
            _navButtons[item] = button;
            y += 43;
        }

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = _theme.Background };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(workspace, 1, 0);

        var top = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface };
        workspace.Controls.Add(top, 0, 0);

        _title.Font = Ui.TitleFont;
        _title.ForeColor = _theme.Text;
        _title.BackColor = Color.Transparent;
        _title.AutoSize = true;
        _title.Location = new Point(28, 20);
        top.Controls.Add(_title);

        var welcome = Ui.Label($"Welcome, {AdminSessionManager.Current?.Username ?? "admin1"}", Ui.BodyFont, _theme.MutedText);
        welcome.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        welcome.Location = new Point(Width - 610, 28);
        welcome.Size = new Size(330, 28);
        welcome.TextAlign = ContentAlignment.MiddleRight;
        top.Controls.Add(welcome);

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(22);
        _content.BackColor = _theme.Background;
        workspace.Controls.Add(_content, 0, 1);
    }

    private void LoadPage(string page)
    {
        _currentPage = page;
        _title.Text = page;
        foreach (var pair in _navButtons)
        {
            pair.Value.BackColor = pair.Key == page ? Color.FromArgb(48, 91, 124) : Color.Transparent;
        }

        _content.Controls.Clear();
        _content.Controls.Add(page switch
        {
            "Dashboard" => BuildDashboard(),
            "Students" => BuildStudentsPage(),
            "Faculty" => BuildFacultyPage(),
            "Departments" => BuildDepartmentsPage(),
            "Sections" => BuildSectionsPage(),
            "Subjects" => BuildSubjectsPage(),
            "Attendance" => BuildAttendancePage(),
            "Marks" => BuildMarksPage(),
            "Evidence" => BuildEvidencePage(),
            "Notes" => BuildNotesPage(),
            "Campus Events" => BuildEventsPage(),
            "Users" => BuildUsersPage(),
            "Reports" => BuildReportsPage(),
            "Backup & Restore" => BuildBackupPage(),
            _ => BuildDashboard()
        });
    }

    private Panel Page(string title)
    {
        return new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _theme.Background };
    }

    private Control BuildDashboard()
    {
        var panel = Page("Admin Dashboard");
        var dashboard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _theme.Background
        };
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(dashboard);

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 150,
            WrapContents = true,
            BackColor = _theme.Background,
            Margin = new Padding(0, 0, 0, 16)
        };
        dashboard.Controls.Add(cards, 0, 0);
        cards.Controls.Add(Metric("Total Students", Count("SELECT COUNT(*) FROM Students;"), _theme.Primary));
        cards.Controls.Add(Metric("Total Faculty", Count("SELECT COUNT(*) FROM Faculty;"), _theme.Success));
        cards.Controls.Add(Metric("Total Departments", Count("SELECT COUNT(*) FROM Departments;"), _theme.Warning));
        cards.Controls.Add(Metric("Total Sections", Count("SELECT COUNT(*) FROM Sections;"), _theme.Primary));
        cards.Controls.Add(Metric("Total Subjects", Count("SELECT COUNT(*) FROM Subjects;"), _theme.Success));
        cards.Controls.Add(Metric("Pending Evidence", Count("SELECT COUNT(*) FROM LearningEvidence WHERE Status IN ('Pending','Submitted','Resubmitted');"), _theme.Warning));
        cards.Controls.Add(Metric("Upcoming Events", Count("SELECT COUNT(*) FROM CampusEvents WHERE EventDate >= date('now');"), _theme.Primary));
        cards.Controls.Add(Metric("Total Notes Uploaded", Count("SELECT COUNT(*) FROM Notes;"), _theme.Success));
        cards.Controls.Add(Metric("Attendance Shortage Students", Count("SELECT COUNT(*) FROM Students WHERE AttendancePercentage < 75;"), _theme.Danger));
        cards.Controls.Add(Metric("Active Users", Count("SELECT COUNT(*) FROM Users WHERE COALESCE(IsActive, 1) = 1;"), _theme.Primary));

        var topRow = DashboardGridRow();
        dashboard.Controls.Add(topRow, 0, 1);
        topRow.Controls.Add(DashboardGridCard("Recent Activity", """
            SELECT ActorRole, ActorName, Action, EntityName, CreatedAt AS Date
            FROM ActivityLogs
            ORDER BY datetime(CreatedAt) DESC
            LIMIT 12;
            """, 260), 0, 0);
        topRow.Controls.Add(DashboardGridCard("Pending Evidence", """
            SELECT le.EvidenceId, st.RegisterNumber, st.Name AS Student, le.Title, le.Category, le.Status, le.UploadDate
            FROM LearningEvidence le
            LEFT JOIN Students st ON st.StudentId = le.StudentId
            WHERE le.Status IN ('Pending','Submitted','Resubmitted')
            ORDER BY le.UploadDate DESC
            LIMIT 12;
            """, 260), 1, 0);

        var bottomRow = DashboardGridRow();
        dashboard.Controls.Add(bottomRow, 0, 2);
        bottomRow.Controls.Add(DashboardGridCard("Attendance Shortage", """
            SELECT RegisterNumber, Name, Section, AttendancePercentage AS 'Attendance %'
            FROM Students
            WHERE AttendancePercentage < 75
            ORDER BY AttendancePercentage;
            """, 240), 0, 0);
        bottomRow.Controls.Add(DashboardGridCard("Upcoming Events", """
            SELECT EventTitle AS Event, EventDate AS Date, Venue, ConductedBy
            FROM CampusEvents
            WHERE EventDate >= date('now')
            ORDER BY EventDate
            LIMIT 12;
            """, 240), 1, 0);
        return panel;
    }

    private Control BuildStudentsPage() => BuildDataPage(new AdminPageSpec(
        "Student Management",
        """
        SELECT st.StudentId, st.RegisterNumber AS 'Register Number', st.Name, st.Department, st.Year, st.Semester,
               sec.SectionName AS Section, st.Email, st.Mobile, st.CGPA, COALESCE(st.Status, 'Active') AS Status
        FROM Students st
        LEFT JOIN Sections sec ON sec.SectionId = st.SectionId
        WHERE $search = '' OR lower(st.RegisterNumber || ' ' || st.Name || ' ' || st.Department || ' ' || sec.SectionName) LIKE '%' || lower($search) || '%'
        ORDER BY st.RegisterNumber;
        """,
        "StudentId",
        AddStudent,
        EditStudent,
        id => DeleteById("Students", "StudentId", id, "Student"),
        ViewStudentProfile,
        new[] { ("Update Image", (Action<int>)UpdateStudentImage), ("Attendance", (Action<int>)ViewStudentAttendance), ("Marks", (Action<int>)ViewStudentMarks), ("Evidence", (Action<int>)ViewStudentEvidence), ("Notes", (Action<int>)ViewStudentNotes), ("Report", (Action<int>)GenerateStudentReport) }));

    private Control BuildFacultyPage() => BuildDataPage(new AdminPageSpec(
        "Faculty Management",
        """
        SELECT f.FacultyId AS 'Faculty ID', f.Name, f.Department, f.Designation, f.Email,
               COALESCE(NULLIF(f.Mobile, ''), f.MobileNumber) AS Mobile,
               COUNT(DISTINCT fs.SubjectId) AS 'Subjects Handled',
               COUNT(DISTINCT fs.SectionId) AS 'Sections Assigned',
               COALESCE(f.Status, 'Active') AS Status
        FROM Faculty f
        LEFT JOIN FacultySections fs ON fs.FacultyId = f.FacultyId
        WHERE $search = '' OR lower(f.Name || ' ' || f.Department || ' ' || f.Email || ' ' || f.FacultyCode) LIKE '%' || lower($search) || '%'
        GROUP BY f.FacultyId
        ORDER BY f.FacultyId;
        """,
        "Faculty ID",
        AddFaculty,
        EditFaculty,
        id => DeleteById("Faculty", "FacultyId", id, "Faculty"),
        ViewFacultyProfile,
        new[] { ("Update Image", (Action<int>)UpdateFacultyImage), ("Assign Subjects", (Action<int>)AssignFacultySubject), ("Assign Sections", (Action<int>)AssignFacultySection), ("Handled Subjects", (Action<int>)ViewFacultySubjects), ("Faculty Students", (Action<int>)ViewFacultyStudents), ("Reset Password", (Action<int>)ResetFacultyPassword) }));

    private Control BuildDepartmentsPage() => BuildDataPage(new AdminPageSpec(
        "Department Management",
        """
        SELECT d.DepartmentId AS 'Department ID', d.DepartmentName AS 'Department Name', d.HodName AS 'HOD Name',
               COUNT(DISTINCT st.StudentId) AS 'Total Students',
               COUNT(DISTINCT f.FacultyId) AS 'Total Faculty',
               d.Status
        FROM Departments d
        LEFT JOIN Students st ON st.Department = d.DepartmentName
        LEFT JOIN Faculty f ON f.Department = d.DepartmentName
        WHERE $search = '' OR lower(d.DepartmentName || ' ' || d.HodName) LIKE '%' || lower($search) || '%'
        GROUP BY d.DepartmentId
        ORDER BY d.DepartmentName;
        """,
        "Department ID",
        AddDepartment,
        EditDepartment,
        id => DeleteById("Departments", "DepartmentId", id, "Department"),
        ViewDepartmentDetails));

    private Control BuildSectionsPage() => BuildDataPage(new AdminPageSpec(
        "Section Management",
        """
        SELECT sec.SectionId AS 'Section ID', sec.Department, sec.Year, sec.Semester, sec.SectionName AS 'Section Name',
               COUNT(st.StudentId) AS 'Total Students', COALESCE(sec.ClassInCharge, '') AS 'Class In-charge'
        FROM Sections sec
        LEFT JOIN Students st ON st.SectionId = sec.SectionId
        WHERE $search = '' OR lower(sec.SectionName || ' ' || sec.Department) LIKE '%' || lower($search) || '%'
        GROUP BY sec.SectionId
        ORDER BY sec.Semester, sec.SectionName;
        """,
        "Section ID",
        AddSection,
        EditSection,
        id => DeleteById("Sections", "SectionId", id, "Section"),
        ViewSectionDetails,
        new[] { ("Assign Students", (Action<int>)AssignStudentsToSection), ("Assign Faculty", (Action<int>)AssignFacultySection) }));

    private Control BuildSubjectsPage() => BuildDataPage(new AdminPageSpec(
        "Subject Management",
        """
        SELECT SubjectId, SubjectCode AS 'Subject Code', SubjectName AS 'Subject Name',
               Department, Semester, Credits, SubjectType AS 'Subject Type', COALESCE(FacultyAssigned, FacultyName) AS 'Faculty Assigned'
        FROM Subjects
        WHERE $search = '' OR lower(SubjectCode || ' ' || SubjectName || ' ' || Department || ' ' || FacultyName) LIKE '%' || lower($search) || '%'
        ORDER BY Semester, SubjectCode;
        """,
        "SubjectId",
        AddSubject,
        EditSubject,
        id => DeleteById("Subjects", "SubjectId", id, "Subject"),
        ViewSubjectDetails,
        new[] { ("Assign Faculty", (Action<int>)AssignSubjectToFaculty) }));

    private Control BuildAttendancePage() => BuildDataPage(new AdminPageSpec(
        "Attendance Control",
        """
        SELECT fad.AttendanceRecordId, st.RegisterNumber, st.Name AS Student, sec.Department, sec.SectionName AS Section,
               fad.Semester, sub.SubjectName AS Subject, fad.AttendanceDate AS Date, fad.Status
        FROM FacultyAttendanceDaily fad
        JOIN Students st ON st.StudentId = fad.StudentId
        JOIN Sections sec ON sec.SectionId = fad.SectionId
        JOIN Subjects sub ON sub.SubjectId = fad.SubjectId
        WHERE $search = '' OR lower(st.RegisterNumber || ' ' || st.Name || ' ' || sec.SectionName || ' ' || sub.SubjectName || ' ' || fad.Status) LIKE '%' || lower($search) || '%'
        ORDER BY fad.AttendanceDate DESC, sec.SectionName, st.RegisterNumber;
        """,
        "AttendanceRecordId",
        null,
        EditAttendance,
        null,
        ViewAttendanceRecord,
        new[]
        {
            ("Shortage List", (Action<int>)(_ => ShowTableDialog("Attendance Shortage List", "SELECT RegisterNumber, Name, Section, AttendancePercentage AS 'Attendance %' FROM Students WHERE AttendancePercentage < 75 ORDER BY AttendancePercentage;"))),
            ("Export Report", (Action<int>)(_ => ExportQuery("Attendance_Report", "SELECT * FROM FacultyAttendanceDaily;")))
        }));

    private Control BuildMarksPage() => BuildDataPage(new AdminPageSpec(
        "Marks Control",
        """
        SELECT fmr.FacultyMarkId, st.RegisterNumber, st.Name AS Student, sec.SectionName AS Section,
               fmr.Semester, sub.SubjectName AS Subject, fmr.ExamType, fmr.MaxMarks, fmr.MarksObtained, fmr.Grade, fmr.Remarks
        FROM FacultyMarksRecords fmr
        JOIN Students st ON st.StudentId = fmr.StudentId
        JOIN Sections sec ON sec.SectionId = fmr.SectionId
        JOIN Subjects sub ON sub.SubjectId = fmr.SubjectId
        WHERE $search = '' OR lower(st.RegisterNumber || ' ' || st.Name || ' ' || sec.SectionName || ' ' || sub.SubjectName || ' ' || fmr.ExamType) LIKE '%' || lower($search) || '%'
        ORDER BY fmr.Semester, sec.SectionName, st.RegisterNumber;
        """,
        "FacultyMarkId",
        null,
        EditMarks,
        null,
        ViewMarksRecord,
        new[]
        {
            ("Generate Report", (Action<int>)(_ => ExportQuery("Marks_Report", "SELECT * FROM FacultyMarksRecords;")))
        }));

    private Control BuildEvidencePage() => BuildDataPage(new AdminPageSpec(
        "Evidence Control",
        """
        SELECT le.EvidenceId, st.RegisterNumber, st.Name AS Student, le.Title, le.Category, le.Semester,
               sub.SubjectName AS Subject, COALESCE(f.Name, '') AS Faculty, le.UploadDate, le.Status
        FROM LearningEvidence le
        LEFT JOIN Students st ON st.StudentId = le.StudentId
        LEFT JOIN Subjects sub ON sub.SubjectId = le.SubjectId
        LEFT JOIN Faculty f ON f.FacultyId = le.FacultyId
        WHERE $search = '' OR lower(st.RegisterNumber || ' ' || st.Name || ' ' || le.Title || ' ' || le.Category || ' ' || le.Status || ' ' || COALESCE(f.Name, '')) LIKE '%' || lower($search) || '%'
        ORDER BY le.UploadDate DESC;
        """,
        "EvidenceId",
        null,
        OverrideEvidenceStatus,
        null,
        ViewEvidence,
        new[]
        {
            ("Approve", (Action<int>)(id => SetEvidenceStatus(id, "Approved"))),
            ("Reject", (Action<int>)(id => SetEvidenceStatus(id, "Rejected"))),
            ("Report", (Action<int>)(_ => ExportQuery("Evidence_Report", "SELECT * FROM LearningEvidence;")))
        }));

    private Control BuildNotesPage() => BuildDataPage(new AdminPageSpec(
        "Notes Control",
        """
        SELECT n.NoteId, n.Title, n.UploadedByRole AS 'Uploader Role', sub.SubjectName AS Subject,
               n.Semester, n.UploadDate, n.ApprovalStatus, n.ReportStatus
        FROM Notes n
        LEFT JOIN Subjects sub ON sub.SubjectId = n.SubjectId
        WHERE $search = '' OR lower(n.Title || ' ' || n.UploadedByRole || ' ' || n.ApprovalStatus || ' ' || COALESCE(sub.SubjectName, '')) LIKE '%' || lower($search) || '%'
        ORDER BY n.UploadDate DESC;
        """,
        "NoteId",
        null,
        EditNoteStatus,
        id => DeleteById("Notes", "NoteId", id, "Note"),
        ViewNote,
        new[] { ("Approve", (Action<int>)(id => SetNoteStatus(id, "Approved"))), ("Reject", (Action<int>)(id => SetNoteStatus(id, "Rejected"))) }));

    private Control BuildEventsPage() => BuildDataPage(new AdminPageSpec(
        "Campus Events Control",
        """
        SELECT EventId, EventTitle AS 'Event Name', EventDate AS Date, Venue, ConductedBy AS Coordinator,
               EventType AS Type, RegistrationDeadline AS Deadline
        FROM CampusEvents
        WHERE $search = '' OR lower(EventTitle || ' ' || Venue || ' ' || ConductedBy || ' ' || EventType) LIKE '%' || lower($search) || '%'
        ORDER BY EventDate DESC;
        """,
        "EventId",
        AddEvent,
        EditEvent,
        id => DeleteById("CampusEvents", "EventId", id, "Event"),
        ViewEvent,
        new[] { ("Registered Students", (Action<int>)ViewEventRegistrations) }));

    private Control BuildUsersPage() => BuildDataPage(new AdminPageSpec(
        "User Account Management",
        """
        SELECT UserId, Username, COALESCE(Email, '') AS Email, Role,
               CASE WHEN COALESCE(IsActive, 1) = 1 THEN 'Active' ELSE 'Inactive' END AS Status,
               COALESCE(LastLogin, '') AS 'Last Login'
        FROM Users
        WHERE $search = '' OR lower(Username || ' ' || COALESCE(Email, '') || ' ' || Role) LIKE '%' || lower($search) || '%'
        ORDER BY Role, Username;
        """,
        "UserId",
        AddUser,
        EditUser,
        id => DeleteById("Users", "UserId", id, "User"),
        ViewUser,
        new[] { ("Reset Password", (Action<int>)ResetUserPassword), ("Activate", (Action<int>)(id => SetUserActive(id, true))), ("Deactivate", (Action<int>)(id => SetUserActive(id, false))) }));

    private Control BuildReportsPage() => BuildDataPage(new AdminPageSpec(
        "Reports Module",
        """
        SELECT ReportId, ReportName AS 'Report Name', ReportType AS Type, GeneratedBy AS 'Generated By',
               GeneratedAt AS 'Generated At', FilePath AS File
        FROM Reports
        WHERE $search = '' OR lower(ReportName || ' ' || ReportType || ' ' || GeneratedBy) LIKE '%' || lower($search) || '%'
        ORDER BY GeneratedAt DESC;
        """,
        "ReportId",
        AddReport,
        null,
        id => DeleteById("Reports", "ReportId", id, "Report"),
        ViewReport,
        new[]
        {
            ("Student Progress", (Action<int>)(_ => ExportQuery("Student_Progress_Report", "SELECT * FROM Students;"))),
            ("Faculty Workload", (Action<int>)(_ => ExportQuery("Faculty_Workload_Report", "SELECT * FROM Faculty;"))),
            ("Department Academic", (Action<int>)(_ => ExportQuery("Department_Academic_Report", "SELECT * FROM Departments;")))
        }));

    private Control BuildBackupPage()
    {
        var panel = Page("Backup & Restore");
        var card = Ui.Card(_theme, 16);
        card.Location = new Point(0, 0);
        card.Size = new Size(1080, 180);
        card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(card);
        card.Controls.Add(Label("SQLite Database Tools", 14, 12, 400, Ui.HeadingFont));
        AddButton(card, "Backup Database", 14, 58, () => BackupDatabase());
        AddButton(card, "Restore Database", 175, 58, () => RestoreDatabase());
        AddButton(card, "Export Students CSV", 336, 58, () => ExportQuery("Students_Export", "SELECT * FROM Students;"));
        AddButton(card, "Export Faculty CSV", 520, 58, () => ExportQuery("Faculty_Export", "SELECT * FROM Faculty;"));
        AddButton(card, "Export All Reports", 710, 58, () => ExportQuery("All_Reports", "SELECT * FROM Reports;"));
        AddButton(card, "Export Events CSV", 890, 58, () => ExportQuery("Events_Export", "SELECT * FROM CampusEvents;"));

        panel.Controls.Add(GridCard("Recent Generated Reports", """
            SELECT ReportName, ReportType, GeneratedBy, GeneratedAt, FilePath FROM Reports ORDER BY GeneratedAt DESC LIMIT 20;
            """, 0, 210, 1080, 420));
        return panel;
    }

    private Control BuildDataPage(AdminPageSpec spec)
    {
        var panel = Page(spec.Title);
        var search = new TextBox { PlaceholderText = "Search records", Location = new Point(0, 0), Width = 330 };
        panel.Controls.Add(search);
        var grid = Grid(0, 46, 1080, 480);
        grid.AllowUserToResizeColumns = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        panel.Controls.Add(grid);

        void Load()
        {
            grid.DataSource = _db.GetDataTable(spec.Sql, ("$search", search.Text.Trim()));
            if (grid.Columns.Contains(spec.IdColumn)) grid.Columns[spec.IdColumn].Visible = spec.IdColumn.EndsWith("ID", StringComparison.OrdinalIgnoreCase);
            // Auto-fit columns to content
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        search.TextChanged += (_, _) => Load();
        Load();

        var x = 0;
        if (spec.AddAction != null) x = AddPageButton(panel, "Add", x, 550, () => { spec.AddAction(); Load(); });
        if (spec.EditAction != null) x = AddPageButton(panel, "Edit", x, 550, () => { if (TrySelectedId(grid, spec.IdColumn, out var id)) { spec.EditAction(id); Load(); } });
        if (spec.DeleteAction != null) x = AddPageButton(panel, "Delete", x, 550, () => { if (TrySelectedId(grid, spec.IdColumn, out var id)) { spec.DeleteAction(id); Load(); } });
        x = AddPageButton(panel, "View", x, 550, () => { if (TrySelectedId(grid, spec.IdColumn, out var id)) spec.ViewAction(id); });
        foreach (var (text, action) in spec.ExtraActions)
        {
            x = AddPageButton(panel, text, x, 550, () => { if (TrySelectedId(grid, spec.IdColumn, out var id)) { action(id); Load(); } });
        }

        return panel;
    }

    private void AddStudent()
    {
        var fields = Prompt("Add Student", ("Register Number", ""), ("Name", ""), ("Department", "Computer Science and Engineering"), ("Year", "2"), ("Semester", "4"), ("Section ID", "1"), ("Email", ""), ("Mobile", ""), ("CGPA", "8.0"), ("Status", "Active"));
        if (fields == null) return;
        _db.Execute("""
            INSERT INTO Students (RegisterNumber, StudentName, Name, Department, Year, Semester, SectionId, Section, Email, Mobile, CGPA, ProfileImagePath, AttendancePercentage, EvidenceStatus, Status)
            VALUES ($register, $name, $name, $department, $year, $semester, $sectionId,
                    (SELECT SectionName FROM Sections WHERE SectionId = $sectionId), $email, $mobile, $cgpa, '', 0, 'Pending', $status);
            """, Params(fields, ("$register", "Register Number"), ("$name", "Name"), ("$department", "Department"), ("$year", "Year"), ("$semester", "Semester"), ("$sectionId", "Section ID"), ("$email", "Email"), ("$mobile", "Mobile"), ("$cgpa", "CGPA"), ("$status", "Status")));
        Log("Add student", fields["Name"]);
    }

    private void EditStudent(int id)
    {
        var row = Row("SELECT * FROM Students WHERE StudentId = $id;", id);
        var fields = Prompt("Edit Student", ("Name", Cell(row, "Name")), ("Department", Cell(row, "Department")), ("Year", Cell(row, "Year")), ("Semester", Cell(row, "Semester")), ("Section ID", Cell(row, "SectionId")), ("Email", Cell(row, "Email")), ("Mobile", Cell(row, "Mobile")), ("CGPA", Cell(row, "CGPA")), ("Status", Cell(row, "Status")));
        if (fields == null) return;
        _db.Execute("""
            UPDATE Students SET Name = $name, StudentName = $name, Department = $department, Year = $year, Semester = $semester,
                SectionId = $sectionId, Section = (SELECT SectionName FROM Sections WHERE SectionId = $sectionId),
                Email = $email, Mobile = $mobile, CGPA = $cgpa, Status = $status
            WHERE StudentId = $id;
            """, Params(fields, ("$name", "Name"), ("$department", "Department"), ("$year", "Year"), ("$semester", "Semester"), ("$sectionId", "Section ID"), ("$email", "Email"), ("$mobile", "Mobile"), ("$cgpa", "CGPA"), ("$status", "Status")).Append(("$id", id)).ToArray());
        Log("Edit student", fields["Name"]);
    }

    private void AddFaculty()
    {
        var fields = Prompt("Add Faculty", ("Faculty Code", ""), ("Name", ""), ("Department", "Computer Science and Engineering"), ("Designation", "Assistant Professor"), ("Email", ""), ("Mobile", ""), ("Office Room", ""), ("Experience", ""), ("Qualification", ""), ("Status", "Active"));
        if (fields == null) return;
        _db.Execute("INSERT INTO Users (Username, Password, Role, Email, IsActive) VALUES ($email, $password, 'Faculty', $email, 1) ON CONFLICT(Username) DO UPDATE SET Role = 'Faculty', Email = excluded.Email, IsActive = 1;",
            ("$email", fields["Email"]), ("$password", DBHelper.HashPassword("1234")));
        _db.Execute("""
            INSERT INTO Faculty (UserId, FacultyCode, Name, Department, Designation, Email, MobileNumber, Mobile, OfficeRoomNumber, OfficeRoom, Experience, Qualification, AssignedClasses, ProfileImagePath, Status)
            SELECT UserId, $code, $name, $department, $designation, $email, $mobile, $mobile, $room, $room, $experience, $qualification, '', '', $status
            FROM Users WHERE Username = $email;
            """, Params(fields, ("$code", "Faculty Code"), ("$name", "Name"), ("$department", "Department"), ("$designation", "Designation"), ("$email", "Email"), ("$mobile", "Mobile"), ("$room", "Office Room"), ("$experience", "Experience"), ("$qualification", "Qualification"), ("$status", "Status")));
        Log("Add faculty", fields["Name"]);
    }

    private void EditFaculty(int id)
    {
        var row = Row("SELECT * FROM Faculty WHERE FacultyId = $id;", id);
        var fields = Prompt("Edit Faculty", ("Name", Cell(row, "Name")), ("Department", Cell(row, "Department")), ("Designation", Cell(row, "Designation")), ("Email", Cell(row, "Email")), ("Mobile", First(Cell(row, "Mobile"), Cell(row, "MobileNumber"))), ("Office Room", First(Cell(row, "OfficeRoom"), Cell(row, "OfficeRoomNumber"))), ("Experience", Cell(row, "Experience")), ("Qualification", Cell(row, "Qualification")), ("Status", Cell(row, "Status")));
        if (fields == null) return;
        _db.Execute("""
            UPDATE Faculty SET Name = $name, Department = $department, Designation = $designation, Email = $email,
                Mobile = $mobile, MobileNumber = $mobile, OfficeRoom = $room, OfficeRoomNumber = $room,
                Experience = $experience, Qualification = $qualification, Status = $status
            WHERE FacultyId = $id;
            """, Params(fields, ("$name", "Name"), ("$department", "Department"), ("$designation", "Designation"), ("$email", "Email"), ("$mobile", "Mobile"), ("$room", "Office Room"), ("$experience", "Experience"), ("$qualification", "Qualification"), ("$status", "Status")).Append(("$id", id)).ToArray());
        Log("Edit faculty", fields["Name"]);
    }

    private void AddDepartment()
    {
        var fields = Prompt("Add Department", ("Department Name", ""), ("HOD Name", ""), ("Status", "Active"));
        if (fields == null) return;
        _db.Execute("INSERT INTO Departments (DepartmentName, HodName, Status) VALUES ($name, $hod, $status);", Params(fields, ("$name", "Department Name"), ("$hod", "HOD Name"), ("$status", "Status")));
        Log("Add department", fields["Department Name"]);
    }

    private void EditDepartment(int id)
    {
        var row = Row("SELECT * FROM Departments WHERE DepartmentId = $id;", id);
        var fields = Prompt("Edit Department", ("Department Name", Cell(row, "DepartmentName")), ("HOD Name", Cell(row, "HodName")), ("Status", Cell(row, "Status")));
        if (fields == null) return;
        _db.Execute("UPDATE Departments SET DepartmentName = $name, HodName = $hod, Status = $status WHERE DepartmentId = $id;", Params(fields, ("$name", "Department Name"), ("$hod", "HOD Name"), ("$status", "Status")).Append(("$id", id)).ToArray());
        Log("Edit department", fields["Department Name"]);
    }

    private void AddSection()
    {
        var fields = Prompt("Add Section", ("Department", "Computer Science and Engineering"), ("Year", "2"), ("Semester", "4"), ("Section Name", ""), ("Total Students", "0"), ("Class In-charge", ""));
        if (fields == null) return;
        _db.Execute("INSERT INTO Sections (Department, Year, Semester, SectionName, TotalStudents, ClassInCharge) VALUES ($department, $year, $semester, $name, $total, $incharge);", Params(fields, ("$department", "Department"), ("$year", "Year"), ("$semester", "Semester"), ("$name", "Section Name"), ("$total", "Total Students"), ("$incharge", "Class In-charge")));
        Log("Add section", fields["Section Name"]);
    }

    private void EditSection(int id)
    {
        var row = Row("SELECT * FROM Sections WHERE SectionId = $id;", id);
        var fields = Prompt("Edit Section", ("Department", Cell(row, "Department")), ("Year", Cell(row, "Year")), ("Semester", Cell(row, "Semester")), ("Section Name", Cell(row, "SectionName")), ("Total Students", Cell(row, "TotalStudents")), ("Class In-charge", Cell(row, "ClassInCharge")));
        if (fields == null) return;
        _db.Execute("UPDATE Sections SET Department = $department, Year = $year, Semester = $semester, SectionName = $name, TotalStudents = $total, ClassInCharge = $incharge WHERE SectionId = $id;", Params(fields, ("$department", "Department"), ("$year", "Year"), ("$semester", "Semester"), ("$name", "Section Name"), ("$total", "Total Students"), ("$incharge", "Class In-charge")).Append(("$id", id)).ToArray());
        Log("Edit section", fields["Section Name"]);
    }

    private void AddSubject()
    {
        var fields = Prompt("Add Subject", ("Subject Code", ""), ("Subject Name", ""), ("Department", "Computer Science and Engineering"), ("Semester", "4"), ("Credits", "3"), ("Subject Type", "Theory"), ("Faculty Assigned", ""));
        if (fields == null) return;
        _db.Execute("""
            INSERT INTO Subjects (SubjectCode, SubjectName, Department, Semester, Credits, SubjectType, FacultyName, FacultyAssigned, SyllabusPath, CourseOutcomes)
            VALUES ($code, $name, $department, $semester, $credits, $type, $faculty, $faculty, '', 'Created by admin.');
            """, Params(fields, ("$code", "Subject Code"), ("$name", "Subject Name"), ("$department", "Department"), ("$semester", "Semester"), ("$credits", "Credits"), ("$type", "Subject Type"), ("$faculty", "Faculty Assigned")));
        Log("Add subject", fields["Subject Name"]);
    }

    private void EditSubject(int id)
    {
        var row = Row("SELECT * FROM Subjects WHERE SubjectId = $id;", id);
        var fields = Prompt("Edit Subject", ("Subject Code", Cell(row, "SubjectCode")), ("Subject Name", Cell(row, "SubjectName")), ("Department", Cell(row, "Department")), ("Semester", Cell(row, "Semester")), ("Credits", Cell(row, "Credits")), ("Subject Type", Cell(row, "SubjectType")), ("Faculty Assigned", First(Cell(row, "FacultyAssigned"), Cell(row, "FacultyName"))));
        if (fields == null) return;
        _db.Execute("UPDATE Subjects SET SubjectCode = $code, SubjectName = $name, Department = $department, Semester = $semester, Credits = $credits, SubjectType = $type, FacultyName = $faculty, FacultyAssigned = $faculty WHERE SubjectId = $id;", Params(fields, ("$code", "Subject Code"), ("$name", "Subject Name"), ("$department", "Department"), ("$semester", "Semester"), ("$credits", "Credits"), ("$type", "Subject Type"), ("$faculty", "Faculty Assigned")).Append(("$id", id)).ToArray());
        Log("Edit subject", fields["Subject Name"]);
    }

    private void AddEvent()
    {
        var fields = Prompt("Add Event", ("Event Name", ""), ("Date", DateTime.Today.ToString("yyyy-MM-dd")), ("Venue", ""), ("Conducted By", "Admin"), ("Type", "Academic"), ("Deadline", DateTime.Today.AddDays(3).ToString("yyyy-MM-dd")), ("Description", ""));
        if (fields == null) return;
        _db.Execute("INSERT INTO CampusEvents (EventTitle, EventDate, Venue, ConductedBy, EventType, RegistrationDeadline, EventDescription) VALUES ($title, $date, $venue, $by, $type, $deadline, $description);", Params(fields, ("$title", "Event Name"), ("$date", "Date"), ("$venue", "Venue"), ("$by", "Conducted By"), ("$type", "Type"), ("$deadline", "Deadline"), ("$description", "Description")));
        Log("Add event", fields["Event Name"]);
    }

    private void EditEvent(int id)
    {
        var row = Row("SELECT * FROM CampusEvents WHERE EventId = $id;", id);
        var fields = Prompt("Edit Event", ("Event Name", Cell(row, "EventTitle")), ("Date", Cell(row, "EventDate")), ("Venue", Cell(row, "Venue")), ("Conducted By", Cell(row, "ConductedBy")), ("Type", Cell(row, "EventType")), ("Deadline", Cell(row, "RegistrationDeadline")), ("Description", Cell(row, "EventDescription")));
        if (fields == null) return;
        _db.Execute("UPDATE CampusEvents SET EventTitle = $title, EventDate = $date, Venue = $venue, ConductedBy = $by, EventType = $type, RegistrationDeadline = $deadline, EventDescription = $description WHERE EventId = $id;", Params(fields, ("$title", "Event Name"), ("$date", "Date"), ("$venue", "Venue"), ("$by", "Conducted By"), ("$type", "Type"), ("$deadline", "Deadline"), ("$description", "Description")).Append(("$id", id)).ToArray());
        Log("Edit event", fields["Event Name"]);
    }

    private void AddUser()
    {
        var fields = Prompt("Add User", ("Username", ""), ("Email", ""), ("Role", "Student"), ("Password", "1234"), ("Active", "1"));
        if (fields == null) return;
        _db.Execute("INSERT INTO Users (Username, Email, Password, Role, IsActive) VALUES ($username, $email, $password, $role, $active);",
            ("$username", fields["Username"]), ("$email", fields["Email"]), ("$password", DBHelper.HashPassword(fields["Password"])), ("$role", fields["Role"]), ("$active", fields["Active"]));
        Log("Add user", fields["Username"]);
    }

    private void EditUser(int id)
    {
        var row = Row("SELECT * FROM Users WHERE UserId = $id;", id);
        var fields = Prompt("Edit User", ("Username", Cell(row, "Username")), ("Email", Cell(row, "Email")), ("Role", Cell(row, "Role")), ("Active", Cell(row, "IsActive")));
        if (fields == null) return;
        _db.Execute("UPDATE Users SET Username = $username, Email = $email, Role = $role, IsActive = $active WHERE UserId = $id;", Params(fields, ("$username", "Username"), ("$email", "Email"), ("$role", "Role"), ("$active", "Active")).Append(("$id", id)).ToArray());
        Log("Edit user", fields["Username"]);
    }

    private void AddReport()
    {
        var fields = Prompt("Add Report", ("Report Name", ""), ("Type", "Student progress report"), ("File Path", ""));
        if (fields == null) return;
        _db.Execute("INSERT INTO Reports (ReportName, ReportType, GeneratedBy, GeneratedAt, FilePath) VALUES ($name, $type, $by, datetime('now'), $path);",
            ("$name", fields["Report Name"]), ("$type", fields["Type"]), ("$by", AdminSessionManager.Current?.Username ?? "admin1"), ("$path", fields["File Path"]));
        Log("Generate report", fields["Report Name"]);
    }

    private void EditAttendance(int id)
    {
        var row = Row("SELECT * FROM FacultyAttendanceDaily WHERE AttendanceRecordId = $id;", id);
        var fields = Prompt("Edit Attendance", ("Status", Cell(row, "Status") == "Present" ? "Present" : "Absent"));
        if (fields == null) return;
        var status = fields["Status"].Equals("Present", StringComparison.OrdinalIgnoreCase) ? "Present" : "Absent";
        _db.Execute("UPDATE FacultyAttendanceDaily SET Status = $status WHERE AttendanceRecordId = $id;", ("$status", status), ("$id", id));
        Log("Edit attendance", id.ToString());
    }

    private void EditMarks(int id)
    {
        var row = Row("SELECT * FROM FacultyMarksRecords WHERE FacultyMarkId = $id;", id);
        var fields = Prompt("Edit Marks", ("Max Marks", Cell(row, "MaxMarks")), ("Marks Obtained", Cell(row, "MarksObtained")), ("Grade", Cell(row, "Grade")), ("Remarks", Cell(row, "Remarks")));
        if (fields == null) return;
        _db.Execute("UPDATE FacultyMarksRecords SET MaxMarks = $max, MarksObtained = $marks, Grade = $grade, Remarks = $remarks WHERE FacultyMarkId = $id;", Params(fields, ("$max", "Max Marks"), ("$marks", "Marks Obtained"), ("$grade", "Grade"), ("$remarks", "Remarks")).Append(("$id", id)).ToArray());
        Log("Edit marks", id.ToString());
    }

    private void OverrideEvidenceStatus(int id)
    {
        var row = Row("SELECT * FROM LearningEvidence WHERE EvidenceId = $id;", id);
        var fields = Prompt("Override Evidence Status", ("Status", Cell(row, "Status")), ("Faculty Comments", Cell(row, "FacultyComments")));
        if (fields == null) return;
        _db.Execute("UPDATE LearningEvidence SET Status = $status, ValidationStatus = $status, FacultyComments = $comments, LastUpdatedDate = date('now') WHERE EvidenceId = $id;", Params(fields, ("$status", "Status"), ("$comments", "Faculty Comments")).Append(("$id", id)).ToArray());
        Log("Override evidence", id.ToString());
    }

    private void EditNoteStatus(int id)
    {
        var row = Row("SELECT * FROM Notes WHERE NoteId = $id;", id);
        var fields = Prompt("Edit Note Status", ("Approval Status", Cell(row, "ApprovalStatus")), ("Faculty Comments", Cell(row, "FacultyComments")));
        if (fields == null) return;
        _db.Execute("UPDATE Notes SET ApprovalStatus = $status, ApprovedBy = 'Admin', FacultyComments = $comments WHERE NoteId = $id;", Params(fields, ("$status", "Approval Status"), ("$comments", "Faculty Comments")).Append(("$id", id)).ToArray());
        Log("Edit note", id.ToString());
    }

    private void DeleteById(string table, string idColumn, int id, string label)
    {
        if (MessageBox.Show($"Delete selected {label}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _db.Execute($"DELETE FROM {table} WHERE {idColumn} = $id;", ("$id", id));
        Log($"Delete {label}", id.ToString());
    }

    private void ViewStudentProfile(int id) => ShowTableDialog("Student Full Profile", "SELECT * FROM Students WHERE StudentId = $id;", ("$id", id));
    private void ViewStudentAttendance(int id) => ShowTableDialog("Student Attendance", "SELECT * FROM FacultyAttendanceDaily WHERE StudentId = $id ORDER BY AttendanceDate DESC;", ("$id", id));
    private void ViewStudentMarks(int id) => ShowTableDialog("Student Marks", "SELECT * FROM FacultyMarksRecords WHERE StudentId = $id ORDER BY Semester, ExamType;", ("$id", id));
    private void ViewStudentEvidence(int id) => ShowTableDialog("Student Evidence", "SELECT * FROM LearningEvidence WHERE StudentId = $id ORDER BY UploadDate DESC;", ("$id", id));
    private void ViewStudentNotes(int id) => ShowTableDialog("Student Notes", "SELECT * FROM Notes WHERE StudentId = $id ORDER BY UploadDate DESC;", ("$id", id));
    private void GenerateStudentReport(int id) { ExportQuery($"Student_{id}_Report", "SELECT * FROM Students WHERE StudentId = " + id); Log("Generate student report", id.ToString()); }
    private void ViewFacultyProfile(int id) => ShowTableDialog("Faculty Full Profile", "SELECT * FROM Faculty WHERE FacultyId = $id;", ("$id", id));
    private void ViewFacultySubjects(int id) => ShowTableDialog("Faculty Handled Subjects", "SELECT sub.SubjectCode, sub.SubjectName, sec.SectionName, sec.Semester FROM FacultySections fs JOIN Subjects sub ON sub.SubjectId = fs.SubjectId JOIN Sections sec ON sec.SectionId = fs.SectionId WHERE fs.FacultyId = $id;", ("$id", id));
    private void ViewFacultyStudents(int id) => ShowTableDialog("Faculty Students", "SELECT DISTINCT st.RegisterNumber, st.Name, sec.SectionName, st.Email, st.Mobile FROM Students st JOIN Sections sec ON sec.SectionId = st.SectionId JOIN FacultySections fs ON fs.SectionId = st.SectionId WHERE fs.FacultyId = $id ORDER BY sec.SectionName, st.RegisterNumber;", ("$id", id));
    private void ViewDepartmentDetails(int id) => ShowTableDialog("Department Details", "SELECT * FROM Departments WHERE DepartmentId = $id;", ("$id", id));
    private void ViewSectionDetails(int id) => ShowTableDialog("Section Students", "SELECT RegisterNumber, Name, Email, Mobile, CGPA, Status FROM Students WHERE SectionId = $id ORDER BY RegisterNumber;", ("$id", id));
    private void ViewSubjectDetails(int id) => ShowTableDialog("Subject Details", "SELECT * FROM Subjects WHERE SubjectId = $id;", ("$id", id));
    private void ViewAttendanceRecord(int id) => ShowTableDialog("Attendance Record", "SELECT * FROM FacultyAttendanceDaily WHERE AttendanceRecordId = $id;", ("$id", id));
    private void ViewMarksRecord(int id) => ShowTableDialog("Marks Record", "SELECT * FROM FacultyMarksRecords WHERE FacultyMarkId = $id;", ("$id", id));
    private void ViewEvidence(int id) => ShowTableDialog("Evidence Details", "SELECT * FROM LearningEvidence WHERE EvidenceId = $id;", ("$id", id));
    private void ViewNote(int id) => ShowTableDialog("Note Details", "SELECT * FROM Notes WHERE NoteId = $id;", ("$id", id));
    private void ViewEvent(int id) => ShowTableDialog("Event Details", "SELECT * FROM CampusEvents WHERE EventId = $id;", ("$id", id));
    private void ViewEventRegistrations(int id) => ShowTableDialog("Registered Students", "SELECT sp.RegisterNumber, sp.FullName, er.RegistrationDate, er.Status FROM EventRegistrations er LEFT JOIN StudentProfile sp ON sp.StudentId = er.StudentId WHERE er.EventId = $id;", ("$id", id));
    private void ViewUser(int id) => ShowTableDialog("User Account", "SELECT UserId, Username, Email, Role, IsActive, LastLogin FROM Users WHERE UserId = $id;", ("$id", id));
    private void ViewReport(int id) => ShowTableDialog("Report Details", "SELECT * FROM Reports WHERE ReportId = $id;", ("$id", id));

    private void SetEvidenceStatus(int id, string status)
    {
        _db.Execute("UPDATE LearningEvidence SET Status = $status, ValidationStatus = $status, LastUpdatedDate = date('now') WHERE EvidenceId = $id;", ("$status", status), ("$id", id));
        Log($"{status} evidence", id.ToString());
    }

    private void SetNoteStatus(int id, string status)
    {
        _db.Execute("UPDATE Notes SET ApprovalStatus = $status, ApprovedBy = 'Admin' WHERE NoteId = $id;", ("$status", status), ("$id", id));
        Log($"{status} note", id.ToString());
    }

    private void SetUserActive(int id, bool active)
    {
        _db.Execute("UPDATE Users SET IsActive = $active WHERE UserId = $id;", ("$active", active ? 1 : 0), ("$id", id));
        Log(active ? "Activate user" : "Deactivate user", id.ToString());
    }

    private void ResetUserPassword(int id)
    {
        var fields = Prompt("Reset Password", ("New Password", "1234"));
        if (fields == null) return;
        _db.Execute("UPDATE Users SET Password = $password WHERE UserId = $id;", ("$password", DBHelper.HashPassword(fields["New Password"])), ("$id", id));
        Log("Reset password", id.ToString());
    }

    private void ResetFacultyPassword(int facultyId)
    {
        var row = Row("SELECT UserId FROM Faculty WHERE FacultyId = $id;", facultyId);
        if (int.TryParse(Cell(row, "UserId"), out var userId)) ResetUserPassword(userId);
    }

    private void AssignFacultySubject(int facultyId)
    {
        var fields = Prompt("Assign Subject To Faculty", ("Section ID", "1"), ("Subject ID", "1"));
        if (fields == null) return;
        _db.Execute("INSERT OR IGNORE INTO FacultySections (FacultyId, SectionId, SubjectId) VALUES ($facultyId, $sectionId, $subjectId);",
            ("$facultyId", facultyId), ("$sectionId", fields["Section ID"]), ("$subjectId", fields["Subject ID"]));
        Log("Assign faculty subject", facultyId.ToString());
    }

    private void AssignFacultySection(int sectionOrFacultyId)
    {
        var fields = Prompt("Assign Faculty To Section", ("Faculty ID", sectionOrFacultyId.ToString()), ("Section ID", "1"), ("Subject ID", "1"));
        if (fields == null) return;
        _db.Execute("INSERT OR IGNORE INTO FacultySections (FacultyId, SectionId, SubjectId) VALUES ($facultyId, $sectionId, $subjectId);",
            ("$facultyId", fields["Faculty ID"]), ("$sectionId", fields["Section ID"]), ("$subjectId", fields["Subject ID"]));
        Log("Assign faculty section", fields["Faculty ID"]);
    }

    private void AssignSubjectToFaculty(int subjectId)
    {
        var fields = Prompt("Assign Subject", ("Faculty ID", "1"), ("Section ID", "1"));
        if (fields == null) return;
        _db.Execute("INSERT OR IGNORE INTO FacultySections (FacultyId, SectionId, SubjectId) VALUES ($facultyId, $sectionId, $subjectId);",
            ("$facultyId", fields["Faculty ID"]), ("$sectionId", fields["Section ID"]), ("$subjectId", subjectId));
        Log("Assign subject", subjectId.ToString());
    }

    private void AssignStudentsToSection(int sectionId)
    {
        var fields = Prompt("Assign Students To Section", ("Register Number Contains", ""), ("Target Section ID", sectionId.ToString()));
        if (fields == null) return;
        _db.Execute("""
            UPDATE Students
            SET SectionId = $sectionId,
                Section = (SELECT SectionName FROM Sections WHERE SectionId = $sectionId),
                Semester = (SELECT Semester FROM Sections WHERE SectionId = $sectionId),
                Year = (SELECT Year FROM Sections WHERE SectionId = $sectionId)
            WHERE RegisterNumber LIKE '%' || $register || '%';
            """, ("$sectionId", fields["Target Section ID"]), ("$register", fields["Register Number Contains"]));
        Log("Assign students to section", fields["Target Section ID"]);
    }

    private void UpdateStudentImage(int id) => UpdateImage("Students", "StudentId", id, $"Admin_Student_{id}.jpg");
    private void UpdateFacultyImage(int id) => UpdateImage("Faculty", "FacultyId", id, $"Admin_Faculty_{id}.jpg");

    private void UpdateImage(string table, string idColumn, int id, string fileName)
    {
        using var picker = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*" };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        var folder = Path.Combine(StudentDatabase.DataDirectory, "ProfileImages");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, fileName);
        File.Copy(picker.FileName, target, true);
        _db.Execute($"UPDATE {table} SET ProfileImagePath = $path WHERE {idColumn} = $id;", ("$path", target), ("$id", id));
        Log("Update image", $"{table}:{id}");
    }

    private void BackupDatabase()
    {
        var folder = Path.Combine(StudentDatabase.DataDirectory, "AdminBackups");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, $"StudentValidationSystem_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite");
        File.Copy(StudentDatabase.DatabasePath, target, true);
        _db.Execute("INSERT INTO Reports (ReportName, ReportType, GeneratedBy, GeneratedAt, FilePath) VALUES ($name, 'Database backup', $by, datetime('now'), $path);",
            ("$name", Path.GetFileName(target)), ("$by", AdminSessionManager.Current?.Username ?? "admin1"), ("$path", target));
        MessageBox.Show($"Backup created:\n{target}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RestoreDatabase()
    {
        using var picker = new OpenFileDialog { Filter = "SQLite database|*.db;*.sqlite;*.sqlite3|All files|*.*" };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show("Restore will replace the current SQLite database. Continue?", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        File.Copy(picker.FileName, StudentDatabase.DatabasePath, true);
        MessageBox.Show("Database restored. Restart the application to reload all data.", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportQuery(string reportName, string sql)
    {
        var table = _db.GetDataTable(sql);
        var folder = Path.Combine(StudentDatabase.DataDirectory, "AdminExports");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var lines = new List<string> { string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Csv(c.ColumnName))) };
        lines.AddRange(table.Rows.Cast<DataRow>().Select(row => string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Csv(row[c]?.ToString() ?? "")))));
        File.WriteAllLines(file, lines, Encoding.UTF8);
        _db.Execute("INSERT INTO Reports (ReportName, ReportType, GeneratedBy, GeneratedAt, FilePath) VALUES ($name, 'CSV export', $by, datetime('now'), $path);",
            ("$name", reportName), ("$by", AdminSessionManager.Current?.Username ?? "admin1"), ("$path", file));
        MessageBox.Show($"Exported:\n{file}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private Panel Metric(string title, string value, Color accent)
    {
        var card = Ui.Card(_theme, 12);
        card.Size = new Size(220, 80);
        card.Controls.Add(new Label { Text = title, Location = new Point(10, 10), Size = new Size(200, 20), ForeColor = _theme.MutedText, BackColor = Color.Transparent, Font = Ui.SmallFont });
        card.Controls.Add(new Label { Text = value, Location = new Point(10, 35), Size = new Size(200, 32), ForeColor = accent, BackColor = Color.Transparent, Font = Ui.TitleFont });
        return card;
    }

    private TableLayoutPanel DashboardGridRow() => new()
    {
        Dock = DockStyle.Top,
        Height = 278,
        ColumnCount = 2,
        RowCount = 1,
        BackColor = _theme.Background,
        Margin = new Padding(0, 0, 0, 16),
        ColumnStyles =
        {
            new ColumnStyle(SizeType.Percent, 50),
            new ColumnStyle(SizeType.Percent, 50)
        },
        RowStyles =
        {
            new RowStyle(SizeType.Percent, 100)
        }
    };

    private Panel DashboardGridCard(string title, string sql, int height, params (string Name, object? Value)[] parameters)
    {
        var card = GridCard(title, sql, 0, 0, 520, height, parameters);
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(0, 0, 16, 0);
        return card;
    }

    private Panel GridCard(string title, string sql, int x, int y, int width, int height, params (string Name, object? Value)[] parameters)
    {
        var card = Ui.Card(_theme, 14);
        card.Location = new Point(x, y);
        card.Size = new Size(width, height);
        card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        card.Controls.Add(Label(title, 10, 8, width - 20, Ui.HeadingFont));
        var grid = new DataGridView 
        { 
            Location = new Point(10, 44), 
            Size = new Size(width - 20, height - 56), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom 
        };
        UiFactory.StyleGrid(grid, _theme);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        grid.AllowUserToResizeColumns = true;
        grid.ScrollBars = ScrollBars.Both;
        grid.DataSource = _db.GetDataTable(sql, parameters);
        card.Controls.Add(grid);
        return card;
    }

    private DataGridView Grid(int x, int y, int width, int height)
    {
        var grid = new DataGridView { Location = new Point(x, y), Size = new Size(width, height), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        UiFactory.StyleGrid(grid, _theme);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        grid.AllowUserToResizeColumns = true;
        grid.AllowUserToResizeRows = false;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
        return grid;
    }

    private Label Label(string text, int x, int y, int width, Font font) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(width, 28),
        Font = font,
        ForeColor = _theme.Text,
        BackColor = Color.Transparent
    };

    private int AddPageButton(Control parent, string text, int x, int y, Action action)
    {
        AddButton(parent, text, x, y, action);
        return x + Math.Max(92, text.Length * 10 + 22) + 10;
    }

    private void AddButton(Control parent, string text, int x, int y, Action action)
    {
        var button = UiFactory.PrimaryButton(text, _theme);
        button.Location = new Point(x, y);
        button.Width = Math.Max(92, text.Length * 10 + 22);
        button.Click += (_, _) =>
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Admin Action Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        parent.Controls.Add(button);
    }

    private bool TrySelectedId(DataGridView grid, string column, out int id)
    {
        id = 0;
        if (grid.CurrentRow == null || !grid.Columns.Contains(column))
        {
            MessageBox.Show("Select a row first.", "Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return int.TryParse(grid.CurrentRow.Cells[column].Value?.ToString(), out id);
    }

    private Dictionary<string, string>? Prompt(string title, params (string Label, string Value)[] fields)
    {
        using var dialog = new Form { Text = title, Size = new Size(520, Math.Min(720, 120 + fields.Length * 58)), StartPosition = FormStartPosition.CenterParent, Font = Ui.BodyFont, BackColor = _theme.Background };
        var values = new Dictionary<string, TextBox>();
        var y = 18;
        foreach (var field in fields)
        {
            dialog.Controls.Add(new Label { Text = field.Label, Location = new Point(24, y), Size = new Size(430, 20), ForeColor = _theme.MutedText, BackColor = Color.Transparent, Font = Ui.SmallFont });
            var box = new TextBox { Location = new Point(24, y + 22), Width = 450, Text = field.Value };
            dialog.Controls.Add(box);
            values[field.Label] = box;
            y += 58;
        }
        var ok = UiFactory.PrimaryButton("Save", _theme);
        ok.Location = new Point(256, y + 4);
        ok.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
        var cancel = Ui.Button("Cancel", _theme.SurfaceAlt, _theme.Text);
        cancel.Location = new Point(374, y + 4);
        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        return dialog.ShowDialog(this) == DialogResult.OK
            ? values.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim())
            : null;
    }

    private void ShowTableDialog(string title, string sql, params (string Name, object? Value)[] parameters)
    {
        using var dialog = new Form { Text = title, Size = new Size(920, 560), StartPosition = FormStartPosition.CenterParent, Font = Ui.BodyFont };
        var grid = new DataGridView { Dock = DockStyle.Fill };
        UiFactory.StyleGrid(grid, _theme);
        grid.DataSource = _db.GetDataTable(sql, parameters);
        dialog.Controls.Add(grid);
        dialog.ShowDialog(this);
    }

    private DataRow Row(string sql, int id)
    {
        var table = _db.GetDataTable(sql, ("$id", id));
        if (table.Rows.Count == 0) throw new InvalidOperationException("Selected record was not found.");
        return table.Rows[0];
    }

    private string Count(string sql) => Convert.ToString(_db.Scalar(sql)) ?? "0";
    private static string Cell(DataRow row, string column) => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column]?.ToString() ?? "" : "";
    private static string First(params string[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static (string Name, object? Value)[] Params(Dictionary<string, string> fields, params (string Name, string Key)[] map) =>
        map.Select(item => (item.Name, (object?)fields[item.Key])).ToArray();

    private void Log(string action, string entity)
    {
        _db.Execute("INSERT INTO ActivityLogs (ActorRole, ActorName, Action, EntityName, CreatedAt) VALUES ('Admin', $actor, $action, $entity, datetime('now'));",
            ("$actor", AdminSessionManager.Current?.Username ?? "admin1"), ("$action", action), ("$entity", entity));
    }

    private sealed record AdminPageSpec(
        string Title,
        string Sql,
        string IdColumn,
        Action? AddAction,
        Action<int>? EditAction,
        Action<int>? DeleteAction,
        Action<int> ViewAction,
        (string Text, Action<int> Action)[]? ExtraActions = null)
    {
        public (string Text, Action<int> Action)[] ExtraActions { get; } = ExtraActions ?? [];
    }
}
