using System.Data;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentEvidenceControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly ComboBox _category = UiFactory.Combo(
        "Assignments", "Lab records", "Mini projects", "Major projects", "Certifications",
        "Internship proofs", "Workshop certificates", "Seminar participation", "Hackathon participation",
        "Club/event reports", "Research papers", "Publications", "Online course completion",
        "Volunteering activities", "Sports/cultural achievements");
    private readonly ComboBox _semester = UiFactory.Combo("1", "2", "3", "4", "5", "6", "7", "8");
    private readonly ComboBox _subject = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 290 };
    private readonly ComboBox _status = UiFactory.Combo("Draft", "Submitted", "Pending");
    private readonly ComboBox _visibility = UiFactory.Combo("Private", "Visible to faculty", "Visible in student portfolio");
    private readonly TextBox _title = new() { PlaceholderText = "Evidence title" };
    private readonly TextBox _academicYear = new() { PlaceholderText = "Academic year e.g. 2023-2027" };
    private readonly TextBox _description = new() { PlaceholderText = "Description", Multiline = true };
    private readonly TextBox _learningOutcome = new() { PlaceholderText = "Learning outcome", Multiline = true };
    private readonly TextBox _skills = new() { PlaceholderText = "Technical, Communication, Leadership, Problem-solving, Teamwork, Research" };
    private readonly TextBox _filePath = new() { PlaceholderText = "PDF / DOCX / JPG / PNG / ZIP / PPT / PPTX file path" };
    private readonly TextBox _startDate = new() { PlaceholderText = "YYYY-MM-DD" };
    private readonly TextBox _endDate = new() { PlaceholderText = "YYYY-MM-DD" };
    private readonly TextBox _organization = new() { PlaceholderText = "Issued organization" };
    private readonly TextBox _certificateId = new() { PlaceholderText = "Certificate ID if available" };
    private readonly TextBox _verification = new() { PlaceholderText = "Verification link if available" };
    private readonly Label _editState = new();
    private int? _editingEvidenceId;

    private readonly DataGridView _statusGrid = new();
    private readonly DataGridView _portfolioGrid = new();
    private readonly DataGridView _searchGrid = new();
    private readonly DataGridView _alertsGrid = new();
    private readonly TextBox _search = new() { PlaceholderText = "Search title, category, subject, status, skills" };
    private readonly ComboBox _filterStatus = UiFactory.Combo("All", "Draft", "Submitted", "Pending", "Approved", "Rejected", "Resubmitted");
    private readonly ComboBox _filterSemester = UiFactory.Combo("All", "1", "2", "3", "4", "5", "6", "7", "8");
    private readonly ComboBox _filterCategory = UiFactory.Combo("All", "Assignments", "Lab records", "Mini projects", "Major projects", "Certifications",
        "Internship proofs", "Workshop certificates", "Seminar participation", "Hackathon participation",
        "Club/event reports", "Research papers", "Publications", "Online course completion",
        "Volunteering activities", "Sports/cultural achievements");
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly FlowLayoutPanel _summary = new();

    public StudentEvidenceControl(StudentDatabase database, ThemePalette theme)
    {
        _database = database;
        _theme = theme;
        Build();
        _semester.Text = _database.GetAcademicDetails(StudentSessionManager.StudentId).CurrentSemester.ToString();
        _academicYear.Text = "2023-2027";
        LoadSubjects();
        LoadData();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);
        tabs.TabPages.Add(BuildUploadTab());
        tabs.TabPages.Add(BuildGridTab("Evidence Status Tracking", _statusGrid, includeEditActions: true));
        tabs.TabPages.Add(BuildPortfolioTab());
        tabs.TabPages.Add(BuildSearchTab());
        tabs.TabPages.Add(BuildSummaryTab());
        tabs.TabPages.Add(BuildGridTab("Reminder and Alerts", _alertsGrid));
    }

    private TabPage BuildUploadTab()
    {
        var page = new TabPage("Evidence Upload") { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);

        var heading = UiFactory.SectionTitle("Advanced Evidence Upload / Resubmission", _theme);
        heading.Location = new Point(16, 16);
        panel.Controls.Add(heading);

        var y = 62;
        AddLabel(panel, "Title", 16, y);
        _title.Location = new Point(16, y + 22);
        _title.Width = 330;
        panel.Controls.Add(_title);

        AddLabel(panel, "Category", 370, y);
        _category.Location = new Point(370, y + 22);
        _category.Width = 260;
        panel.Controls.Add(_category);

        AddLabel(panel, "Status", 650, y);
        _status.Location = new Point(650, y + 22);
        panel.Controls.Add(_status);

        y += 70;
        AddLabel(panel, "Semester", 16, y);
        _semester.Location = new Point(16, y + 22);
        panel.Controls.Add(_semester);

        AddLabel(panel, "Subject", 220, y);
        _subject.Location = new Point(220, y + 22);
        panel.Controls.Add(_subject);

        AddLabel(panel, "Academic Year", 540, y);
        _academicYear.Location = new Point(540, y + 22);
        _academicYear.Width = 170;
        panel.Controls.Add(_academicYear);

        AddLabel(panel, "Visibility", 730, y);
        _visibility.Location = new Point(730, y + 22);
        _visibility.Width = 230;
        panel.Controls.Add(_visibility);

        y += 70;
        AddLabel(panel, "Description", 16, y);
        _description.Location = new Point(16, y + 22);
        _description.Size = new Size(460, 72);
        panel.Controls.Add(_description);

        AddLabel(panel, "Learning Outcome", 500, y);
        _learningOutcome.Location = new Point(500, y + 22);
        _learningOutcome.Size = new Size(460, 72);
        panel.Controls.Add(_learningOutcome);

        y += 112;
        AddLabel(panel, "Skills Gained / Skill Mapping", 16, y);
        _skills.Location = new Point(16, y + 22);
        _skills.Width = 460;
        panel.Controls.Add(_skills);

        AddLabel(panel, "File Path", 500, y);
        _filePath.Location = new Point(500, y + 22);
        _filePath.Width = 360;
        panel.Controls.Add(_filePath);

        var browse = Ui.Button("Browse", _theme.SurfaceAlt, _theme.Text);
        browse.Location = new Point(875, y + 18);
        browse.Width = 88;
        browse.Click += (_, _) => BrowseFile();
        panel.Controls.Add(browse);

        y += 70;
        AddLabel(panel, "Start Date", 16, y);
        _startDate.Location = new Point(16, y + 22);
        _startDate.Width = 150;
        panel.Controls.Add(_startDate);

        AddLabel(panel, "End Date", 186, y);
        _endDate.Location = new Point(186, y + 22);
        _endDate.Width = 150;
        panel.Controls.Add(_endDate);

        AddLabel(panel, "Issued Organization", 356, y);
        _organization.Location = new Point(356, y + 22);
        _organization.Width = 230;
        panel.Controls.Add(_organization);

        AddLabel(panel, "Certificate ID", 606, y);
        _certificateId.Location = new Point(606, y + 22);
        _certificateId.Width = 160;
        panel.Controls.Add(_certificateId);

        AddLabel(panel, "Verification Link", 786, y);
        _verification.Location = new Point(786, y + 22);
        _verification.Width = 230;
        panel.Controls.Add(_verification);

        y += 78;
        var save = UiFactory.PrimaryButton("Save Evidence", _theme);
        save.Location = new Point(16, y);
        save.Width = 145;
        save.Click += (_, _) => SaveEvidence(isResubmission: false);
        panel.Controls.Add(save);

        var resubmit = Ui.Button("Resubmit Corrected File", _theme.SurfaceAlt, _theme.Text);
        resubmit.Location = new Point(176, y);
        resubmit.Width = 190;
        resubmit.Click += (_, _) => SaveEvidence(isResubmission: true);
        panel.Controls.Add(resubmit);

        var load = Ui.Button("Load Selected", _theme.SurfaceAlt, _theme.Text);
        load.Location = new Point(382, y);
        load.Width = 130;
        load.Click += (_, _) => LoadSelectedForEdit();
        panel.Controls.Add(load);

        var clear = Ui.Button("Clear", _theme.SurfaceAlt, _theme.Text);
        clear.Location = new Point(528, y);
        clear.Width = 90;
        clear.Click += (_, _) => ClearForm();
        panel.Controls.Add(clear);

        _editState.Location = new Point(640, y + 8);
        _editState.Size = new Size(420, 28);
        _editState.ForeColor = _theme.MutedText;
        _editState.BackColor = Color.Transparent;
        _editState.Text = "Drafts stay private until you submit them.";
        panel.Controls.Add(_editState);

        return page;
    }

    private TabPage BuildGridTab(string title, DataGridView grid, bool includeEditActions = false)
    {
        var page = new TabPage(title) { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);

        var heading = UiFactory.SectionTitle(title, _theme);
        heading.Location = new Point(16, 16);
        panel.Controls.Add(heading);

        var open = Ui.Button("Open File", _theme.SurfaceAlt, _theme.Text);
        open.Location = new Point(16, 58);
        open.Width = 110;
        open.Click += (_, _) => OpenSelected(grid);
        panel.Controls.Add(open);

        if (includeEditActions)
        {
            var load = UiFactory.PrimaryButton("Load For Edit / Resubmit", _theme);
            load.Location = new Point(140, 58);
            load.Width = 190;
            load.Click += (_, _) => LoadSelectedForEdit();
            panel.Controls.Add(load);
        }

        grid.Location = new Point(16, 112);
        grid.Size = new Size(1100, 500);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(grid, _theme);
        panel.Controls.Add(grid);
        return page;
    }

    private TabPage BuildPortfolioTab()
    {
        var page = BuildGridTab("Evidence Portfolio", _portfolioGrid);
        var panel = page.Controls[0];
        var exportApproved = UiFactory.PrimaryButton("Export Approved Portfolio", _theme);
        exportApproved.Location = new Point(140, 58);
        exportApproved.Width = 190;
        exportApproved.Click += (_, _) => ExportReport("Approved evidence portfolio", "Status = 'Approved'");
        panel.Controls.Add(exportApproved);
        return page;
    }

    private TabPage BuildSearchTab()
    {
        var page = new TabPage("Search and Filter") { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);

        var heading = UiFactory.SectionTitle("Search Evidence", _theme);
        heading.Location = new Point(16, 16);
        panel.Controls.Add(heading);

        _search.Location = new Point(16, 62);
        _search.Width = 240;
        _search.TextChanged += (_, _) => LoadSearch();
        panel.Controls.Add(_search);

        _filterStatus.Location = new Point(270, 62);
        _filterStatus.Width = 135;
        _filterStatus.SelectedIndexChanged += (_, _) => LoadSearch();
        panel.Controls.Add(_filterStatus);

        _filterSemester.Location = new Point(418, 62);
        _filterSemester.Width = 90;
        _filterSemester.SelectedIndexChanged += (_, _) => LoadSearch();
        panel.Controls.Add(_filterSemester);

        _filterCategory.Location = new Point(522, 62);
        _filterCategory.Width = 215;
        _filterCategory.SelectedIndexChanged += (_, _) => LoadSearch();
        panel.Controls.Add(_filterCategory);

        _from.Location = new Point(750, 62);
        _from.ValueChanged += (_, _) => LoadSearch();
        panel.Controls.Add(_from);

        _to.Location = new Point(870, 62);
        _to.ValueChanged += (_, _) => LoadSearch();
        panel.Controls.Add(_to);

        var export = UiFactory.PrimaryButton("Export Report", _theme);
        export.Location = new Point(990, 58);
        export.Width = 120;
        export.Click += (_, _) => ExportReport("Evidence search report", SearchWhereClause());
        panel.Controls.Add(export);

        _searchGrid.Location = new Point(16, 112);
        _searchGrid.Size = new Size(1100, 500);
        _searchGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_searchGrid, _theme);
        panel.Controls.Add(_searchGrid);
        return page;
    }

    private TabPage BuildSummaryTab()
    {
        var page = new TabPage("Progress Summary") { BackColor = _theme.Background };
        _summary.Dock = DockStyle.Fill;
        _summary.AutoScroll = true;
        _summary.BackColor = _theme.Background;
        _summary.FlowDirection = FlowDirection.TopDown;
        _summary.WrapContents = false;
        _summary.Padding = new Padding(16);
        page.Controls.Add(_summary);
        return page;
    }

    private void LoadData()
    {
        _statusGrid.DataSource = EvidenceTable("1 = 1");
        _portfolioGrid.DataSource = EvidenceTable("Status = 'Approved'");
        _alertsGrid.DataSource = AlertsTable();
        LoadSearch();
        LoadSummary();
        HideIds(_statusGrid, _portfolioGrid, _searchGrid, _alertsGrid);
    }

    private void LoadSearch()
    {
        _searchGrid.DataSource = EvidenceTable(SearchWhereClause());
        HideIds(_searchGrid);
    }

    private string SearchWhereClause()
    {
        var statusFilter = _filterStatus.Text == "All" ? "" : "AND Status = $status";
        var semesterFilter = _filterSemester.Text == "All" ? "" : "AND Semester = $semester";
        var categoryFilter = _filterCategory.Text == "All" ? "" : "AND Category = $category";
        var fromFilter = _from.Checked ? "AND date(UploadDate) >= date($from)" : "";
        var toFilter = _to.Checked ? "AND date(UploadDate) <= date($to)" : "";

        return $"""
            ($search = ''
             OR lower(le.Title || le.Category || s.SubjectName || le.Status || le.SkillsGained || le.Description) LIKE '%' || lower($search) || '%')
            {statusFilter}
            {semesterFilter}
            {categoryFilter}
            {fromFilter}
            {toFilter}
            """;
    }

    private DataTable EvidenceTable(string where)
    {
        return _database.GetDataTable($"""
            SELECT le.EvidenceId,
                   le.Title,
                   le.Category,
                   le.Semester,
                   s.SubjectCode,
                   s.SubjectName,
                   le.AcademicYear,
                   le.Description,
                   le.LearningOutcome,
                   le.SkillsGained,
                   le.FilePath,
                   le.FileType,
                   le.UploadDate,
                   le.StartDate,
                   le.EndDate,
                   le.IssuedOrganization,
                   le.CertificateId,
                   le.VerificationLink,
                   le.Status,
                   CASE WHEN le.Status = 'Rejected' THEN 'Resubmission required' ELSE '' END AS ResubmissionAlert,
                   le.FacultyComments,
                   le.Visibility,
                   le.ResubmissionCount,
                   le.LastUpdatedDate
            FROM LearningEvidence le
            JOIN Subjects s ON s.SubjectId = le.SubjectId
            WHERE le.StudentId = $studentId AND {where}
            ORDER BY le.Semester DESC, le.UploadDate DESC, le.EvidenceId DESC;
            """,
            ("$studentId", StudentSessionManager.StudentId),
            ("$search", _search.Text.Trim()),
            ("$status", _filterStatus.Text),
            ("$semester", _filterSemester.Text),
            ("$category", _filterCategory.Text),
            ("$from", _from.Value.ToString("yyyy-MM-dd")),
            ("$to", _to.Value.ToString("yyyy-MM-dd")));
    }

    private DataTable AlertsTable()
    {
        var academic = _database.GetAcademicDetails(StudentSessionManager.StudentId);
        return _database.GetDataTable("""
            SELECT 'Pending evidence validation' AS AlertType, Title, Semester, Status, FacultyComments, LastUpdatedDate
            FROM LearningEvidence
            WHERE StudentId = $studentId AND Status IN ('Submitted', 'Pending', 'Resubmitted')
            UNION ALL
            SELECT 'Rejected evidence needing resubmission', Title, Semester, Status, FacultyComments, LastUpdatedDate
            FROM LearningEvidence
            WHERE StudentId = $studentId AND Status = 'Rejected'
            UNION ALL
            SELECT 'Missing evidence for current semester', 'Upload at least one approved evidence for this semester', $semester, 'Missing', '', date('now')
            WHERE NOT EXISTS (
                SELECT 1 FROM LearningEvidence WHERE StudentId = $studentId AND Semester = $semester AND Status = 'Approved'
            )
            UNION ALL
            SELECT 'Upcoming certificate expiry', Title, Semester, Status, 'Review certificate validity if applicable', EndDate
            FROM LearningEvidence
            WHERE StudentId = $studentId AND EndDate <> '' AND date(EndDate) BETWEEN date('now') AND date('now', '+30 days')
            ORDER BY LastUpdatedDate DESC;
            """, ("$studentId", StudentSessionManager.StudentId), ("$semester", academic.CurrentSemester));
    }

    private void LoadSummary()
    {
        _summary.SuspendLayout();
        _summary.Controls.Clear();
        var counts = _database.GetDataTable("""
            SELECT
                COUNT(*) AS Total,
                SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) AS Approved,
                SUM(CASE WHEN Status IN ('Pending', 'Submitted', 'Resubmitted') THEN 1 ELSE 0 END) AS Pending,
                SUM(CASE WHEN Status = 'Rejected' THEN 1 ELSE 0 END) AS Rejected
            FROM LearningEvidence WHERE StudentId = $studentId;
            """, ("$studentId", StudentSessionManager.StudentId));
        var row = counts.Rows[0];
        var total = Convert.ToInt32(row["Total"]);
        var approved = Convert.ToInt32(row["Approved"] == DBNull.Value ? 0 : row["Approved"]);
        var pending = Convert.ToInt32(row["Pending"] == DBNull.Value ? 0 : row["Pending"]);
        var rejected = Convert.ToInt32(row["Rejected"] == DBNull.Value ? 0 : row["Rejected"]);
        var completion = total == 0 ? 0 : approved * 100 / total;

        _summary.Controls.Add(SummaryCard("Total evidence uploaded", total.ToString(), _theme.Primary));
        _summary.Controls.Add(SummaryCard("Approved count", approved.ToString(), _theme.Success));
        _summary.Controls.Add(SummaryCard("Pending count", pending.ToString(), _theme.Warning));
        _summary.Controls.Add(SummaryCard("Rejected count", rejected.ToString(), _theme.Danger));
        _summary.Controls.Add(SummaryCard("Evidence completion percentage", $"{completion}%", _theme.Primary));

        var semesterRows = _database.GetDataTable("""
            SELECT Semester, COUNT(*) AS Total, SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) AS Approved
            FROM LearningEvidence WHERE StudentId = $studentId
            GROUP BY Semester ORDER BY Semester;
            """, ("$studentId", StudentSessionManager.StudentId));
        var grid = new DataGridView { Width = 980, Height = 240 };
        UiFactory.StyleGrid(grid, _theme);
        grid.DataSource = semesterRows;
        _summary.Controls.Add(grid);
        _summary.ResumeLayout();
    }

    private Control SummaryCard(string label, string value, Color color)
    {
        var card = Ui.Card(_theme, 14);
        card.Width = 980;
        card.Height = 72;
        var l = Ui.Label(label, Ui.BodyFont, _theme.MutedText);
        l.Location = new Point(12, 10);
        card.Controls.Add(l);
        var v = Ui.Label(value, new Font("Segoe UI", 18, FontStyle.Bold), color);
        v.Location = new Point(12, 34);
        card.Controls.Add(v);
        return card;
    }

    private void SaveEvidence(bool isResubmission)
    {
        try
        {
            var subject = SelectedSubject();
            if (_editingEvidenceId == null)
            {
                _database.AddAdvancedEvidence(StudentSessionManager.StudentId, _title.Text, _category.Text, int.Parse(_semester.Text),
                    subject.SubjectId, _academicYear.Text, _description.Text, _learningOutcome.Text, _skills.Text, _filePath.Text,
                    _startDate.Text, _endDate.Text, _organization.Text, _certificateId.Text, _verification.Text, _status.Text, _visibility.Text);
            }
            else
            {
                _database.UpdateAdvancedEvidence(_editingEvidenceId.Value, StudentSessionManager.StudentId, _title.Text, _category.Text,
                    int.Parse(_semester.Text), subject.SubjectId, _academicYear.Text, _description.Text, _learningOutcome.Text, _skills.Text,
                    _filePath.Text, _startDate.Text, _endDate.Text, _organization.Text, _certificateId.Text, _verification.Text,
                    _status.Text, _visibility.Text, isResubmission);
            }

            ClearForm();
            LoadData();
            MessageBox.Show(isResubmission ? "Evidence resubmitted successfully." : "Evidence saved successfully.", "Evidence", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Evidence", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void LoadSelectedForEdit()
    {
        if (_statusGrid.CurrentRow == null) return;
        var row = _statusGrid.CurrentRow;
        _editingEvidenceId = Convert.ToInt32(row.Cells["EvidenceId"].Value);
        _title.Text = row.Cells["Title"].Value?.ToString() ?? "";
        _category.Text = row.Cells["Category"].Value?.ToString() ?? _category.Text;
        _semester.Text = row.Cells["Semester"].Value?.ToString() ?? _semester.Text;
        SelectSubject(row.Cells["SubjectCode"].Value?.ToString() ?? "");
        _academicYear.Text = row.Cells["AcademicYear"].Value?.ToString() ?? "";
        _description.Text = row.Cells["Description"].Value?.ToString() ?? "";
        _learningOutcome.Text = row.Cells["LearningOutcome"].Value?.ToString() ?? "";
        _skills.Text = row.Cells["SkillsGained"].Value?.ToString() ?? "";
        _filePath.Text = row.Cells["FilePath"].Value?.ToString() ?? "";
        _startDate.Text = row.Cells["StartDate"].Value?.ToString() ?? "";
        _endDate.Text = row.Cells["EndDate"].Value?.ToString() ?? "";
        _organization.Text = row.Cells["IssuedOrganization"].Value?.ToString() ?? "";
        _certificateId.Text = row.Cells["CertificateId"].Value?.ToString() ?? "";
        _verification.Text = row.Cells["VerificationLink"].Value?.ToString() ?? "";
        _status.Text = row.Cells["Status"].Value?.ToString() is "Approved" or "Rejected" ? "Submitted" : row.Cells["Status"].Value?.ToString() ?? "Draft";
        _visibility.Text = row.Cells["Visibility"].Value?.ToString() ?? _visibility.Text;
        _editState.Text = $"Editing Evidence #{_editingEvidenceId}. Rejected records can be resubmitted.";
    }

    private void ExportReport(string title, string where)
    {
        var table = EvidenceTable(where);
        var directory = Path.Combine(StudentDatabase.DataDirectory, "EvidenceReports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{title.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        using var writer = new StreamWriter(path);
        writer.WriteLine(title);
        writer.WriteLine($"Generated: {DateTime.Now}");
        writer.WriteLine();
        foreach (DataRow row in table.Rows)
        {
            writer.WriteLine($"#{row["EvidenceId"]} | {row["Title"]} | {row["Category"]} | Semester {row["Semester"]} | {row["SubjectName"]}");
            writer.WriteLine($"Status: {row["Status"]} | Visibility: {row["Visibility"]} | Skills: {row["SkillsGained"]}");
            writer.WriteLine($"File: {row["FilePath"]}");
            writer.WriteLine();
        }
        MessageBox.Show($"Evidence report exported:\n{path}", "Evidence Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenSelected(DataGridView grid)
    {
        if (grid.CurrentRow == null) return;
        var path = grid.CurrentRow.Cells["FilePath"].Value?.ToString() ?? "";
        if (!File.Exists(path))
        {
            MessageBox.Show($"File path stored in database:\n{path}\n\nThe file does not exist on this computer.", "Preview");
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void LoadSubjects()
    {
        _subject.Items.Clear();
        var rows = _database.GetDataTable("SELECT SubjectId, SubjectCode, SubjectName FROM Subjects ORDER BY SubjectName;");
        foreach (DataRow row in rows.Rows)
        {
            _subject.Items.Add(new SubjectItem(Convert.ToInt32(row["SubjectId"]), row["SubjectCode"].ToString() ?? "", row["SubjectName"].ToString() ?? ""));
        }
        if (_subject.Items.Count > 0) _subject.SelectedIndex = 0;
    }

    private void BrowseFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Evidence files|*.pdf;*.docx;*.jpg;*.jpeg;*.png;*.zip;*.ppt;*.pptx|All files|*.*" };
        if (dialog.ShowDialog() == DialogResult.OK) _filePath.Text = dialog.FileName;
    }

    private void ClearForm()
    {
        _editingEvidenceId = null;
        foreach (var box in new[] { _title, _description, _learningOutcome, _skills, _filePath, _startDate, _endDate, _organization, _certificateId, _verification }) box.Clear();
        _status.SelectedIndex = 0;
        _visibility.SelectedIndex = 0;
        _editState.Text = "Drafts stay private until you submit them.";
    }

    private SubjectItem SelectedSubject()
    {
        if (_subject.SelectedItem is SubjectItem item) return item;
        throw new InvalidOperationException("Select a subject.");
    }

    private void SelectSubject(string code)
    {
        for (var i = 0; i < _subject.Items.Count; i++)
        {
            if (_subject.Items[i] is SubjectItem item && item.Code == code)
            {
                _subject.SelectedIndex = i;
                return;
            }
        }
    }

    private void AddLabel(Control parent, string text, int x, int y)
    {
        var label = UiFactory.SmallLabel(text, _theme);
        label.Location = new Point(x, y);
        parent.Controls.Add(label);
    }

    private static void HideIds(params DataGridView[] grids)
    {
        foreach (var grid in grids)
        {
            if (grid.Columns["EvidenceId"] is DataGridViewColumn c) c.Visible = false;
        }
    }

    private sealed record SubjectItem(int SubjectId, string Code, string Name)
    {
        public override string ToString() => $"{Code} - {Name}";
    }
}
