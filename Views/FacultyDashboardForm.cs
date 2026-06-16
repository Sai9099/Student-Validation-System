using System.Data;
using System.Drawing.Imaging;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyDashboardForm : Form
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly DBHelper _db = new();
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly List<Button> _navButtons = [];
    private string _page = "Dashboard";

    private int FacultyId => FacultySessionManager.FacultyId == 0 ? 1 : FacultySessionManager.FacultyId;

    public FacultyDashboardForm()
    {
        _db.Initialize();
        Text = "Faculty Dashboard - Student Learning Evidence & Progress Validation System";
        Size = new Size(1380, 830);
        MinimumSize = new Size(1220, 740);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.BodyFont;
        BuildShell();
        LoadPage("Dashboard");
    }

    private void BuildShell()
    {
        Controls.Clear();
        _navButtons.Clear();
        BackColor = _theme.Background;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildSidebar(), 0, 0);

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = _theme.Background };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(workspace, 1, 0);

        var top = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface };
        workspace.Controls.Add(top, 0, 0);

        _title.Font = Ui.TitleFont;
        _title.ForeColor = _theme.Text;
        _title.AutoSize = true;
        _title.Location = new Point(28, 20);
        top.Controls.Add(_title);

        var welcome = Ui.Label($"Welcome, {FacultySessionManager.FacultyName}", Ui.BodyFont, _theme.MutedText);
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

    private Control BuildSidebar()
    {
        var side = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Sidebar, Padding = new Padding(18) };
        side.Controls.Add(new Label
        {
            Text = "Faculty\nModule",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(18, 24),
            Size = new Size(200, 72)
        });

        var y = 118;
        foreach (var page in new[]
        {
            "Dashboard", "Profile", "My Sections", "Student Management", "Attendance", "Marks",
            "Evidence Validation", "Notes Management", "Campus Events"
        })
        {
            AddNav(side, page, y);
            y += 43;
        }

        var logout = Ui.Button("Logout", Color.FromArgb(57, 72, 91), Color.White);
        logout.FlatAppearance.BorderSize = 0;
        logout.Location = new Point(18, 700);
        logout.Width = 205;
        logout.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        logout.Click += (_, _) =>
        {
            FacultySessionManager.Clear();
            Close();
        };
        side.Controls.Add(logout);
        return side;
    }

    private void AddNav(Control side, string page, int y)
    {
        var button = Ui.Button(page, Color.Transparent, Color.FromArgb(226, 236, 245));
        button.FlatAppearance.BorderSize = 0;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Location = new Point(18, y);
        button.Size = new Size(205, 36);
        button.Tag = page;
        button.Click += (_, _) => LoadPage(page);
        _navButtons.Add(button);
        side.Controls.Add(button);
    }

    private void LoadPage(string page)
    {
        _page = page;
        _title.Text = page;
        _content.Controls.Clear();
        _content.Controls.Add(page switch
        {
            "Profile" => BuildProfilePage(),
            "My Sections" => BuildMySectionsPage(),
            "Student Management" => BuildStudentManagementPage(),
            "Attendance" => BuildAttendancePage(),
            "Marks" => BuildMarksPage(),
            "Evidence Validation" => BuildEvidenceValidationPage(),
            "Notes Management" => BuildNotesManagementPage(),
            "Campus Events" => BuildCampusEventsPage(),
            _ => BuildHomePage()
        });

        foreach (var button in _navButtons)
        {
            button.BackColor = button.Tag?.ToString() == page ? Color.FromArgb(48, 91, 124) : Color.Transparent;
        }
    }

    private Control BuildHomePage()
    {
        var root = ScrollRoot();
        root.Padding = new Padding(0, 0, 18, 18);

        var cards = new FlowLayoutPanel { Width = 1088, Height = 220, WrapContents = true, BackColor = _theme.Background, Margin = new Padding(0, 0, 0, 14) };
        root.Controls.Add(cards);
        cards.Controls.Add(DashboardCard("Total Sections Assigned", Count("SELECT COUNT(DISTINCT SectionId) FROM FacultySections WHERE FacultyId = $facultyId;", ("$facultyId", FacultyId)), _theme.Primary, 250));
        cards.Controls.Add(DashboardCard("Total Students", Count("SELECT COUNT(*) FROM Students WHERE SectionId IN (SELECT SectionId FROM FacultySections WHERE FacultyId = $facultyId);", ("$facultyId", FacultyId)), _theme.Primary, 250));
        cards.Controls.Add(DashboardCard("Subjects Handled", Count("SELECT COUNT(DISTINCT SubjectId) FROM FacultySections WHERE FacultyId = $facultyId;", ("$facultyId", FacultyId)), _theme.Primary, 250));
        cards.Controls.Add(DashboardCard("Pending Evidence", Count(AssignedEvidenceCountSql("le.Status IN ('Pending','Submitted','Resubmitted')"), ("$facultyId", FacultyId)), _theme.Warning, 250));
        cards.Controls.Add(DashboardCard("Approved Evidence", Count(AssignedEvidenceCountSql("le.Status = 'Approved'"), ("$facultyId", FacultyId)), _theme.Success, 250));
        cards.Controls.Add(DashboardCard("Attendance Shortage Students", Count("SELECT COUNT(*) FROM Students WHERE SectionId IN (SELECT SectionId FROM FacultySections WHERE FacultyId = $facultyId) AND AttendancePercentage < 75;", ("$facultyId", FacultyId)), _theme.Danger, 250));
        cards.Controls.Add(DashboardCard("Upcoming Events", Count("SELECT COUNT(*) FROM CampusEvents WHERE EventDate >= date('now');"), _theme.Primary, 250));
        cards.Controls.Add(DashboardCard("Notes Uploaded", Count("SELECT COUNT(*) FROM Notes WHERE FacultyId = $facultyId OR UploadedByRole = 'Faculty';", ("$facultyId", FacultyId)), _theme.Success, 250));

        var actions = Ui.Card(_theme, 14);
        actions.Width = 1088;
        actions.Height = 86;
        actions.Margin = new Padding(0, 0, 0, 14);
        actions.Controls.Add(Section("Quick Actions", 12, 8));
        var quick = new[] { ("Take Attendance", "Attendance"), ("Enter Marks", "Marks"), ("Validate Evidence", "Evidence Validation"), ("Upload Notes", "Notes Management"), ("View Sections", "My Sections") };
        var actionX = 12;
        foreach (var (text, page) in quick)
        {
            AddAction(actions, text, actionX, 42, () => LoadPage(page));
            actionX += 160;
        }
        root.Controls.Add(actions);

        root.Controls.Add(HomeGridCard("My Sections Overview", SectionOverviewSql(), 1088, 250, ("$facultyId", FacultyId)));

        var split = new TableLayoutPanel { Width = 1088, Height = 300, ColumnCount = 2, RowCount = 1, BackColor = _theme.Background, Margin = new Padding(0, 0, 0, 14) };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.Controls.Add(split);
        split.Controls.Add(HomeGridCard("Recent Activity", RecentActivitySql(), 526, 300, ("$facultyId", FacultyId)), 0, 0);
        var alerts = HomeGridCard("Attendance Alerts", ShortageSql(), 526, 300, ("$facultyId", FacultyId));
        var alertGrid = alerts.Controls.OfType<DataGridView>().FirstOrDefault();
        if (alertGrid != null) ApplyAttendanceAlertFormatting(alertGrid);
        split.Controls.Add(alerts, 1, 0);

        var lower = new TableLayoutPanel { Width = 1088, Height = 350, ColumnCount = 2, RowCount = 1, BackColor = _theme.Background, Margin = new Padding(0, 0, 0, 14) };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.Controls.Add(lower);
        lower.Controls.Add(HomeGridCard("Upcoming Events", """
            SELECT EventTitle AS 'Event Name', EventDate AS Date, Venue, ConductedBy AS 'Conducted By'
            FROM CampusEvents
            WHERE EventDate >= date('now')
            ORDER BY EventDate
            LIMIT 8;
            """, 526, 350), 0, 0);

        var chartCard = Ui.Card(_theme, 14);
        chartCard.Dock = DockStyle.Fill;
        chartCard.Controls.Add(Section("Charts", 12, 8));
        var attendanceChart = new SimpleChartPanel(_theme, ChartKind.Bar)
        {
            Location = new Point(12, 42),
            Size = new Size(240, 255),
            Data = ChartData(SectionAttendanceChartSql(), "SectionName", "AverageAttendance", ("$facultyId", FacultyId))
        };
        var evidenceChart = new SimpleChartPanel(_theme, ChartKind.Pie)
        {
            Location = new Point(270, 42),
            Size = new Size(220, 255),
            Data = ChartData(EvidenceStatusChartSql(), "Status", "Total", ("$facultyId", FacultyId))
        };
        chartCard.Controls.Add(attendanceChart);
        chartCard.Controls.Add(evidenceChart);
        lower.Controls.Add(chartCard, 1, 0);
        return root;
    }

    private Control BuildProfilePage()
    {
        var root = ScrollRoot();
        root.Padding = new Padding(0, 0, 18, 18);
        var profile = FacultyProfileRow();
        var fields = new Dictionary<string, TextBox>();

        var top = new TableLayoutPanel { Width = 1088, Height = 390, ColumnCount = 2, RowCount = 1, BackColor = _theme.Background, Margin = new Padding(0, 0, 0, 14) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(top);

        var photoCard = Ui.Card(_theme, 16);
        photoCard.Dock = DockStyle.Fill;
        photoCard.Controls.Add(Section("Faculty Photo", 12, 8));
        var image = new PictureBox { Location = new Point(28, 56), Size = new Size(180, 180), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        LoadPicture(image, profile["ProfileImagePath"]?.ToString());
        photoCard.Controls.Add(image);
        AddAction(photoCard, "Upload Photo", 28, 258, () =>
        {
            var path = PickImagePath();
            if (path == null) return;
            var storedPath = SaveImageCopy(path, $"Faculty_{FacultyId}_Profile.jpg", 400);
            _db.Execute("UPDATE Faculty SET ProfileImagePath = $path WHERE FacultyId = $facultyId;", ("$path", storedPath), ("$facultyId", FacultyId));
            LoadPicture(image, storedPath);
            Info("Faculty profile photo updated.");
        });
        AddAction(photoCard, "Remove Photo", 28, 310, () =>
        {
            _db.Execute("UPDATE Faculty SET ProfileImagePath = '' WHERE FacultyId = $facultyId;", ("$facultyId", FacultyId));
            LoadPicture(image, "");
            Info("Faculty profile photo removed.");
        });
        top.Controls.Add(photoCard, 0, 0);

        var details = Ui.Card(_theme, 16);
        details.Dock = DockStyle.Fill;
        details.Controls.Add(Section("Faculty Details", 12, 8));
        var left = new[] { ("Faculty ID", "FacultyCode"), ("Name", "Name"), ("Department", "Department"), ("Designation", "Designation"), ("Email", "Email") };
        var right = new[] { ("Mobile Number", "MobileNumber"), ("Office Room Number", "OfficeRoomNumber"), ("Experience", "Experience"), ("Qualification", "Qualification") };
        AddProfileFields(details, fields, left, profile, 18, 52);
        AddProfileFields(details, fields, right, profile, 410, 52);
        top.Controls.Add(details, 1, 0);

        var bottom = new TableLayoutPanel { Width = 1088, Height = 300, ColumnCount = 2, RowCount = 1, BackColor = _theme.Background, Margin = new Padding(0, 0, 0, 14) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.Controls.Add(bottom);
        bottom.Controls.Add(HomeGridCard("Subjects Handled", """
            SELECT sub.SubjectCode AS 'Subject Code', sub.SubjectName AS 'Subject Name', sec.Semester, sec.SectionName AS Section
            FROM FacultySections fs
            JOIN Subjects sub ON sub.SubjectId = fs.SubjectId
            JOIN Sections sec ON sec.SectionId = fs.SectionId
            WHERE fs.FacultyId = $facultyId
            ORDER BY sec.Semester, sec.SectionName, sub.SubjectName;
            """, 526, 300, ("$facultyId", FacultyId)), 0, 0);
        bottom.Controls.Add(HomeGridCard("Assigned Sections", """
            SELECT DISTINCT sec.SectionName AS Section, sec.Department, sec.Semester, sec.TotalStudents AS 'Total Students'
            FROM FacultySections fs
            JOIN Sections sec ON sec.SectionId = fs.SectionId
            WHERE fs.FacultyId = $facultyId
            ORDER BY sec.Semester, sec.SectionName;
            """, 526, 300, ("$facultyId", FacultyId)), 1, 0);

        var buttons = Ui.Card(_theme, 14);
        buttons.Width = 1088;
        buttons.Height = 76;
        root.Controls.Add(buttons);
        AddAction(buttons, "Edit Profile", 12, 20, () => SetFieldsReadOnly(fields, false));
        AddAction(buttons, "Save Changes", 140, 20, () =>
        {
            SaveFacultyProfile(fields);
            SetFieldsReadOnly(fields, true);
        });
        AddAction(buttons, "Change Password", 290, 20, ShowChangePasswordDialog);
        AddAction(buttons, "Cancel", 465, 20, () =>
        {
            ResetFacultyProfileFields(fields);
            SetFieldsReadOnly(fields, true);
        });
        SetFieldsReadOnly(fields, true);
        return root;
    }

    private Control BuildMySectionsPage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("My Assigned Sections", 16, 16));
        var cards = new FlowLayoutPanel { Location = new Point(16, 58), Size = new Size(1110, 195), BackColor = _theme.Surface, AutoScroll = true };
        panel.Controls.Add(cards);
        var grid = Grid(16, 292, 1110, 330);
        panel.Controls.Add(grid);

        void LoadSectionStudents(int sectionId)
        {
            grid.DataSource = AssignedStudentsTable(sectionId: sectionId);
            HideColumn(grid, "StudentId");
        }

        var rows = _db.GetDataTable("""
            SELECT sec.SectionId, sec.SectionName, sec.Department, sec.Semester, sub.SubjectName,
                   COUNT(DISTINCT st.StudentId) AS TotalStudents,
                   ROUND(IFNULL(AVG(st.AttendancePercentage), 0), 2) AS AverageAttendance,
                   ROUND(IFNULL(AVG(fmr.MarksObtained), 0), 2) AS AverageInternalMarks,
                   COUNT(DISTINCT CASE WHEN st.EvidenceStatus IN ('Pending','Submitted','Rejected') THEN st.StudentId END) AS PendingEvidence
            FROM FacultySections fs
            JOIN Sections sec ON sec.SectionId = fs.SectionId
            JOIN Subjects sub ON sub.SubjectId = fs.SubjectId
            LEFT JOIN Students st ON st.SectionId = sec.SectionId
            LEFT JOIN FacultyMarksRecords fmr ON fmr.StudentId = st.StudentId AND fmr.SectionId = sec.SectionId
            WHERE fs.FacultyId = $facultyId
            GROUP BY sec.SectionId, sec.SectionName, sec.Department, sec.Semester, sub.SubjectName
            ORDER BY sec.Semester, sec.SectionName;
            """, ("$facultyId", FacultyId));

        foreach (DataRow row in rows.Rows)
        {
            var card = SectionCard(row);
            var sectionId = Convert.ToInt32(row["SectionId"]);
            card.Click += (_, _) => LoadSectionStudents(sectionId);
            foreach (Control child in card.Controls) child.Click += (_, _) => LoadSectionStudents(sectionId);
            cards.Controls.Add(card);
        }

        if (rows.Rows.Count > 0) grid.DataSource = AssignedStudentsTable(sectionId: Convert.ToInt32(rows.Rows[0]["SectionId"]));
        HideColumn(grid, "StudentId");
        AddAction(panel, "Bulk Attendance Update", 16, 646, () => LoadPage("Attendance"));
        AddAction(panel, "Bulk Marks Update", 225, 646, () => LoadPage("Marks"));
        AddAction(panel, "Generate Section Report", 390, 646, () => ShowTableDialog("Section Report", SectionReportSql(), ("$facultyId", FacultyId)));
        return panel;
    }

    private Control BuildStudentManagementPage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("Section-wise Student Control", 16, 16));

        var department = UiFactory.Combo("All", "Computer Science and Engineering");
        department.Location = new Point(16, 58); department.Width = 230;
        var year = UiFactory.Combo("All", "1", "2", "3", "4");
        year.Location = new Point(258, 58); year.Width = 75;
        var semester = UiFactory.Combo("All", "1", "2", "3", "4", "5", "6", "7", "8");
        semester.Location = new Point(346, 58); semester.Width = 90;
        var section = SectionCombo();
        section.Location = new Point(448, 58); section.Width = 170;
        var subject = SubjectCombo(includeAll: true);
        subject.Location = new Point(630, 58); subject.Width = 250;
        var search = new TextBox { PlaceholderText = "Register number or student name", Location = new Point(895, 58), Width = 230 };
        panel.Controls.Add(department); panel.Controls.Add(year); panel.Controls.Add(semester); panel.Controls.Add(section); panel.Controls.Add(subject); panel.Controls.Add(search);

        var grid = Grid(16, 112, 1110, 420);
        panel.Controls.Add(grid);

        void Load()
        {
            grid.DataSource = AssignedStudentsTable(
                department: department.Text == "All" ? null : department.Text,
                year: year.Text == "All" ? null : Convert.ToInt32(year.Text),
                semester: semester.Text == "All" ? null : Convert.ToInt32(semester.Text),
                sectionId: SelectedSectionId(section),
                subjectId: SelectedSubjectIdOrNull(subject),
                search: search.Text.Trim());
            HideColumn(grid, "StudentId");
        }

        department.SelectedIndexChanged += (_, _) => Load();
        year.SelectedIndexChanged += (_, _) => Load();
        semester.SelectedIndexChanged += (_, _) => Load();
        section.SelectedIndexChanged += (_, _) => Load();
        subject.SelectedIndexChanged += (_, _) => Load();
        search.TextChanged += (_, _) => Load();
        Load();

        AddAction(panel, "View Profile", 16, 558, () => ViewSelectedStudentProfile(grid));
        AddAction(panel, "Update Image", 140, 558, () => UpdateSelectedStudentImage(grid, Load));
        AddAction(panel, "Update Academic", 275, 558, () => UpdateSelectedStudentAcademic(grid, Load));
        AddAction(panel, "Update Attendance", 435, 558, () => LoadPage("Attendance"));
        AddAction(panel, "Update Marks", 600, 558, () => LoadPage("Marks"));
        AddAction(panel, "All Semester Marks", 735, 558, () => ShowStudentRelatedTable(grid, "All Semester Marks", """
            SELECT fmr.SubjectCode, fmr.Semester, fmr.ExamType, fmr.MaxMarks, fmr.MarksObtained, fmr.Grade, fmr.Remarks
            FROM FacultyMarksRecords fmr
            WHERE fmr.StudentId = $studentId
            ORDER BY fmr.Semester, fmr.SubjectCode, fmr.ExamType;
            """));
        AddAction(panel, "Evidence", 16, 610, () => ShowStudentRelatedTable(grid, "Student Evidence", """
            SELECT EvidenceId, Title, Category, Semester, UploadDate, Status, FacultyComments
            FROM LearningEvidence WHERE StudentId = $studentId ORDER BY UploadDate DESC;
            """));
        AddAction(panel, "Approve Evidence", 120, 610, () => UpdateEvidenceForSelectedStudent(grid, "Approved"));
        AddAction(panel, "Reject Evidence", 280, 610, () => UpdateEvidenceForSelectedStudent(grid, "Rejected"));
        AddAction(panel, "Student Notes", 420, 610, () => ShowStudentRelatedTable(grid, "Student Notes", """
            SELECT NoteId, Title, Semester, UnitNumber, UploadDate, ApprovalStatus
            FROM Notes WHERE StudentId = $studentId ORDER BY UploadDate DESC;
            """));
        AddAction(panel, "Generate Report", 555, 610, () => ViewSelectedStudentProfile(grid));
        return panel;
    }

    private Control BuildAttendancePage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("Attendance Management", 16, 16));
        var section = SectionCombo();
        section.Location = new Point(16, 62); section.Width = 190;
        var subject = SubjectComboForSection(SelectedSectionId(section));
        subject.Location = new Point(220, 62); subject.Width = 260;
        var date = new DateTimePicker { Location = new Point(494, 62), Width = 140, Format = DateTimePickerFormat.Short };
        var search = new TextBox { PlaceholderText = "Search register number or student name", Location = new Point(650, 62), Width = 260 };
        panel.Controls.Add(section); panel.Controls.Add(subject); panel.Controls.Add(date); panel.Controls.Add(search);

        var cardPanel = new FlowLayoutPanel { Location = new Point(16, 108), Size = new Size(1110, 105), BackColor = _theme.Background };
        panel.Controls.Add(cardPanel);
        var grid = Grid(16, 232, 1110, 365);
        grid.ReadOnly = false;
        grid.DataError += (_, _) => { };
        panel.Controls.Add(grid);

        var isLoading = false;
        void Load()
        {
            if (isLoading) return;
            try
            {
                isLoading = true;
                subject.Items.Clear();
                foreach (SubjectEntry item in SubjectRows(SelectedSectionId(section)).Rows.Cast<DataRow>().Select(SubjectFromRow)) subject.Items.Add(item);
                if (subject.Items.Count > 0 && subject.SelectedIndex < 0) subject.SelectedIndex = 0;
                grid.DataSource = AttendanceTable(SelectedSectionId(section), SelectedSubjectId(subject), date.Value, search.Text.Trim());
                HideColumn(grid, "StudentId");
                if (grid.Columns.Contains("Register Number")) grid.Columns["Register Number"].ReadOnly = true;
                if (grid.Columns.Contains("Student Name")) grid.Columns["Student Name"].ReadOnly = true;
                if (grid.Columns.Contains("Section")) grid.Columns["Section"].ReadOnly = true;
                if (grid.Columns.Contains("Subject")) grid.Columns["Subject"].ReadOnly = true;
                if (grid.Columns.Contains("Date")) grid.Columns["Date"].ReadOnly = true;
                ConfigureAttendanceStatusColumn(grid);
                RefreshAttendanceCards(cardPanel, grid);
            }
            finally
            {
                isLoading = false;
            }
        }

        section.SelectedIndexChanged += (_, _) => Load();
        subject.SelectedIndexChanged += (_, _) => Load();
        date.ValueChanged += (_, _) => Load();
        search.TextChanged += (_, _) => Load();
        Load();

        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "Present/Absent") RefreshAttendanceCards(cardPanel, grid);
        };

        AddAction(panel, "Mark All Present", 16, 622, () => SetAttendanceStatus(grid, "Present", cardPanel));
        AddAction(panel, "Mark All Absent", 165, 622, () => SetAttendanceStatus(grid, "Absent", cardPanel));
        AddAction(panel, "Clear Selection", 310, 622, () => SetAttendanceStatus(grid, "Absent", cardPanel));
        AddAction(panel, "Save Attendance", 455, 622, () => SaveAttendance(grid, section, subject, date, Load));
        AddAction(panel, "Update Attendance", 610, 622, () => SaveAttendance(grid, section, subject, date, Load));
        AddAction(panel, "Subject-wise Attendance", 780, 622, () => ShowTableDialog("Subject-wise Attendance", """
            SELECT sec.SectionName, s.SubjectName, fad.AttendanceDate, fad.Status, COUNT(*) AS Students
            FROM FacultyAttendanceDaily fad
            JOIN Sections sec ON sec.SectionId = fad.SectionId
            JOIN Subjects s ON s.SubjectId = fad.SubjectId
            WHERE fad.FacultyId = $facultyId
            GROUP BY sec.SectionName, s.SubjectName, fad.AttendanceDate, fad.Status
            ORDER BY fad.AttendanceDate DESC;
            """, ("$facultyId", FacultyId)));
        AddAction(panel, "Shortage List", 1000, 622, () => ShowTableDialog("Attendance Shortage List", ShortageSql(), ("$facultyId", FacultyId)));
        return panel;
    }

    private Control BuildMarksPage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("Marks Management", 16, 16));
        var section = SectionCombo();
        section.Location = new Point(16, 62); section.Width = 190;
        var subject = SubjectComboForSection(SelectedSectionId(section));
        subject.Location = new Point(220, 62); subject.Width = 260;
        var exam = UiFactory.Combo("Internal 1", "Internal 2", "Assignment", "Quiz", "Lab", "Semester Exam");
        exam.Location = new Point(494, 62); exam.Width = 170;
        panel.Controls.Add(section); panel.Controls.Add(subject); panel.Controls.Add(exam);

        var grid = Grid(16, 116, 1110, 465);
        grid.ReadOnly = false;
        panel.Controls.Add(grid);

        var isLoading = false;
        void Load()
        {
            if (isLoading) return;
            try
            {
                isLoading = true;
                subject.Items.Clear();
                foreach (SubjectEntry item in SubjectRows(SelectedSectionId(section)).Rows.Cast<DataRow>().Select(SubjectFromRow)) subject.Items.Add(item);
                if (subject.Items.Count > 0 && subject.SelectedIndex < 0) subject.SelectedIndex = 0;
                grid.DataSource = MarksTable(SelectedSectionId(section), SelectedSubjectCode(subject), exam.Text);
                HideColumn(grid, "StudentId");
                foreach (var name in new[] { "Register Number", "Student Name", "Grade" })
                {
                    if (grid.Columns.Contains(name)) grid.Columns[name].ReadOnly = true;
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        section.SelectedIndexChanged += (_, _) => Load();
        subject.SelectedIndexChanged += (_, _) => Load();
        exam.SelectedIndexChanged += (_, _) => Load();
        Load();

        AddAction(panel, "Save Marks", 16, 612, () => SaveMarks(grid, section, subject, exam, Load));
        AddAction(panel, "Update Marks", 130, 612, () => SaveMarks(grid, section, subject, exam, Load));
        AddAction(panel, "Semester-wise Marks", 260, 612, () => ShowTableDialog("Semester-wise Marks", MarksReportSql(), ("$facultyId", FacultyId)));
        AddAction(panel, "Internal Marks", 440, 612, () => ShowTableDialog("Internal Marks", InternalMarksSql(), ("$facultyId", FacultyId)));
        AddAction(panel, "Generate Marks Report", 585, 612, () => ShowTableDialog("Marks Report", MarksReportSql(), ("$facultyId", FacultyId)));
        return panel;
    }

    private Control BuildEvidenceValidationPage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("Evidence Validation", 16, 16));
        var status = UiFactory.Combo("All", "Pending", "Submitted", "Resubmitted", "Approved", "Rejected");
        status.Location = new Point(16, 62);
        var semester = UiFactory.Combo("All", "1", "2", "3", "4", "5", "6", "7", "8");
        semester.Location = new Point(210, 62); semester.Width = 90;
        var category = UiFactory.Combo("All", "Assignments", "Lab records", "Mini projects", "Major projects", "Certifications", "Workshop certificates", "Hackathon participation");
        category.Location = new Point(315, 62); category.Width = 230;
        panel.Controls.Add(status); panel.Controls.Add(semester); panel.Controls.Add(category);

        var cards = new FlowLayoutPanel { Location = new Point(16, 105), Size = new Size(1110, 96), BackColor = _theme.Background };
        panel.Controls.Add(cards);
        var grid = Grid(16, 220, 1110, 340);
        panel.Controls.Add(grid);

        void Load()
        {
            var statusFilter = status.Text == "All" ? "" : "AND le.Status = $status";
            var semesterFilter = semester.Text == "All" ? "" : "AND le.Semester = $semester";
            var categoryFilter = category.Text == "All" ? "" : "AND le.Category = $category";
            grid.DataSource = _db.GetDataTable($"""
                SELECT le.EvidenceId, COALESCE(sp.FullName, st.Name) AS StudentName, COALESCE(sp.RegisterNumber, st.RegisterNumber) AS RegisterNumber,
                       le.Category, le.Title, s.SubjectName AS Subject, le.Semester, le.UploadDate, le.Status, le.FacultyComments
                FROM LearningEvidence le
                JOIN Subjects s ON s.SubjectId = le.SubjectId
                LEFT JOIN StudentProfile sp ON sp.StudentId = le.StudentId
                LEFT JOIN Students st ON st.StudentId = le.StudentId
                WHERE EXISTS (
                    SELECT 1
                    FROM Students assigned
                    JOIN FacultySections fs ON fs.SectionId = assigned.SectionId
                    WHERE assigned.StudentId = le.StudentId AND fs.FacultyId = $facultyId
                ) {statusFilter} {semesterFilter} {categoryFilter}
                ORDER BY le.UploadDate DESC;
                """, ("$facultyId", FacultyId), ("$status", status.Text), ("$semester", semester.Text), ("$category", category.Text));
            cards.Controls.Clear();
            cards.Controls.Add(DashboardCard("Total Evidence", Count(AssignedEvidenceCountSql("1 = 1"), ("$facultyId", FacultyId)), _theme.Primary, 160));
            cards.Controls.Add(DashboardCard("Pending Evidence", Count(AssignedEvidenceCountSql("le.Status IN ('Pending','Submitted','Resubmitted')"), ("$facultyId", FacultyId)), _theme.Warning, 170));
            cards.Controls.Add(DashboardCard("Approved Evidence", Count(AssignedEvidenceCountSql("le.Status = 'Approved'"), ("$facultyId", FacultyId)), _theme.Success, 170));
            cards.Controls.Add(DashboardCard("Rejected Evidence", Count(AssignedEvidenceCountSql("le.Status = 'Rejected'"), ("$facultyId", FacultyId)), _theme.Danger, 170));
        }

        status.SelectedIndexChanged += (_, _) => Load();
        semester.SelectedIndexChanged += (_, _) => Load();
        category.SelectedIndexChanged += (_, _) => Load();
        Load();

        AddAction(panel, "View Evidence", 16, 590, () => ViewSelectedEvidence(grid));
        AddAction(panel, "Approve", 140, 590, () => UpdateSelectedEvidence(grid, "Approved", Load));
        AddAction(panel, "Reject", 240, 590, () => UpdateSelectedEvidence(grid, "Rejected", Load));
        AddAction(panel, "Add Comments", 330, 590, () => AddEvidenceComment(grid, Load));
        return panel;
    }

    private Control BuildNotesManagementPage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("Notes Management", 16, 16));
        var title = new TextBox { PlaceholderText = "Faculty note title", Location = new Point(16, 62), Width = 240 };
        var subject = SubjectCombo(); subject.Location = new Point(272, 62);
        var semester = UiFactory.Combo("1", "2", "3", "4", "5", "6", "7", "8"); semester.Location = new Point(540, 62); semester.Width = 80; semester.Text = "4";
        var unit = UiFactory.Combo("1", "2", "3", "4", "5", "6"); unit.Location = new Point(635, 62); unit.Width = 80;
        var path = new TextBox { PlaceholderText = "File path", Location = new Point(730, 62), Width = 260 };
        panel.Controls.Add(title); panel.Controls.Add(subject); panel.Controls.Add(semester); panel.Controls.Add(unit); panel.Controls.Add(path);
        var grid = Grid(16, 130, 1110, 430);
        panel.Controls.Add(grid);

        void Load()
        {
            grid.DataSource = _db.GetDataTable("""
                SELECT n.NoteId, n.Title, s.SubjectName AS Subject, n.Semester, n.UnitNumber, n.UploadedByRole AS 'Uploaded By',
                       n.UploadDate, n.ApprovalStatus
                FROM Notes n JOIN Subjects s ON s.SubjectId = n.SubjectId
                ORDER BY n.UploadDate DESC;
                """);
            HideColumn(grid, "NoteId");
        }
        Load();

        AddAction(panel, "Upload Notes", 16, 590, () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title.Text)) throw new ArgumentException("Enter a notes title.");
                if (SelectedSubjectId(subject) <= 0) throw new ArgumentException("Select a subject.");
                var filePath = string.IsNullOrWhiteSpace(path.Text) ? "Faculty notes shared in class" : path.Text.Trim();
                _db.Execute("""
                    INSERT INTO Notes
                        (StudentId, FacultyId, UploadedByRole, Title, SubjectId, Semester, UnitNumber, Description, FilePath,
                         UploadDate, ApprovalStatus, ApprovedBy, FacultyComments, LikesCount, IsBookmarked, ReportStatus)
                    VALUES
                        (NULL, $facultyId, 'Faculty', $title, $subjectId, $semester, $unit, 'Faculty uploaded notes', $path,
                         date('now'), 'Approved', 'Faculty', 'Faculty material', 0, 0, 'None');
                    """, ("$facultyId", FacultyId), ("$title", title.Text.Trim()), ("$subjectId", SelectedSubjectId(subject)), ("$semester", semester.Text), ("$unit", unit.Text), ("$path", filePath));
                Load();
                Info("Faculty notes uploaded.");
            }
            catch (Exception ex) { Error(ex.Message); }
        });
        AddAction(panel, "View Notes", 145, 590, () => ViewSelectedNote(grid));
        AddAction(panel, "Approve Student Notes", 260, 590, () => UpdateSelectedNote(grid, "Approved", Load));
        AddAction(panel, "Reject Student Notes", 460, 590, () => UpdateSelectedNote(grid, "Rejected", Load));
        AddAction(panel, "Delete Notes", 635, 590, () => DeleteSelectedNote(grid, Load));
        return panel;
    }

    private Control BuildCampusEventsPage()
    {
        var panel = PagePanel();
        panel.Controls.Add(Section("Campus Events Management", 16, 16));
        var title = new TextBox { PlaceholderText = "Event title", Location = new Point(16, 62), Width = 190 };
        var type = UiFactory.Combo("Technical", "Cultural", "Sports", "Workshop", "Seminar"); type.Location = new Point(220, 62); type.Width = 140;
        var by = new TextBox { PlaceholderText = "Conducted by", Location = new Point(374, 62), Width = 170, Text = "CSE Department" };
        var date = new DateTimePicker { Location = new Point(558, 62), Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(5) };
        var time = new TextBox { PlaceholderText = "Time", Location = new Point(692, 62), Width = 80, Text = "10:00 AM" };
        var venue = new TextBox { PlaceholderText = "Venue", Location = new Point(786, 62), Width = 140 };
        var deadline = new DateTimePicker { Location = new Point(940, 62), Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(3) };
        var desc = new TextBox { PlaceholderText = "Description", Location = new Point(16, 100), Width = 1044 };
        panel.Controls.Add(title); panel.Controls.Add(type); panel.Controls.Add(by); panel.Controls.Add(date); panel.Controls.Add(time); panel.Controls.Add(venue); panel.Controls.Add(deadline); panel.Controls.Add(desc);
        var grid = Grid(16, 160, 1110, 400);
        panel.Controls.Add(grid);

        void Load()
        {
            grid.DataSource = _db.GetDataTable("""
                SELECT ce.EventId, ce.EventTitle AS Title, ce.EventType AS Type, ce.EventDate AS Date, ce.Venue,
                       COUNT(er.RegistrationId) AS RegisteredStudents,
                       CASE WHEN ce.EventDate >= date('now') THEN 'Upcoming' ELSE 'Completed' END AS Status
                FROM CampusEvents ce
                LEFT JOIN EventRegistrations er ON er.EventId = ce.EventId
                GROUP BY ce.EventId
                ORDER BY ce.EventDate DESC;
                """);
            HideColumn(grid, "EventId");
        }
        Load();

        AddAction(panel, "Add Event", 16, 590, () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title.Text)) throw new ArgumentException("Enter an event title.");
                if (string.IsNullOrWhiteSpace(venue.Text)) throw new ArgumentException("Enter an event venue.");
                _db.Execute("""
                    INSERT INTO CampusEvents (EventTitle, EventDate, Venue, ConductedBy, EventDescription, RegistrationDeadline, EventType)
                    VALUES ($title, $date, $venue, $by, $description, $deadline, $type);
                    """, ("$title", title.Text.Trim()), ("$date", date.Value.ToString("yyyy-MM-dd")), ("$venue", venue.Text.Trim()),
                    ("$by", by.Text.Trim()), ("$description", $"{time.Text} - {desc.Text}".Trim()), ("$deadline", deadline.Value.ToString("yyyy-MM-dd")), ("$type", type.Text));
                Load();
                Info("Campus event added.");
            }
            catch (Exception ex) { Error(ex.Message); }
        });
        AddAction(panel, "Edit Event", 125, 590, () => UpdateSelectedEvent(grid, title, type, by, date, time, venue, deadline, desc, Load));
        AddAction(panel, "Delete Event", 235, 590, () => DeleteSelectedEvent(grid, Load));
        AddAction(panel, "View Registered Students", 360, 590, () => ShowTableDialog("Registered Students", """
            SELECT sp.RegisterNumber, sp.FullName, er.RegistrationDate, er.Status
            FROM EventRegistrations er JOIN StudentProfile sp ON sp.StudentId = er.StudentId;
            """));
        return panel;
    }

    private DataTable AssignedStudentsTable(string? department = null, int? year = null, int? semester = null, int? sectionId = null, int? subjectId = null, string? search = null)
    {
        var departmentFilter = department == null ? "" : "AND st.Department = $department";
        var yearFilter = year == null ? "" : "AND st.Year = $year";
        var semesterFilter = semester == null ? "" : "AND st.Semester = $semester";
        var sectionFilter = sectionId == null ? "" : "AND st.SectionId = $sectionId";
        var subjectFilter = subjectId == null ? "" : "AND fs.SubjectId = $subjectId";
        return _db.GetDataTable($"""
            SELECT st.StudentId, st.RegisterNumber AS 'Register Number', st.Name AS 'Student Name', st.Department,
                   st.Year, st.Semester, sec.SectionName AS Section, st.Email, st.Mobile AS 'Mobile Number',
                   st.AttendancePercentage AS 'Attendance %', ROUND(IFNULL(AVG(fmr.MarksObtained), 0), 2) AS 'Internal Marks',
                   st.EvidenceStatus AS 'Evidence Status', st.ProfileImagePath AS 'Profile Image'
            FROM Students st
            JOIN Sections sec ON sec.SectionId = st.SectionId
            JOIN FacultySections fs ON fs.SectionId = st.SectionId AND fs.FacultyId = $facultyId
            LEFT JOIN FacultyMarksRecords fmr ON fmr.StudentId = st.StudentId AND fmr.SectionId = st.SectionId
            WHERE ($search = '' OR lower(st.RegisterNumber || ' ' || st.Name) LIKE '%' || lower($search) || '%')
            {departmentFilter} {yearFilter} {semesterFilter} {sectionFilter} {subjectFilter}
            GROUP BY st.StudentId
            ORDER BY sec.Semester, sec.SectionName, st.RegisterNumber;
            """,
            ("$facultyId", FacultyId), ("$search", search ?? ""), ("$department", department ?? ""), ("$year", year ?? 0),
            ("$semester", semester ?? 0), ("$sectionId", sectionId ?? 0), ("$subjectId", subjectId ?? 0));
    }

    private DataTable AttendanceTable(int? sectionId, int subjectId, DateTime date, string search)
    {
        return _db.GetDataTable("""
            SELECT st.StudentId, st.RegisterNumber AS 'Register Number', st.Name AS 'Student Name',
                   sec.SectionName AS Section, sub.SubjectName AS Subject, date($date) AS Date,
                   CASE WHEN fad.Status = 'Present' THEN 'Present' ELSE 'Absent' END AS 'Present/Absent'
            FROM Students st
            JOIN Sections sec ON sec.SectionId = st.SectionId
            JOIN FacultySections fs ON fs.SectionId = st.SectionId
                AND fs.FacultyId = $facultyId
                AND fs.SubjectId = $subjectId
            JOIN Subjects sub ON sub.SubjectId = fs.SubjectId
            LEFT JOIN FacultyAttendanceDaily fad ON fad.StudentId = st.StudentId
                AND fad.FacultyId = $facultyId
                AND fad.SubjectId = $subjectId
                AND fad.SectionId = st.SectionId
                AND fad.AttendanceDate = date($date)
            WHERE st.SectionId = $sectionId
                AND ($search = '' OR lower(st.RegisterNumber || ' ' || st.Name) LIKE '%' || lower($search) || '%')
            ORDER BY st.RegisterNumber;
            """, ("$facultyId", FacultyId), ("$sectionId", sectionId ?? 0), ("$subjectId", subjectId),
            ("$date", date.ToString("yyyy-MM-dd")), ("$search", search));
    }

    private DataTable MarksTable(int? sectionId, string subjectCode, string examType)
    {
        return _db.GetDataTable("""
            SELECT st.StudentId, st.RegisterNumber AS 'Register Number', st.Name AS 'Student Name',
                   COALESCE(fmr.MaxMarks, 100) AS 'Max Marks',
                   COALESCE(fmr.MarksObtained, 0) AS 'Marks Obtained',
                   COALESCE(fmr.Grade, '') AS Grade,
                   COALESCE(fmr.Remarks, '') AS Remarks
            FROM Students st
            JOIN FacultySections fs ON fs.SectionId = st.SectionId AND fs.FacultyId = $facultyId
            LEFT JOIN FacultyMarksRecords fmr ON fmr.StudentId = st.StudentId
                AND fmr.SubjectCode = $subjectCode
                AND fmr.ExamType = $examType
            WHERE st.SectionId = $sectionId
            ORDER BY st.RegisterNumber;
            """, ("$facultyId", FacultyId), ("$sectionId", sectionId ?? 0), ("$subjectCode", subjectCode), ("$examType", examType));
    }

    private void SaveAttendance(DataGridView grid, ComboBox section, ComboBox subject, DateTimePicker date, Action reload)
    {
        try
        {
            var sectionId = SelectedSectionId(section) ?? 0;
            var subjectId = SelectedSubjectId(subject);
            var subjectCode = SelectedSubjectCode(subject);
            if (sectionId <= 0) throw new ArgumentException("Select a section.");
            if (subjectId <= 0 || string.IsNullOrWhiteSpace(subjectCode)) throw new ArgumentException("Select a subject.");
            var semester = SectionRows().Rows.Cast<DataRow>().First(r => Convert.ToInt32(r["SectionId"]) == sectionId)["Semester"];
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var status = row.Cells["Present/Absent"].Value?.ToString();
                if (status is not ("Present" or "Absent")) status = "Absent";
                _db.Execute("""
                    INSERT INTO FacultyAttendanceDaily
                        (FacultyId, StudentId, SubjectCode, SubjectId, SectionId, Semester, AttendanceDate, Status)
                    VALUES
                        ($facultyId, $studentId, $subjectCode, $subjectId, $sectionId, $semester, $date, $status)
                    ON CONFLICT(StudentId, SubjectCode, AttendanceDate) DO UPDATE SET
                        FacultyId = excluded.FacultyId,
                        SubjectCode = excluded.SubjectCode,
                        SubjectId = excluded.SubjectId,
                        SectionId = excluded.SectionId,
                        Semester = excluded.Semester,
                        Status = excluded.Status;
                    """, ("$facultyId", FacultyId), ("$studentId", Convert.ToInt32(row.Cells["StudentId"].Value)),
                    ("$subjectCode", subjectCode), ("$subjectId", subjectId), ("$sectionId", sectionId),
                    ("$semester", semester), ("$date", date.Value.ToString("yyyy-MM-dd")), ("$status", status));
            }
            reload();
            Info("Attendance saved to SQLite.");
        }
        catch (Exception ex) { Error(ex.Message); }
    }

    private void SaveMarks(DataGridView grid, ComboBox section, ComboBox subject, ComboBox exam, Action reload)
    {
        try
        {
            var sectionId = SelectedSectionId(section) ?? 0;
            var subjectId = SelectedSubjectId(subject);
            var subjectCode = SelectedSubjectCode(subject);
            if (sectionId <= 0) throw new ArgumentException("Select a section.");
            if (subjectId <= 0 || string.IsNullOrWhiteSpace(subjectCode)) throw new ArgumentException("Select a subject.");
            var semester = SectionRows().Rows.Cast<DataRow>().First(r => Convert.ToInt32(r["SectionId"]) == sectionId)["Semester"];
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var maxMarks = Convert.ToInt32(row.Cells["Max Marks"].Value ?? 100);
                var marks = Convert.ToDouble(row.Cells["Marks Obtained"].Value ?? 0);
                if (maxMarks <= 0) throw new ArgumentException("Max marks must be greater than zero.");
                if (marks < 0 || marks > maxMarks) throw new ArgumentException($"Marks must be between 0 and {maxMarks}.");
                var grade = GradeFor(marks, maxMarks);
                var remarks = row.Cells["Remarks"].Value?.ToString() ?? "";
                _db.Execute("""
                    INSERT INTO FacultyMarksRecords
                        (FacultyId, StudentId, SubjectCode, SubjectId, SectionId, Semester, ExamType, MaxMarks, MarksObtained, Grade, Remarks)
                    VALUES
                        ($facultyId, $studentId, $subjectCode, $subjectId, $sectionId, $semester, $examType, $maxMarks, $marks, $grade, $remarks)
                    ON CONFLICT(StudentId, SubjectCode, Semester, ExamType) DO UPDATE SET
                        FacultyId = excluded.FacultyId,
                        SubjectId = excluded.SubjectId,
                        SectionId = excluded.SectionId,
                        MaxMarks = excluded.MaxMarks,
                        MarksObtained = excluded.MarksObtained,
                        Grade = excluded.Grade,
                        Remarks = excluded.Remarks;
                    """, ("$facultyId", FacultyId), ("$studentId", Convert.ToInt32(row.Cells["StudentId"].Value)),
                    ("$subjectCode", subjectCode), ("$subjectId", subjectId), ("$sectionId", sectionId), ("$semester", semester),
                    ("$examType", exam.Text), ("$maxMarks", maxMarks), ("$marks", marks), ("$grade", grade), ("$remarks", remarks));
            }
            reload();
            Info("Marks saved to SQLite.");
        }
        catch (Exception ex) { Error(ex.Message); }
    }

    private void ViewSelectedStudentProfile(DataGridView grid)
    {
        var id = SelectedStudentId(grid);
        if (id == null) return;
        var table = _db.GetDataTable("""
            SELECT st.StudentId, st.RegisterNumber, st.Name, st.Department, st.Year, st.Semester,
                   sec.SectionName AS Section, st.Email, st.Mobile, st.AttendancePercentage,
                   st.CGPA, st.EvidenceStatus, st.ProfileImagePath,
                   ROUND(IFNULL(AVG(fmr.MarksObtained), 0), 2) AS InternalMarks,
                   COUNT(DISTINCT le.EvidenceId) AS EvidenceCount,
                   IFNULL(SUM(CASE WHEN le.Status = 'Approved' THEN 1 ELSE 0 END), 0) AS ApprovedEvidence,
                   IFNULL(SUM(CASE WHEN le.Status IN ('Pending','Submitted','Resubmitted') THEN 1 ELSE 0 END), 0) AS PendingEvidence
            FROM Students st
            JOIN Sections sec ON sec.SectionId = st.SectionId
            JOIN FacultySections fs ON fs.SectionId = st.SectionId AND fs.FacultyId = $facultyId
            LEFT JOIN FacultyMarksRecords fmr ON fmr.StudentId = st.StudentId AND fmr.FacultyId = $facultyId
            LEFT JOIN LearningEvidence le ON le.StudentId = st.StudentId AND le.FacultyId = $facultyId
            WHERE st.StudentId = $studentId
            GROUP BY st.StudentId;
            """, ("$studentId", id.Value), ("$facultyId", FacultyId));
        if (table.Rows.Count == 0)
        {
            Error("Student profile was not found or is outside your assigned sections.");
            return;
        }

        ShowStudentProfileDialog(table.Rows[0]);
    }

    private void UpdateSelectedStudentImage(DataGridView grid, Action reload)
    {
        var id = SelectedStudentId(grid);
        if (id == null) return;
        var path = PickImagePath();
        if (path == null) return;
        using var preview = new Form { Text = "Student Image Preview", Size = new Size(360, 420), StartPosition = FormStartPosition.CenterParent };
        var picture = new PictureBox { Dock = DockStyle.Top, Height = 300, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
        LoadPicture(picture, path);
        var save = UiFactory.PrimaryButton("Save Image", _theme);
        save.Location = new Point(54, 325);
        save.Click += (_, _) =>
        {
            var storedPath = SaveImageCopy(path, $"Student_{id.Value}_Profile.jpg", 300);
            _db.Execute("UPDATE Students SET ProfileImagePath = $path WHERE StudentId = $studentId;", ("$path", storedPath), ("$studentId", id.Value));
            preview.Close();
            reload();
            Info("Student profile image updated.");
        };
        var remove = UiFactory.PrimaryButton("Remove Image", _theme);
        remove.Location = new Point(178, 325);
        remove.Click += (_, _) =>
        {
            _db.Execute("UPDATE Students SET ProfileImagePath = '' WHERE StudentId = $studentId;", ("$studentId", id.Value));
            preview.Close();
            reload();
            Info("Student profile image removed.");
        };
        preview.Controls.Add(picture);
        preview.Controls.Add(save);
        preview.Controls.Add(remove);
        preview.ShowDialog(this);
    }

    private void ShowStudentProfileDialog(DataRow student)
    {
        using var dialog = new Form
        {
            Text = $"Student Profile - {Cell(student, "Name")}",
            Size = new Size(1030, 720),
            MinimumSize = new Size(960, 640),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _theme.Background,
            Font = Ui.BodyFont
        };

        var root = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _theme.Background, Padding = new Padding(22) };
        dialog.Controls.Add(root);

        var header = Ui.Card(_theme, 16);
        header.Location = new Point(22, 18);
        header.Size = new Size(930, 70);
        header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        header.Controls.Add(new Label
        {
            Text = "Student Profile",
            Font = Ui.TitleFont,
            ForeColor = _theme.Text,
            BackColor = Color.Transparent,
            Location = new Point(14, 14),
            Size = new Size(360, 36)
        });
        header.Controls.Add(new Label
        {
            Text = $"{Cell(student, "RegisterNumber")}  |  {Cell(student, "Section")}",
            Font = Ui.BodyFont,
            ForeColor = _theme.MutedText,
            BackColor = Color.Transparent,
            Location = new Point(520, 22),
            Size = new Size(360, 28),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        });
        root.Controls.Add(header);

        var photoCard = Ui.Card(_theme, 16);
        photoCard.Location = new Point(22, 108);
        photoCard.Size = new Size(240, 300);
        photoCard.Controls.Add(Section("Student Photo", 12, 8));
        var image = new PictureBox
        {
            Location = new Point(30, 58),
            Size = new Size(180, 180),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };
        LoadPicture(image, Cell(student, "ProfileImagePath"));
        photoCard.Controls.Add(image);
        photoCard.Controls.Add(new Label
        {
            Text = Cell(student, "Name"),
            Font = Ui.HeadingFont,
            ForeColor = _theme.Text,
            BackColor = Color.Transparent,
            Location = new Point(18, 250),
            Size = new Size(200, 30),
            TextAlign = ContentAlignment.MiddleCenter
        });
        root.Controls.Add(photoCard);

        var details = Ui.Card(_theme, 16);
        details.Location = new Point(282, 108);
        details.Size = new Size(670, 300);
        details.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        details.Controls.Add(Section("Student Personal Details", 12, 8));
        AddReadOnlyField(details, "Register Number", Cell(student, "RegisterNumber"), 18, 52, 290);
        AddReadOnlyField(details, "Student Name", Cell(student, "Name"), 18, 112, 290);
        AddReadOnlyField(details, "Email", Cell(student, "Email"), 18, 172, 290);
        AddReadOnlyField(details, "Mobile Number", Cell(student, "Mobile"), 18, 232, 290);
        AddReadOnlyField(details, "Department", Cell(student, "Department"), 350, 52, 290);
        AddReadOnlyField(details, "Year", Cell(student, "Year"), 350, 112, 130);
        AddReadOnlyField(details, "Semester", Cell(student, "Semester"), 510, 112, 130);
        AddReadOnlyField(details, "Section", Cell(student, "Section"), 350, 172, 290);
        AddReadOnlyField(details, "CGPA", Cell(student, "CGPA"), 350, 232, 130);
        root.Controls.Add(details);

        var academic = Ui.Card(_theme, 16);
        academic.Location = new Point(22, 430);
        academic.Size = new Size(930, 150);
        academic.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        academic.Controls.Add(Section("Academic Details", 12, 8));
        AddReadOnlyField(academic, "Attendance %", $"{Cell(student, "AttendancePercentage")}%", 18, 52, 150);
        AddReadOnlyField(academic, "Internal Marks", Cell(student, "InternalMarks"), 195, 52, 150);
        AddReadOnlyField(academic, "Evidence Status", Cell(student, "EvidenceStatus"), 372, 52, 170);
        AddReadOnlyField(academic, "Total Evidence", Cell(student, "EvidenceCount"), 568, 52, 140);
        AddReadOnlyField(academic, "Approved / Pending", $"{Cell(student, "ApprovedEvidence")} / {Cell(student, "PendingEvidence")}", 732, 52, 160);
        var attendance = new ProgressBar
        {
            Location = new Point(18, 112),
            Size = new Size(875, 18),
            Minimum = 0,
            Maximum = 100,
            Value = Math.Clamp(Convert.ToInt32(Math.Round(ToDouble(Cell(student, "AttendancePercentage")))), 0, 100)
        };
        academic.Controls.Add(attendance);
        root.Controls.Add(academic);

        var lower = new TableLayoutPanel { Location = new Point(22, 600), Size = new Size(930, 210), ColumnCount = 2, RowCount = 1, BackColor = _theme.Background };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        lower.Controls.Add(HomeGridCard("Recent Marks", """
            SELECT SubjectCode AS Subject, ExamType AS Exam, MaxMarks AS 'Max Marks',
                   MarksObtained AS Marks, Grade, Remarks
            FROM FacultyMarksRecords
            WHERE StudentId = $studentId AND FacultyId = $facultyId
            ORDER BY FacultyMarkId DESC
            LIMIT 6;
            """, 440, 210, ("$studentId", Convert.ToInt32(student["StudentId"])), ("$facultyId", FacultyId)), 0, 0);
        lower.Controls.Add(HomeGridCard("Evidence Summary", """
            SELECT Title, Category, Status, UploadDate AS Date
            FROM LearningEvidence
            WHERE StudentId = $studentId AND FacultyId = $facultyId
            ORDER BY UploadDate DESC
            LIMIT 6;
            """, 440, 210, ("$studentId", Convert.ToInt32(student["StudentId"])), ("$facultyId", FacultyId)), 1, 0);
        root.Controls.Add(lower);

        var close = UiFactory.PrimaryButton("Close", _theme);
        close.Location = new Point(812, 825);
        close.Width = 120;
        close.Click += (_, _) => dialog.Close();
        root.Controls.Add(close);

        dialog.ShowDialog(this);
    }

    private void AddReadOnlyField(Control parent, string label, string value, int x, int y, int width)
    {
        var fieldLabel = UiFactory.SmallLabel(label, _theme);
        fieldLabel.Location = new Point(x, y);
        parent.Controls.Add(fieldLabel);
        var box = new TextBox
        {
            Location = new Point(x, y + 22),
            Width = width,
            Height = 28,
            Text = value,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Ui.BodyFont
        };
        parent.Controls.Add(box);
    }

    private static string Cell(DataRow row, string column) =>
        row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column]?.ToString() ?? "" : "";

    private static double ToDouble(string value) => double.TryParse(value, out var parsed) ? parsed : 0;

    private void UpdateSelectedStudentAcademic(DataGridView grid, Action reload)
    {
        var id = SelectedStudentId(grid);
        if (id == null) return;
        var row = _db.GetDataTable("SELECT * FROM Students WHERE StudentId = $studentId;", ("$studentId", id.Value)).Rows[0];
        using var dialog = new Form { Text = "Update Student Academic Details", Size = new Size(420, 320), StartPosition = FormStartPosition.CenterParent };
        var year = UiFactory.Combo("1", "2", "3", "4"); year.Text = row["Year"].ToString();
        year.Location = new Point(170, 30);
        var semester = UiFactory.Combo("1", "2", "3", "4", "5", "6", "7", "8"); semester.Text = row["Semester"].ToString();
        semester.Location = new Point(170, 78);
        var section = SectionCombo(); section.Location = new Point(170, 126);
        SelectSection(section, Convert.ToInt32(row["SectionId"]));
        dialog.Controls.Add(new Label { Text = "Year", Location = new Point(30, 34), Width = 120 });
        dialog.Controls.Add(year);
        dialog.Controls.Add(new Label { Text = "Semester", Location = new Point(30, 82), Width = 120 });
        dialog.Controls.Add(semester);
        dialog.Controls.Add(new Label { Text = "Assigned Section", Location = new Point(30, 130), Width = 120 });
        dialog.Controls.Add(section);
        var save = UiFactory.PrimaryButton("Save Academic Details", _theme);
        save.Location = new Point(110, 205);
        save.Click += (_, _) =>
        {
            var selectedSectionId = SelectedSectionId(section) ?? Convert.ToInt32(row["SectionId"]);
            var selectedSection = SectionRows().Rows.Cast<DataRow>().First(r => Convert.ToInt32(r["SectionId"]) == selectedSectionId);
            _db.Execute("""
                UPDATE Students
                SET Year = $year, Semester = $semester, SectionId = $sectionId, Section = $section
                WHERE StudentId = $studentId;
                """, ("$year", year.Text), ("$semester", semester.Text), ("$sectionId", selectedSectionId),
                ("$section", selectedSection["SectionName"].ToString()), ("$studentId", id.Value));
            dialog.Close();
            reload();
            Info("Student academic details updated.");
        };
        dialog.Controls.Add(save);
        dialog.ShowDialog(this);
    }

    private void ShowStudentRelatedTable(DataGridView grid, string title, string sql)
    {
        var id = SelectedStudentId(grid);
        if (id == null) return;
        ShowTableDialog(title, sql, ("$studentId", id.Value));
    }

    private void UpdateEvidenceForSelectedStudent(DataGridView grid, string status)
    {
        var id = SelectedStudentId(grid);
        if (id == null) return;
        _db.Execute("""
            UPDATE LearningEvidence
            SET Status = $status, ValidationStatus = $status, FacultyComments = $comments, LastUpdatedDate = date('now')
            WHERE StudentId = $studentId AND Status IN ('Pending','Submitted','Resubmitted','Rejected');
            """, ("$status", status), ("$comments", status == "Approved" ? "Approved by faculty." : "Please resubmit with corrected proof."),
            ("$studentId", id.Value));
        Info($"Evidence records for selected student marked {status}.");
    }

    private void UpdateSelectedEvidence(DataGridView grid, string status, Action reload)
    {
        var id = SelectedGridId(grid, "EvidenceId");
        if (id == null) return;
        _db.Execute("""
            UPDATE LearningEvidence
            SET Status = $status, ValidationStatus = $status, FacultyComments = $comments, LastUpdatedDate = date('now')
            WHERE EvidenceId = $evidenceId;
            """, ("$status", status), ("$comments", status == "Approved" ? "Approved by faculty." : "Rejected. Resubmission required."),
            ("$evidenceId", id.Value));
        reload();
        Info($"Evidence {status.ToLower()}.");
    }

    private void ViewSelectedEvidence(DataGridView grid)
    {
        var id = SelectedGridId(grid, "EvidenceId");
        if (id == null) return;
        ShowTableDialog("Evidence Details", """
            SELECT le.EvidenceId, COALESCE(sp.FullName, st.Name) AS StudentName,
                   COALESCE(sp.RegisterNumber, st.RegisterNumber) AS RegisterNumber,
                   le.Title, le.Category, s.SubjectName AS Subject, le.Semester,
                   le.Description, le.LearningOutcome, le.SkillsGained, le.FilePath,
                   le.UploadDate, le.Status, le.FacultyComments
            FROM LearningEvidence le
            JOIN Subjects s ON s.SubjectId = le.SubjectId
            LEFT JOIN StudentProfile sp ON sp.StudentId = le.StudentId
            LEFT JOIN Students st ON st.StudentId = le.StudentId
            WHERE le.EvidenceId = $evidenceId;
            """, ("$evidenceId", id.Value));
    }

    private void AddEvidenceComment(DataGridView grid, Action reload)
    {
        var id = SelectedGridId(grid, "EvidenceId");
        if (id == null) return;
        using var dialog = new Form { Text = "Faculty Comments", Size = new Size(430, 230), StartPosition = FormStartPosition.CenterParent };
        var comments = new TextBox { Location = new Point(20, 20), Size = new Size(370, 95), Multiline = true };
        var save = UiFactory.PrimaryButton("Save Comments", _theme);
        save.Location = new Point(135, 135);
        save.Click += (_, _) =>
        {
            _db.Execute("UPDATE LearningEvidence SET FacultyComments = $comments, LastUpdatedDate = date('now') WHERE EvidenceId = $id;", ("$comments", comments.Text.Trim()), ("$id", id.Value));
            dialog.Close();
            reload();
            Info("Faculty comments saved.");
        };
        dialog.Controls.Add(comments);
        dialog.Controls.Add(save);
        dialog.ShowDialog(this);
    }

    private void UpdateSelectedNote(DataGridView grid, string status, Action reload)
    {
        var id = SelectedGridId(grid, "NoteId");
        if (id == null) return;
        _db.Execute("UPDATE Notes SET ApprovalStatus = $status, ApprovedBy = 'Faculty', FacultyComments = $comments WHERE NoteId = $id;",
            ("$status", status), ("$comments", status == "Approved" ? "Approved for sharing." : "Rejected by faculty."), ("$id", id.Value));
        reload();
        Info($"Note {status.ToLower()}.");
    }

    private void ViewSelectedNote(DataGridView grid)
    {
        var id = SelectedGridId(grid, "NoteId");
        if (id == null) return;
        ShowTableDialog("Notes Details", """
            SELECT n.NoteId, n.Title, s.SubjectName AS Subject, n.Semester, n.UnitNumber,
                   n.UploadedByRole, n.Description, n.FilePath, n.UploadDate,
                   n.ApprovalStatus, n.ApprovedBy, n.FacultyComments
            FROM Notes n
            JOIN Subjects s ON s.SubjectId = n.SubjectId
            WHERE n.NoteId = $noteId;
            """, ("$noteId", id.Value));
    }

    private void DeleteSelectedNote(DataGridView grid, Action reload)
    {
        var id = SelectedGridId(grid, "NoteId");
        if (id == null) return;
        _db.Execute("DELETE FROM Notes WHERE NoteId = $id;", ("$id", id.Value));
        reload();
        Info("Selected note deleted.");
    }

    private void UpdateSelectedEvent(DataGridView grid, TextBox title, ComboBox type, TextBox by, DateTimePicker date, TextBox time, TextBox venue, DateTimePicker deadline, TextBox desc, Action reload)
    {
        var id = SelectedGridId(grid, "EventId");
        if (id == null) return;
        if (string.IsNullOrWhiteSpace(title.Text)) throw new ArgumentException("Enter an event title before editing.");
        if (string.IsNullOrWhiteSpace(venue.Text)) throw new ArgumentException("Enter an event venue before editing.");
        _db.Execute("""
            UPDATE CampusEvents
            SET EventTitle = $title, EventType = $type, ConductedBy = $by, EventDate = $date,
                Venue = $venue, RegistrationDeadline = $deadline, EventDescription = $description
            WHERE EventId = $id;
            """, ("$title", title.Text.Trim()), ("$type", type.Text), ("$by", by.Text.Trim()), ("$date", date.Value.ToString("yyyy-MM-dd")),
            ("$venue", venue.Text.Trim()), ("$deadline", deadline.Value.ToString("yyyy-MM-dd")),
            ("$description", $"{time.Text} - {desc.Text}".Trim()), ("$id", id.Value));
        reload();
        Info("Campus event updated.");
    }

    private void DeleteSelectedEvent(DataGridView grid, Action reload)
    {
        var id = SelectedGridId(grid, "EventId");
        if (id == null) return;
        _db.Execute("DELETE FROM CampusEvents WHERE EventId = $id;", ("$id", id.Value));
        reload();
        Info("Campus event deleted.");
    }

    private void ShowChangePasswordDialog()
    {
        using var dialog = new Form { Text = "Change Faculty Password", Size = new Size(430, 285), StartPosition = FormStartPosition.CenterParent };
        var current = new TextBox { Location = new Point(175, 28), Width = 190, UseSystemPasswordChar = true };
        var next = new TextBox { Location = new Point(175, 78), Width = 190, UseSystemPasswordChar = true };
        var confirm = new TextBox { Location = new Point(175, 128), Width = 190, UseSystemPasswordChar = true };
        dialog.Controls.Add(new Label { Text = "Current password", Location = new Point(30, 32), Width = 130 });
        dialog.Controls.Add(current);
        dialog.Controls.Add(new Label { Text = "New password", Location = new Point(30, 82), Width = 130 });
        dialog.Controls.Add(next);
        dialog.Controls.Add(new Label { Text = "Confirm password", Location = new Point(30, 132), Width = 130 });
        dialog.Controls.Add(confirm);

        var save = UiFactory.PrimaryButton("Save Password", _theme);
        save.Location = new Point(135, 185);
        save.Click += (_, _) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(next.Text) || next.Text.Length < 4) throw new ArgumentException("New password must be at least 4 characters.");
                if (next.Text != confirm.Text) throw new ArgumentException("New password and confirmation do not match.");

                var stored = _db.Scalar("SELECT Password FROM Users WHERE Username = $username;", ("$username", FacultySessionManager.Current?.Username ?? "faculty1")).ToString() ?? "";
                if (!string.Equals(stored, DBHelper.HashPassword(current.Text), StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Current password is incorrect.");
                }

                _db.Execute("UPDATE Users SET Password = $password WHERE Username = $username;",
                    ("$password", DBHelper.HashPassword(next.Text)),
                    ("$username", FacultySessionManager.Current?.Username ?? "faculty1"));
                dialog.Close();
                Info("Password updated.");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        };
        dialog.Controls.Add(save);
        dialog.ShowDialog(this);
    }

    private void SetAttendanceStatus(DataGridView grid, string status, FlowLayoutPanel cards)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (!row.IsNewRow) row.Cells["Present/Absent"].Value = status;
        }
        RefreshAttendanceCards(cards, grid);
    }

    private void ConfigureAttendanceStatusColumn(DataGridView grid)
    {
        if (!grid.Columns.Contains("Present/Absent")) return;

        var columnIndex = grid.Columns["Present/Absent"].Index;
        if (grid.Columns["Present/Absent"] is DataGridViewComboBoxColumn)
        {
            return;
        }

        grid.Columns.Remove("Present/Absent");
        var statusColumn = new DataGridViewComboBoxColumn
        {
            Name = "Present/Absent",
            HeaderText = "Attendance Status",
            DataPropertyName = "Present/Absent",
            FlatStyle = FlatStyle.Flat,
            Width = 170,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        };
        statusColumn.Items.AddRange("Present", "Absent");
        grid.Columns.Insert(columnIndex, statusColumn);
    }

    private void RefreshAttendanceCards(FlowLayoutPanel cards, DataGridView grid)
    {
        var total = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        var present = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow && r.Cells["Present/Absent"].Value?.ToString() == "Present");
        var absent = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow && r.Cells["Present/Absent"].Value?.ToString() == "Absent");
        var percentage = total == 0 ? 0 : Math.Round(present * 100.0 / total, 2);
        ApplyAttendanceRowStyles(grid);
        cards.Controls.Clear();
        cards.Controls.Add(DashboardCard("Total Students", total.ToString(), _theme.Primary, 160));
        cards.Controls.Add(DashboardCard("Present Count", present.ToString(), _theme.Success, 160));
        cards.Controls.Add(DashboardCard("Absent Count", absent.ToString(), _theme.Danger, 160));
        cards.Controls.Add(DashboardCard("Present Percentage", $"{percentage:0.00}%", _theme.Warning, 190));
        grid.Invalidate();
    }

    private void ApplyAttendanceRowStyles(DataGridView grid)
    {
        if (!grid.Columns.Contains("Present/Absent")) return;

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            var status = row.Cells["Present/Absent"].Value?.ToString();
            row.DefaultCellStyle.BackColor = status == "Present"
                ? Color.FromArgb(230, 247, 235)
                : Color.FromArgb(255, 235, 235);
            row.DefaultCellStyle.ForeColor = _theme.Text;
        }
    }

    private Panel HomeGridCard(string title, string sql, int width, int height, params (string Name, object? Value)[] parameters)
    {
        var card = Ui.Card(_theme, 14);
        card.Width = width;
        card.Height = height;
        card.Margin = new Padding(0, 0, 14, 14);
        card.Controls.Add(Section(title, 12, 8));
        var grid = Grid(12, 44, Math.Max(280, width - 24), Math.Max(120, height - 58));
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grid.DataSource = _db.GetDataTable(sql, parameters);
        card.Controls.Add(grid);
        return card;
    }

    private void ApplyAttendanceAlertFormatting(DataGridView grid)
    {
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            var value = grid.Columns.Contains("Attendance %") ? row.Cells["Attendance %"].Value : null;
            if (value != null && value != DBNull.Value && Convert.ToDouble(value) < 75)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                row.DefaultCellStyle.ForeColor = _theme.Danger;
            }
        };

        grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].HeaderText != "Attendance %") return;
            e.PaintBackground(e.ClipBounds, true);
            var percent = Math.Clamp(Convert.ToDouble(e.Value ?? 0), 0, 100);
            var barBounds = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 8, e.CellBounds.Width - 16, e.CellBounds.Height - 16);
            using var border = new Pen(_theme.Border);
            using var fill = new SolidBrush(percent < 75 ? _theme.Danger : _theme.Success);
            e.Graphics.DrawRectangle(border, barBounds);
            var fillWidth = Math.Max(1, (int)(barBounds.Width * percent / 100));
            e.Graphics.FillRectangle(fill, barBounds.X + 1, barBounds.Y + 1, fillWidth - 2, barBounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, $"{percent:0.0}%", Ui.SmallFont, e.CellBounds, _theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        };
    }

    private DataRow FacultyProfileRow()
    {
        var table = _db.GetDataTable("SELECT * FROM Faculty WHERE FacultyId = $facultyId;", ("$facultyId", FacultyId));
        if (table.Rows.Count == 0) throw new InvalidOperationException("Faculty profile was not found.");
        return table.Rows[0];
    }

    private void AddProfileFields(Control parent, Dictionary<string, TextBox> fields, (string Label, string Column)[] items, DataRow profile, int x, int y)
    {
        foreach (var (label, column) in items)
        {
            var fieldLabel = UiFactory.SmallLabel(label, _theme);
            fieldLabel.Location = new Point(x, y);
            parent.Controls.Add(fieldLabel);

            var box = new TextBox
            {
                Location = new Point(x, y + 22),
                Width = 330,
                Height = column == "Qualification" ? 52 : 28,
                Multiline = column == "Qualification",
                Text = profile.Table.Columns.Contains(column) ? profile[column]?.ToString() ?? "" : ""
            };
            UiFactory.StyleEditableTextBox(box);
            fields[column] = box;
            parent.Controls.Add(box);
            y += column == "Qualification" ? 76 : 60;
        }
    }

    private static void SetFieldsReadOnly(Dictionary<string, TextBox> fields, bool readOnly)
    {
        foreach (var box in fields.Values) box.ReadOnly = readOnly;
    }

    private void ResetFacultyProfileFields(Dictionary<string, TextBox> fields)
    {
        var profile = FacultyProfileRow();
        foreach (var (column, box) in fields)
        {
            box.Text = profile.Table.Columns.Contains(column) ? profile[column]?.ToString() ?? "" : "";
        }
        Info("Profile changes reset.");
    }

    private void SaveFacultyProfile(Dictionary<string, TextBox> fields)
    {
        if (string.IsNullOrWhiteSpace(fields["Name"].Text)) throw new ArgumentException("Faculty name is required.");
        if (!fields["Email"].Text.Contains('@')) throw new ArgumentException("Enter a valid faculty email.");
        _db.Execute("""
            UPDATE Faculty
            SET FacultyCode = $code,
                Name = $name,
                Department = $department,
                Designation = $designation,
                Email = $email,
                MobileNumber = $mobile,
                Mobile = $mobile,
                OfficeRoomNumber = $room,
                OfficeRoom = $room,
                Experience = $experience,
                Qualification = $qualification
            WHERE FacultyId = $facultyId;
            """,
            ("$code", fields["FacultyCode"].Text.Trim()),
            ("$name", fields["Name"].Text.Trim()),
            ("$department", fields["Department"].Text.Trim()),
            ("$designation", fields["Designation"].Text.Trim()),
            ("$email", fields["Email"].Text.Trim()),
            ("$mobile", fields["MobileNumber"].Text.Trim()),
            ("$room", fields["OfficeRoomNumber"].Text.Trim()),
            ("$experience", fields["Experience"].Text.Trim()),
            ("$qualification", fields["Qualification"].Text.Trim()),
            ("$facultyId", FacultyId));
        Info("Faculty profile saved.");
    }

    private Panel PagePanel()
    {
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        panel.AutoScroll = true;
        return panel;
    }

    private FlowLayoutPanel ScrollRoot() => new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = _theme.Background
    };

    private Control DashboardCard(string title, string value, Color color, int width = 170)
    {
        var card = Ui.Card(_theme, 14);
        card.Width = width;
        card.Height = 92;
        var t = Ui.Label(title, Ui.SmallFont, _theme.MutedText);
        t.Location = new Point(12, 10);
        card.Controls.Add(t);
        var v = Ui.Label(value, new Font("Segoe UI", 20, FontStyle.Bold), color);
        v.Location = new Point(12, 40);
        card.Controls.Add(v);
        return card;
    }

    private Control GridCard(string title, string sql, params (string Name, object? Value)[] parameters)
    {
        var panel = Ui.Card(_theme, 14);
        panel.Dock = DockStyle.Fill;
        var heading = Ui.Label(title, Ui.HeadingFont, _theme.Text);
        heading.Location = new Point(12, 10);
        panel.Controls.Add(heading);
        var grid = Grid(12, 46, 500, 235);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grid.DataSource = _db.GetDataTable(sql, parameters);
        panel.Controls.Add(grid);
        return panel;
    }

    private List<(string Label, double Value)> ChartData(string sql, string labelColumn, string valueColumn, params (string Name, object? Value)[] parameters)
    {
        var table = _db.GetDataTable(sql, parameters);
        return table.Rows.Cast<DataRow>()
            .Select(row => (row[labelColumn]?.ToString() ?? "", Convert.ToDouble(row[valueColumn] == DBNull.Value ? 0 : row[valueColumn])))
            .ToList();
    }

    private Panel SectionCard(DataRow row)
    {
        var card = Ui.Card(_theme, 12);
        card.Size = new Size(210, 160);
        card.Cursor = Cursors.Hand;
        card.Controls.Add(new Label { Text = row["SectionName"].ToString(), Font = Ui.HeadingFont, ForeColor = _theme.Text, Location = new Point(12, 10), Size = new Size(185, 28), BackColor = Color.Transparent });
        card.Controls.Add(new Label { Text = $"Sem {row["Semester"]}  |  {row["TotalStudents"]} students", ForeColor = _theme.MutedText, Location = new Point(12, 42), Size = new Size(185, 22), BackColor = Color.Transparent });
        card.Controls.Add(new Label { Text = row["SubjectName"].ToString(), ForeColor = _theme.Primary, Location = new Point(12, 68), Size = new Size(185, 36), BackColor = Color.Transparent });
        card.Controls.Add(new Label { Text = $"Avg attendance: {row["AverageAttendance"]}%", ForeColor = _theme.Text, Location = new Point(12, 108), Size = new Size(185, 20), BackColor = Color.Transparent });
        card.Controls.Add(new Label { Text = $"Pending evidence: {row["PendingEvidence"]}", ForeColor = _theme.Warning, Location = new Point(12, 130), Size = new Size(185, 20), BackColor = Color.Transparent });
        return card;
    }

    private DataGridView Grid(int x, int y, int width, int height)
    {
        var grid = new DataGridView { Location = new Point(x, y), Size = new Size(width, height), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        UiFactory.StyleGrid(grid, _theme);
        return grid;
    }

    private Label Section(string text, int x, int y)
    {
        var label = UiFactory.SectionTitle(text, _theme);
        label.Location = new Point(x, y);
        return label;
    }

    private Control InfoRow(string label, string value, int x, int y)
    {
        var panel = new Panel { Location = new Point(x, y), Size = new Size(900, 30), BackColor = Color.Transparent };
        panel.Controls.Add(new Label { Text = label, Location = new Point(0, 3), Size = new Size(190, 22), ForeColor = _theme.MutedText, BackColor = Color.Transparent });
        panel.Controls.Add(new Label { Text = value, Location = new Point(210, 3), Size = new Size(650, 22), ForeColor = _theme.Text, BackColor = Color.Transparent });
        return panel;
    }

    private void AddAction(Control parent, string text, int x, int y, Action action)
    {
        var button = UiFactory.PrimaryButton(text, _theme);
        button.Location = new Point(x, y);
        button.Width = Math.Max(105, text.Length * 10);
        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        };
        parent.Controls.Add(button);
    }

    private ComboBox SectionCombo(bool includeAll = false)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        if (includeAll) combo.Items.Add(new SectionEntry(null, "All Sections", null, null));
        foreach (DataRow row in SectionRows().Rows) combo.Items.Add(SectionFromRow(row));
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    private ComboBox SubjectCombo(bool includeAll = false)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        if (includeAll) combo.Items.Add(new SubjectEntry(null, "All", "All Subjects", null));
        foreach (DataRow row in SubjectRows().Rows) combo.Items.Add(SubjectFromRow(row));
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    private ComboBox SubjectComboForSection(int? sectionId)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        foreach (DataRow row in SubjectRows(sectionId).Rows) combo.Items.Add(SubjectFromRow(row));
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    private DataTable SectionRows() => _db.GetDataTable("""
        SELECT DISTINCT sec.SectionId, sec.SectionName, sec.Department, sec.Year, sec.Semester, sec.TotalStudents
        FROM FacultySections fs
        JOIN Sections sec ON sec.SectionId = fs.SectionId
        WHERE fs.FacultyId = $facultyId
        ORDER BY sec.Semester, sec.SectionName;
        """, ("$facultyId", FacultyId));

    private DataTable SubjectRows(int? sectionId = null)
    {
        var sectionFilter = sectionId == null ? "" : "AND fs.SectionId = $sectionId";
        return _db.GetDataTable($"""
            SELECT DISTINCT sub.SubjectId, sub.SubjectCode, sub.SubjectName, sec.Semester
            FROM FacultySections fs
            JOIN Subjects sub ON sub.SubjectId = fs.SubjectId
            JOIN Sections sec ON sec.SectionId = fs.SectionId
            WHERE fs.FacultyId = $facultyId {sectionFilter}
            ORDER BY sec.Semester, sub.SubjectName;
            """, ("$facultyId", FacultyId), ("$sectionId", sectionId ?? 0));
    }

    private static SectionEntry SectionFromRow(DataRow row) => new(
        Convert.ToInt32(row["SectionId"]),
        row["SectionName"].ToString() ?? "",
        Convert.ToInt32(row["Semester"]),
        Convert.ToInt32(row["Year"]));

    private static SubjectEntry SubjectFromRow(DataRow row) => new(
        Convert.ToInt32(row["SubjectId"]),
        row["SubjectCode"].ToString() ?? "",
        row["SubjectName"].ToString() ?? "",
        Convert.ToInt32(row["Semester"]));

    private static int? SelectedSectionId(ComboBox combo) => combo.SelectedItem is SectionEntry entry ? entry.Id : null;
    private static int? SelectedSubjectIdOrNull(ComboBox combo) => combo.SelectedItem is SubjectEntry entry ? entry.Id : null;
    private static int SelectedSubjectId(ComboBox combo) => SelectedSubjectIdOrNull(combo) ?? 0;
    private static string SelectedSubjectCode(ComboBox combo) => combo.SelectedItem is SubjectEntry entry ? entry.Code : "";

    private static void SelectSection(ComboBox combo, int sectionId)
    {
        for (var index = 0; index < combo.Items.Count; index++)
        {
            if (combo.Items[index] is SectionEntry entry && entry.Id == sectionId)
            {
                combo.SelectedIndex = index;
                return;
            }
        }
    }

    private int? SelectedStudentId(DataGridView grid) => SelectedGridId(grid, "StudentId");

    private static int? SelectedGridId(DataGridView grid, string columnName)
    {
        if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow || !grid.Columns.Contains(columnName))
        {
            MessageBox.Show("Please select a row first.", "Faculty Module", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        var value = grid.CurrentRow.Cells[columnName].Value;
        if (value == null || value == DBNull.Value)
        {
            MessageBox.Show("The selected row does not have a valid id.", "Faculty Module", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        return Convert.ToInt32(value);
    }

    private static void HideColumn(DataGridView grid, string columnName)
    {
        if (grid.Columns.Contains(columnName)) grid.Columns[columnName].Visible = false;
    }

    private static string GradeFor(double marks, int maxMarks)
    {
        var pct = maxMarks <= 0 ? 0 : marks * 100 / maxMarks;
        return pct >= 90 ? "O" : pct >= 80 ? "A+" : pct >= 70 ? "A" : pct >= 60 ? "B+" : pct >= 50 ? "B" : "RA";
    }

    private static string? PickImagePath()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select profile image",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*"
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    private static string SaveImageCopy(string sourcePath, string fileName, int size)
    {
        var directory = Path.Combine(StudentDatabase.DataDirectory, "ProfileImages");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, fileName);
        using var original = Image.FromFile(sourcePath);
        using var resized = new Bitmap(original, new Size(size, size));
        resized.Save(destination, ImageFormat.Jpeg);
        return destination;
    }

    private static void LoadPicture(PictureBox picture, string? path)
    {
        try
        {
            picture.Image?.Dispose();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                picture.Image = null;
                return;
            }

            using var loaded = Image.FromFile(path);
            picture.Image = new Bitmap(loaded);
        }
        catch
        {
            picture.Image = null;
        }
    }

    private string Count(string sql, params (string Name, object? Value)[] parameters) => _db.Scalar(sql, parameters).ToString() ?? "0";

    private static void Info(string message) => MessageBox.Show(message, "Faculty Module", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void Error(string message) => MessageBox.Show(message, "Faculty Module", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void ShowTableDialog(string title, string sql, params (string Name, object? Value)[] parameters)
    {
        try
        {
            var table = _db.GetDataTable(sql, parameters);
            var dialog = new Form { Text = title, Size = new Size(860, 460), StartPosition = FormStartPosition.CenterParent };
            if (table.Rows.Count == 0)
            {
                dialog.Controls.Add(new Label
                {
                    Text = "No records found.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = Ui.HeadingFont,
                    ForeColor = _theme.MutedText
                });
            }
            else
            {
                var grid = new DataGridView { Dock = DockStyle.Fill };
                UiFactory.StyleGrid(grid, _theme);
                grid.DataSource = table;
                dialog.Controls.Add(grid);
            }
            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Error(ex.Message);
        }
    }

    private static string ShortageSql() => """
        SELECT RegisterNumber, Name AS StudentName, Section, AttendancePercentage
        FROM Students
        WHERE AttendancePercentage < 75
          AND SectionId IN (SELECT SectionId FROM FacultySections WHERE FacultyId = $facultyId)
        ORDER BY AttendancePercentage;
        """;

    private static string SectionReportSql() => """
        SELECT sec.SectionName, sec.Department, sec.Semester, COUNT(DISTINCT st.StudentId) AS TotalStudents,
               ROUND(AVG(st.AttendancePercentage), 2) AS AverageAttendance,
               ROUND(AVG(st.CGPA), 2) AS AverageCGPA,
               COUNT(DISTINCT CASE WHEN st.EvidenceStatus IN ('Pending','Submitted','Rejected') THEN st.StudentId END) AS PendingEvidence
        FROM Sections sec
        JOIN FacultySections fs ON fs.SectionId = sec.SectionId AND fs.FacultyId = $facultyId
        JOIN Students st ON st.SectionId = sec.SectionId
        GROUP BY sec.SectionId
        ORDER BY sec.Semester, sec.SectionName;
        """;

    private static string MarksReportSql() => """
        SELECT sec.SectionName, fmr.SubjectCode, fmr.Semester, fmr.ExamType,
               ROUND(AVG(fmr.MarksObtained), 2) AS AverageMarks,
               MIN(fmr.MarksObtained) AS LowestMarks,
               MAX(fmr.MarksObtained) AS HighestMarks
        FROM FacultyMarksRecords fmr
        JOIN Sections sec ON sec.SectionId = fmr.SectionId
        WHERE fmr.FacultyId = $facultyId
        GROUP BY sec.SectionName, fmr.SubjectCode, fmr.Semester, fmr.ExamType
        ORDER BY fmr.Semester, sec.SectionName, fmr.SubjectCode;
        """;

    private static string InternalMarksSql() => """
        SELECT st.RegisterNumber, st.Name AS StudentName, sec.SectionName, fmr.SubjectCode, fmr.ExamType,
               fmr.MaxMarks, fmr.MarksObtained, fmr.Grade, fmr.Remarks
        FROM FacultyMarksRecords fmr
        JOIN Students st ON st.StudentId = fmr.StudentId
        JOIN Sections sec ON sec.SectionId = fmr.SectionId
        WHERE fmr.ExamType LIKE 'Internal%'
          AND fmr.FacultyId = $facultyId
        ORDER BY sec.SectionName, st.RegisterNumber;
        """;

    private static string AssignedEvidenceCountSql(string condition) => $"""
        SELECT COUNT(*)
        FROM LearningEvidence le
        WHERE {condition}
          AND EXISTS (
              SELECT 1
              FROM Students st
              JOIN FacultySections fs ON fs.SectionId = st.SectionId
              WHERE st.StudentId = le.StudentId AND fs.FacultyId = $facultyId
          );
        """;

    private static string SectionOverviewSql() => """
        SELECT sec.SectionName AS 'Section Name',
               sec.Semester,
               sub.SubjectName AS Subject,
               COUNT(DISTINCT st.StudentId) AS 'Total Students',
               ROUND(IFNULL(AVG(st.AttendancePercentage), 0), 2) AS 'Average Attendance %',
               ROUND(IFNULL(AVG(fmr.MarksObtained), 0), 2) AS 'Average Internal Marks'
        FROM FacultySections fs
        JOIN Sections sec ON sec.SectionId = fs.SectionId
        JOIN Subjects sub ON sub.SubjectId = fs.SubjectId
        LEFT JOIN Students st ON st.SectionId = sec.SectionId
        LEFT JOIN FacultyMarksRecords fmr ON fmr.StudentId = st.StudentId
            AND fmr.SectionId = sec.SectionId
            AND fmr.SubjectId = fs.SubjectId
        WHERE fs.FacultyId = $facultyId
        GROUP BY sec.SectionId, sec.SectionName, sec.Semester, sub.SubjectName
        ORDER BY sec.Semester, sec.SectionName;
        """;

    private static string RecentActivitySql() => """
        SELECT COALESCE(st.Name, 'Faculty') AS 'Student Name',
               'Evidence submitted: ' || le.Title AS Action,
               le.UploadDate AS Date
        FROM LearningEvidence le
        LEFT JOIN Students st ON st.StudentId = le.StudentId
        WHERE le.FacultyId = $facultyId
        UNION ALL
        SELECT COALESCE(st.Name, 'Faculty') AS 'Student Name',
               'Evidence approved: ' || le.Title AS Action,
               le.UploadDate AS Date
        FROM LearningEvidence le
        LEFT JOIN Students st ON st.StudentId = le.StudentId
        WHERE le.FacultyId = $facultyId AND le.Status = 'Approved'
        UNION ALL
        SELECT 'Faculty' AS 'Student Name',
               'Notes uploaded: ' || n.Title AS Action,
               n.UploadDate AS Date
        FROM Notes n
        WHERE n.FacultyId = $facultyId OR n.UploadedByRole = 'Faculty'
        UNION ALL
        SELECT st.Name AS 'Student Name',
               'Marks updated: ' || fmr.SubjectCode || ' ' || fmr.ExamType AS Action,
               date('now') AS Date
        FROM FacultyMarksRecords fmr
        JOIN Students st ON st.StudentId = fmr.StudentId
        WHERE fmr.FacultyId = $facultyId
        ORDER BY Date DESC
        LIMIT 10;
        """;

    private static string SectionAttendanceChartSql() => """
        SELECT sec.SectionName,
               ROUND(IFNULL(AVG(st.AttendancePercentage), 0), 2) AS AverageAttendance
        FROM FacultySections fs
        JOIN Sections sec ON sec.SectionId = fs.SectionId
        LEFT JOIN Students st ON st.SectionId = sec.SectionId
        WHERE fs.FacultyId = $facultyId
        GROUP BY sec.SectionId, sec.SectionName
        ORDER BY sec.SectionName;
        """;

    private static string EvidenceStatusChartSql() => """
        SELECT le.Status, COUNT(*) AS Total
        FROM LearningEvidence le
        WHERE le.FacultyId = $facultyId
        GROUP BY le.Status
        ORDER BY le.Status;
        """;

    private record SectionEntry(int? Id, string Name, int? Semester, int? Year)
    {
        public override string ToString() => Id == null ? Name : $"{Name} - Sem {Semester}";
    }

    private record SubjectEntry(int? Id, string Code, string Name, int? Semester)
    {
        public override string ToString() => Id == null ? Name : $"{Code} - {Name}";
    }
}

public enum ChartKind
{
    Bar,
    Pie
}

public class SimpleChartPanel : Panel
{
    private readonly ThemePalette _theme;
    private readonly ChartKind _kind;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public List<(string Label, double Value)> Data { get; set; } = [];

    public SimpleChartPanel(ThemePalette theme, ChartKind kind)
    {
        _theme = theme;
        _kind = kind;
        BackColor = theme.Surface;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Data.Count == 0 || Data.Sum(point => point.Value) <= 0)
        {
            TextRenderer.DrawText(e.Graphics, "No chart data", Ui.BodyFont, ClientRectangle, _theme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        if (_kind == ChartKind.Bar) DrawBarChart(e.Graphics);
        else DrawPieChart(e.Graphics);
    }

    private void DrawBarChart(Graphics graphics)
    {
        var titleBounds = new Rectangle(0, 0, Width, 24);
        TextRenderer.DrawText(graphics, "Section vs Attendance", Ui.SmallFont, titleBounds, _theme.Text, TextFormatFlags.HorizontalCenter);
        var max = Math.Max(100, Data.Max(point => point.Value));
        var chartBounds = new Rectangle(16, 34, Width - 32, Height - 78);
        var barWidth = Math.Max(18, chartBounds.Width / Math.Max(1, Data.Count) - 10);
        using var fill = new SolidBrush(_theme.Primary);
        using var textBrush = new SolidBrush(_theme.Text);
        using var axis = new Pen(_theme.Border);
        graphics.DrawLine(axis, chartBounds.Left, chartBounds.Bottom, chartBounds.Right, chartBounds.Bottom);

        for (var index = 0; index < Data.Count; index++)
        {
            var point = Data[index];
            var x = chartBounds.Left + index * (barWidth + 10);
            var height = (int)(chartBounds.Height * point.Value / max);
            var rect = new Rectangle(x, chartBounds.Bottom - height, barWidth, height);
            graphics.FillRectangle(fill, rect);
            TextRenderer.DrawText(graphics, $"{point.Value:0}", Ui.SmallFont, new Rectangle(x - 4, rect.Top - 20, barWidth + 8, 18), _theme.Text, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(graphics, point.Label.Replace("CSE-", ""), Ui.SmallFont, new Rectangle(x - 12, chartBounds.Bottom + 4, barWidth + 24, 36), _theme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }

    private void DrawPieChart(Graphics graphics)
    {
        TextRenderer.DrawText(graphics, "Evidence Status", Ui.SmallFont, new Rectangle(0, 0, Width, 24), _theme.Text, TextFormatFlags.HorizontalCenter);
        var total = Data.Sum(point => point.Value);
        var pie = new Rectangle(18, 42, Math.Min(130, Width - 110), Math.Min(130, Height - 90));
        var colors = new[] { _theme.Success, _theme.Warning, _theme.Danger, _theme.Primary };
        var start = 0f;
        for (var index = 0; index < Data.Count; index++)
        {
            var sweep = (float)(Data[index].Value * 360 / total);
            using var brush = new SolidBrush(colors[index % colors.Length]);
            graphics.FillPie(brush, pie, start, sweep);
            start += sweep;
        }

        var legendY = 44;
        for (var index = 0; index < Data.Count; index++)
        {
            using var brush = new SolidBrush(colors[index % colors.Length]);
            graphics.FillRectangle(brush, Width - 84, legendY + 4, 10, 10);
            TextRenderer.DrawText(graphics, $"{Data[index].Label}: {Data[index].Value:0}", Ui.SmallFont, new Rectangle(Width - 70, legendY, 70, 24), _theme.Text);
            legendY += 24;
        }
    }
}
