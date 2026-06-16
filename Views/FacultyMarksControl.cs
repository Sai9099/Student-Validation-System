using System.Data;
using System.Text;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyMarksControl : Control
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly StudentDatabase _db = new();
    private ComboBox _sectionCombo = new();
    private ComboBox _subjectCombo = new();
    private ComboBox _examTypeCombo = new();
    private NumericUpDown _maxMarksInput = new();
    private DataGridView _marksGrid = new();
    private Button _generateReportButton = new();
    private Button _saveButton = new();

    public FacultyMarksControl()
    {
        Dock = DockStyle.Fill;
        BackColor = _theme.Background;
        Build();
        LoadSections();
    }

    private void Build()
    {
        var container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _theme.Background };

        var title = Ui.Label("Marks Management", Ui.TitleFont, _theme.Text);
        title.Location = new Point(22, 18);
        title.AutoSize = true;
        container.Controls.Add(title);

        // Selection Panel
        var selectionPanel = new Panel { Location = new Point(20, 60), Size = new Size(container.Width - 40, 110), BackColor = _theme.Surface };
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
        _subjectCombo.SelectedIndexChanged += (_, _) => LoadMarks();
        selectionPanel.Controls.Add(_subjectCombo);

        // Exam Type
        var examLabel = Ui.Label("Exam Type:", labelFont, _theme.Text);
        examLabel.Location = new Point(10, 72);
        examLabel.AutoSize = true;
        selectionPanel.Controls.Add(examLabel);

        _examTypeCombo.Location = new Point(inputX, 72);
        _examTypeCombo.Size = new Size(inputWidth, 24);
        _examTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _examTypeCombo.Items.AddRange("Internal", "Quiz", "Assignment", "Midterm", "Practical", "External");
        _examTypeCombo.SelectedIndex = 0;
        _examTypeCombo.SelectedIndexChanged += (_, _) => LoadMarks();
        selectionPanel.Controls.Add(_examTypeCombo);

        // Max Marks
        var maxMarksLabel = Ui.Label("Max Marks:", labelFont, _theme.Text);
        maxMarksLabel.Location = new Point(380, 72);
        maxMarksLabel.AutoSize = true;
        selectionPanel.Controls.Add(maxMarksLabel);

        _maxMarksInput.Location = new Point(inputX + 230, 72);
        _maxMarksInput.Size = new Size(100, 24);
        _maxMarksInput.Maximum = 999;
        _maxMarksInput.Value = 100;
        _maxMarksInput.ValueChanged += (_, _) => LoadMarks();
        selectionPanel.Controls.Add(_maxMarksInput);

        // Marks Grid
        _marksGrid.Location = new Point(20, 185);
        _marksGrid.Size = new Size(container.Width - 40, 380);
        _marksGrid.AutoGenerateColumns = false;
        UiFactory.StyleGrid(_marksGrid, _theme);
        _marksGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _marksGrid.RowTemplate.Height = 32;

        // Add columns
        var regColumn = new DataGridViewTextBoxColumn { Name = "RegisterNumber", HeaderText = "Register Number", Width = 140, ReadOnly = true };
        var nameColumn = new DataGridViewTextBoxColumn { Name = "StudentName", HeaderText = "Student Name", Width = 200, ReadOnly = true };
        var marksColumn = new DataGridViewNumericColumn { Name = "MarksObtained", HeaderText = "Marks Obtained", Width = 120 };
        var gradeColumn = new DataGridViewComboBoxColumn
        {
            Name = "Grade",
            HeaderText = "Grade",
            Width = 80,
            DataSource = new[] { "O", "A+", "A", "B+", "B", "C+", "C", "F" }
        };

        _marksGrid.Columns.AddRange(regColumn, nameColumn, marksColumn, gradeColumn);

        container.Controls.Add(_marksGrid);

        // Button Panel
        var buttonPanel = new Panel { Location = new Point(20, 575), Size = new Size(container.Width - 40, 50), BackColor = Color.Transparent };
        container.Controls.Add(buttonPanel);

        _saveButton = Ui.Button("Save Marks", Color.FromArgb(40, 167, 69), Color.White);
        _saveButton.Location = new Point(0, 0);
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);

        _generateReportButton = Ui.Button("Generate Report", _theme.Primary, Color.White);
        _generateReportButton.Location = new Point(150, 0);
        _generateReportButton.Click += (_, _) => GenerateReport();
        buttonPanel.Controls.Add(_generateReportButton);

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

    private void LoadMarks()
    {
        if (_subjectCombo.SelectedIndex < 0 || _sectionCombo.SelectedIndex < 0) return;

        _marksGrid.Rows.Clear();

        var selectedSection = _sectionCombo.SelectedItem.ToString() ?? "";
        var selectedSubject = _subjectCombo.SelectedItem.ToString() ?? "";
        var sectionIdStr = selectedSection.Split('(').Last().TrimEnd(')');
        var subjectIdStr = selectedSubject.Split('(').Last().TrimEnd(')');

        if (!int.TryParse(sectionIdStr, out var sectionId) || !int.TryParse(subjectIdStr, out var subjectId)) return;

        var examType = _examTypeCombo.SelectedItem?.ToString() ?? "Internal";

        var students = _db.GetDataTable("""
            SELECT
                sp.StudentId,
                sp.RegisterNumber,
                sp.FullName,
                COALESCE(fmr.MarksObtained, 0) as MarksObtained,
                COALESCE(fmr.Grade, '') as Grade
            FROM StudentSectionMapping ssm
            JOIN StudentProfile sp ON ssm.StudentId = sp.StudentId
            LEFT JOIN FacultyMarksRecords fmr ON fmr.StudentId = sp.StudentId 
                AND fmr.SubjectId = $subjectId 
                AND fmr.ExamType = $examType
            WHERE ssm.SectionId = $sectionId
            ORDER BY sp.RegisterNumber;
            """, ("$sectionId", sectionId), ("$subjectId", subjectId), ("$examType", examType));

        foreach (DataRow row in students.Rows)
        {
            var studentId = (int)row["StudentId"];
            var regNum = row["RegisterNumber"].ToString() ?? "";
            var name = row["FullName"].ToString() ?? "";
            var marks = (int)Convert.ToDouble(row["MarksObtained"]);
            var grade = row["Grade"].ToString() ?? "";

            var rowIndex = _marksGrid.Rows.Add(regNum, name, marks, grade);
            _marksGrid.Rows[rowIndex].Tag = studentId;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_marksGrid.Rows.Count == 0)
        {
            MessageBox.Show("No marks records to save", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_subjectCombo.SelectedIndex < 0 || _sectionCombo.SelectedIndex < 0) return;

        var selectedSubject = _subjectCombo.SelectedItem.ToString() ?? "";
        var selectedSection = _sectionCombo.SelectedItem.ToString() ?? "";
        var subjectIdStr = selectedSubject.Split('(').Last().TrimEnd(')');
        var sectionIdStr = selectedSection.Split('(').Last().TrimEnd(')');

        if (!int.TryParse(subjectIdStr, out var subjectId) || !int.TryParse(sectionIdStr, out var sectionId)) return;

        var examType = _examTypeCombo.SelectedItem?.ToString() ?? "Internal";
        var maxMarks = (int)_maxMarksInput.Value;

        try
        {
            foreach (DataGridViewRow row in _marksGrid.Rows)
            {
                var studentId = (int)row.Tag;
                var marksObtained = Convert.ToDouble(row.Cells["MarksObtained"].Value ?? 0);
                var grade = row.Cells["Grade"].Value?.ToString() ?? "";

                if (marksObtained < 0 || marksObtained > maxMarks)
                {
                    MessageBox.Show($"Invalid marks for row {row.Index + 1}. Marks should be between 0 and {maxMarks}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _db.Execute("""
                    INSERT INTO FacultyMarksRecords (FacultyId, StudentId, SubjectId, SectionId, ExamType, MaxMarks, MarksObtained, Grade, Semester)
                    VALUES ($facultyId, $studentId, $subjectId, $sectionId, $examType, $maxMarks, $marks, $grade, 4)
                    ON CONFLICT() DO UPDATE SET
                        MarksObtained = $marks, Grade = $grade, UpdatedAt = CURRENT_TIMESTAMP;
                    """,
                    ("$facultyId", FacultySessionManager.FacultyId),
                    ("$studentId", studentId),
                    ("$subjectId", subjectId),
                    ("$sectionId", sectionId),
                    ("$examType", examType),
                    ("$maxMarks", maxMarks),
                    ("$marks", marksObtained),
                    ("$grade", grade));
            }

            MessageBox.Show("Marks saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadMarks();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving marks: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GenerateReport()
    {
        if (_marksGrid.Rows.Count == 0)
        {
            MessageBox.Show("No marks data to generate report", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var report = new StringBuilder();
        report.AppendLine("=== MARKS REPORT ===");
        report.AppendLine($"Section: {_sectionCombo.SelectedItem}");
        report.AppendLine($"Subject: {_subjectCombo.SelectedItem}");
        report.AppendLine($"Exam Type: {_examTypeCombo.SelectedItem}");
        report.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine(new string('-', 80));
        report.AppendLine($"{"Register",-15} {"Student Name",-30} {"Marks",-10} {"Grade"}");
        report.AppendLine(new string('-', 80));

        var totalMarks = 0.0;
        var count = 0;

        foreach (DataGridViewRow row in _marksGrid.Rows)
        {
            var reg = row.Cells["RegisterNumber"].Value.ToString() ?? "";
            var name = row.Cells["StudentName"].Value.ToString() ?? "";
            var marks = Convert.ToDouble(row.Cells["MarksObtained"].Value ?? 0);
            var grade = row.Cells["Grade"].Value?.ToString() ?? "";

            report.AppendLine($"{reg,-15} {name,-30} {marks,-10:F2} {grade}");
            totalMarks += marks;
            count++;
        }

        report.AppendLine(new string('-', 80));
        if (count > 0)
        {
            var average = totalMarks / count;
            report.AppendLine($"Average Marks: {average:F2}");
        }

        MessageBox.Show(report.ToString(), "Marks Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// Custom numeric column for DataGridView
public class DataGridViewNumericColumn : DataGridViewTextBoxColumn
{
    public DataGridViewNumericColumn()
    {
        this.CellTemplate = new DataGridViewNumericCell();
    }
}

public class DataGridViewNumericCell : DataGridViewTextBoxCell
{
    public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
        if (DataGridView.EditingControl is TextBox textBox)
        {
            textBox.KeyPress += TextBox_KeyPress;
        }
    }

    private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
        {
            e.Handled = true;
        }
    }
}
