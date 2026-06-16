using System.Data;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyAttendanceControl : Control
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly StudentDatabase _db = new();
    private ComboBox _sectionCombo = new();
    private ComboBox _subjectCombo = new();
    private DateTimePicker _datePicker = new();
    private DataGridView _attendanceGrid = new();
    private Button _markAllPresentButton = new();
    private Button _markAllAbsentButton = new();
    private Button _saveButton = new();

    public FacultyAttendanceControl()
    {
        Dock = DockStyle.Fill;
        BackColor = _theme.Background;
        Build();
        LoadSections();
    }

    private void Build()
    {
        var container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _theme.Background };

        var title = Ui.Label("Attendance Management", Ui.TitleFont, _theme.Text);
        title.Location = new Point(22, 18);
        title.AutoSize = true;
        container.Controls.Add(title);

        // Selection Panel
        var selectionPanel = new Panel { Location = new Point(20, 60), Size = new Size(container.Width - 40, 100), BackColor = _theme.Surface };
        selectionPanel.BorderStyle = BorderStyle.FixedSingle;
        container.Controls.Add(selectionPanel);

        var labelFont = Ui.SmallFont;
        var inputX = 150;
        var inputWidth = 160;

        // Section
        var sectionLabel = Ui.Label("Section:", labelFont, _theme.Text);
        sectionLabel.Location = new Point(10, 12);
        sectionLabel.AutoSize = true;
        selectionPanel.Controls.Add(sectionLabel);

        _sectionCombo.Location = new Point(inputX, 12);
        _sectionCombo.Size = new Size(inputWidth, 24);
        _sectionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sectionCombo.SelectedIndexChanged += (_, _) => LoadSubjects();
        selectionPanel.Controls.Add(_sectionCombo);

        // Subject
        var subjectLabel = Ui.Label("Subject:", labelFont, _theme.Text);
        subjectLabel.Location = new Point(10, 42);
        subjectLabel.AutoSize = true;
        selectionPanel.Controls.Add(subjectLabel);

        _subjectCombo.Location = new Point(inputX, 42);
        _subjectCombo.Size = new Size(inputWidth, 24);
        _subjectCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _subjectCombo.SelectedIndexChanged += (_, _) => LoadAttendance();
        selectionPanel.Controls.Add(_subjectCombo);

        // Date
        var dateLabel = Ui.Label("Date:", labelFont, _theme.Text);
        dateLabel.Location = new Point(380, 12);
        dateLabel.AutoSize = true;
        selectionPanel.Controls.Add(dateLabel);

        _datePicker.Location = new Point(inputX + 230, 12);
        _datePicker.Size = new Size(inputWidth, 24);
        _datePicker.ValueChanged += (_, _) => LoadAttendance();
        selectionPanel.Controls.Add(_datePicker);

        // Action Buttons
        _markAllPresentButton = Ui.Button("Mark All Present", _theme.Primary, Color.White);
        _markAllPresentButton.Location = new Point(380, 42);
        _markAllPresentButton.Click += (_, _) => MarkAll("Present");
        selectionPanel.Controls.Add(_markAllPresentButton);

        _markAllAbsentButton = Ui.Button("Mark All Absent", Color.FromArgb(220, 53, 69), Color.White);
        _markAllAbsentButton.Location = new Point(500, 42);
        _markAllAbsentButton.Click += (_, _) => MarkAll("Absent");
        selectionPanel.Controls.Add(_markAllAbsentButton);

        // Attendance Grid
        _attendanceGrid.Location = new Point(20, 170);
        _attendanceGrid.Size = new Size(container.Width - 40, 400);
        _attendanceGrid.AutoGenerateColumns = false;
        UiFactory.StyleGrid(_attendanceGrid, _theme);
        _attendanceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _attendanceGrid.RowTemplate.Height = 32;

        // Add columns
        var regColumn = new DataGridViewTextBoxColumn { Name = "RegisterNumber", HeaderText = "Register Number", Width = 140 };
        var nameColumn = new DataGridViewTextBoxColumn { Name = "StudentName", HeaderText = "Student Name", Width = 200 };
        var statusColumn = new DataGridViewComboBoxColumn
        {
            Name = "AttendanceStatus",
            HeaderText = "Attendance",
            Width = 120,
            DataSource = new[] { "Present", "Absent", "Leave" },
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        };

        _attendanceGrid.Columns.AddRange(regColumn, nameColumn, statusColumn);

        container.Controls.Add(_attendanceGrid);

        // Save Button
        _saveButton = Ui.Button("Save Attendance", Color.FromArgb(40, 167, 69), Color.White);
        _saveButton.Location = new Point(20, 580);
        _saveButton.Width = 150;
        _saveButton.Click += SaveButton_Click;
        container.Controls.Add(_saveButton);

        Controls.Add(container);
    }

    private void LoadSections()
    {
        _sectionCombo.Items.Clear();
        var sections = _db.GetDataTable("""
            SELECT DISTINCT s.SectionId, s.SectionName
            FROM FacultySections fs
            JOIN Sections s ON fs.SectionId = s.SectionId
            WHERE fs.FacultyId = $facultyId
            ORDER BY s.SectionName;
            """, ("$facultyId", FacultySessionManager.FacultyId));

        foreach (DataRow row in sections.Rows)
        {
            _sectionCombo.Items.Add($"{row["SectionName"]} ({row["SectionId"]})");
        }

        if (_sectionCombo.Items.Count > 0)
            _sectionCombo.SelectedIndex = 0;
    }

    private void LoadSubjects()
    {
        if (_sectionCombo.SelectedIndex < 0) return;

        _subjectCombo.Items.Clear();
        var selectedSection = _sectionCombo.SelectedItem.ToString() ?? "";
        var sectionIdStr = selectedSection.Split('(').Last().TrimEnd(')');

        if (!int.TryParse(sectionIdStr, out var sectionId)) return;

        var subjects = _db.GetDataTable("""
            SELECT DISTINCT s.SubjectId, s.SubjectCode, s.SubjectName
            FROM FacultySections fs
            JOIN Subjects s ON fs.SubjectId = s.SubjectId
            WHERE fs.FacultyId = $facultyId AND fs.SectionId = $sectionId
            ORDER BY s.SubjectName;
            """, ("$facultyId", FacultySessionManager.FacultyId), ("$sectionId", sectionId));

        foreach (DataRow row in subjects.Rows)
        {
            _subjectCombo.Items.Add($"{row["SubjectCode"]} - {row["SubjectName"]} ({row["SubjectId"]})");
        }

        if (_subjectCombo.Items.Count > 0)
            _subjectCombo.SelectedIndex = 0;
    }

    private void LoadAttendance()
    {
        if (_subjectCombo.SelectedIndex < 0 || _sectionCombo.SelectedIndex < 0) return;

        _attendanceGrid.Rows.Clear();

        var selectedSection = _sectionCombo.SelectedItem.ToString() ?? "";
        var selectedSubject = _subjectCombo.SelectedItem.ToString() ?? "";
        var sectionIdStr = selectedSection.Split('(').Last().TrimEnd(')');
        var subjectIdStr = selectedSubject.Split('(').Last().TrimEnd(')');

        if (!int.TryParse(sectionIdStr, out var sectionId) || !int.TryParse(subjectIdStr, out var subjectId)) return;

        var attendanceDate = _datePicker.Value.ToString("yyyy-MM-dd");

        var students = _db.GetDataTable("""
            SELECT
                sp.StudentId,
                sp.RegisterNumber,
                sp.FullName,
                COALESCE(fad.Status, 'Present') as Status
            FROM StudentSectionMapping ssm
            JOIN StudentProfile sp ON ssm.StudentId = sp.StudentId
            LEFT JOIN FacultyAttendanceDaily fad ON fad.StudentId = sp.StudentId 
                AND fad.SubjectId = $subjectId 
                AND fad.AttendanceDate = $date
            WHERE ssm.SectionId = $sectionId
            ORDER BY sp.RegisterNumber;
            """, ("$sectionId", sectionId), ("$subjectId", subjectId), ("$date", attendanceDate));

        foreach (DataRow row in students.Rows)
        {
            var studentId = (int)row["StudentId"];
            var regNum = row["RegisterNumber"].ToString() ?? "";
            var name = row["FullName"].ToString() ?? "";
            var status = row["Status"].ToString() ?? "Present";

            var rowIndex = _attendanceGrid.Rows.Add(regNum, name, status);
            _attendanceGrid.Rows[rowIndex].Tag = studentId;
        }
    }

    private void MarkAll(string status)
    {
        foreach (DataGridViewRow row in _attendanceGrid.Rows)
        {
            row.Cells["AttendanceStatus"].Value = status;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_attendanceGrid.Rows.Count == 0)
        {
            MessageBox.Show("No attendance records to save", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_subjectCombo.SelectedIndex < 0 || _sectionCombo.SelectedIndex < 0) return;

        var selectedSubject = _subjectCombo.SelectedItem.ToString() ?? "";
        var selectedSection = _sectionCombo.SelectedItem.ToString() ?? "";
        var subjectIdStr = selectedSubject.Split('(').Last().TrimEnd(')');
        var sectionIdStr = selectedSection.Split('(').Last().TrimEnd(')');

        if (!int.TryParse(subjectIdStr, out var subjectId) || !int.TryParse(sectionIdStr, out var sectionId)) return;

        var attendanceDate = _datePicker.Value.ToString("yyyy-MM-dd");

        try
        {
            foreach (DataGridViewRow row in _attendanceGrid.Rows)
            {
                var studentId = (int)row.Tag;
                var status = row.Cells["AttendanceStatus"].Value.ToString() ?? "Present";

                _db.Execute("""
                    INSERT INTO FacultyAttendanceDaily (FacultyId, StudentId, SubjectId, SectionId, AttendanceDate, Status)
                    VALUES ($facultyId, $studentId, $subjectId, $sectionId, $date, $status)
                    ON CONFLICT(FacultyId, StudentId, SubjectId, AttendanceDate) DO UPDATE SET
                        Status = $status;
                    """,
                    ("$facultyId", FacultySessionManager.FacultyId),
                    ("$studentId", studentId),
                    ("$subjectId", subjectId),
                    ("$sectionId", sectionId),
                    ("$date", attendanceDate),
                    ("$status", status));
            }

            MessageBox.Show("Attendance saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadAttendance();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving attendance: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
