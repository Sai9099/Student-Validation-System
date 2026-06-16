using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentMarksControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly ComboBox _semester = UiFactory.Combo("Current", "All", "1", "2", "3", "4", "5", "6", "7", "8");
    private readonly DataGridView _internalGrid = new();
    private readonly DataGridView _historyGrid = new();
    private readonly ProgressBar _marksProgress = new();
    private readonly Label _summary = new();

    public StudentMarksControl(StudentDatabase database, ThemePalette theme)
    {
        _database = database;
        _theme = theme;
        Build();
        LoadData();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);

        var current = new TabPage("Internal Marks") { BackColor = _theme.Background };
        var p1 = Ui.Card(_theme, 18);
        p1.Dock = DockStyle.Fill;
        current.Controls.Add(p1);
        tabs.TabPages.Add(current);

        var title = UiFactory.SectionTitle("Marks Module", _theme);
        title.Location = new Point(16, 16);
        p1.Controls.Add(title);
        _semester.Location = new Point(16, 62);
        _semester.SelectedIndexChanged += (_, _) => LoadData();
        p1.Controls.Add(_semester);
        _marksProgress.Location = new Point(220, 66);
        _marksProgress.Size = new Size(360, 18);
        p1.Controls.Add(_marksProgress);
        _summary.Location = new Point(600, 58);
        _summary.Size = new Size(560, 36);
        _summary.ForeColor = _theme.Text;
        p1.Controls.Add(_summary);
        _internalGrid.Location = new Point(16, 115);
        _internalGrid.Size = new Size(1100, 490);
        _internalGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_internalGrid, _theme);
        p1.Controls.Add(_internalGrid);

        var history = new TabPage("All Semester History") { BackColor = _theme.Background };
        var p2 = Ui.Card(_theme, 18);
        p2.Dock = DockStyle.Fill;
        history.Controls.Add(p2);
        tabs.TabPages.Add(history);
        _historyGrid.Location = new Point(16, 58);
        _historyGrid.Size = new Size(1100, 520);
        _historyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_historyGrid, _theme);
        p2.Controls.Add(UiFactory.SectionTitle("GPA / CGPA History", _theme));
        p2.Controls.Add(_historyGrid);
    }

    private void LoadData()
    {
        var academic = _database.GetAcademicDetails(StudentSessionManager.StudentId);
        var currentSemester = Math.Clamp(academic.CurrentSemester, 1, 8);
        var selectedSemester = _semester.Text;
        var isCurrent = selectedSemester == "Current";
        var isAll = selectedSemester == "All";
        var where = isCurrent
            ? "AND im.Semester = $currentSemester"
            : isAll
                ? ""
                : "AND im.Semester = $semester";

        var table = _database.GetDataTable($"""
            SELECT im.Semester, s.SubjectCode, s.SubjectName, im.AssignmentMarks, im.QuizMarks, im.MidExamMarks,
                   im.LabMarks, im.ExternalMarks, im.TotalMarks, im.Grade, im.GPA
            FROM InternalMarks im JOIN Subjects s ON s.SubjectId = im.SubjectId
            WHERE im.StudentId = $studentId {where}
            ORDER BY im.Semester DESC, s.SubjectName;
            """, ("$studentId", StudentSessionManager.StudentId), ("$currentSemester", currentSemester), ("$semester", selectedSemester));
        _internalGrid.DataSource = table;

        var average = Convert.ToDouble(_database.Scalar($"""
            SELECT IFNULL(AVG(TotalMarks), 0) FROM InternalMarks im WHERE im.StudentId=$studentId {where};
            """, ("$studentId", StudentSessionManager.StudentId), ("$currentSemester", currentSemester), ("$semester", selectedSemester)));
        _marksProgress.Value = Math.Min(100, Math.Max(0, (int)Math.Round(average)));

        var label = isCurrent
            ? $"Current semester {currentSemester}"
            : isAll
                ? "All semesters"
                : $"Semester {selectedSemester}";
        _summary.Text = $"{label}: {table.Rows.Count} subject(s) | Average: {average:0.0}/100 | CGPA: {academic.CGPA:0.00}";
        _historyGrid.DataSource = _database.GetDataTable("SELECT Semester, GPA, CGPA, ResultStatus, PublishedDate FROM Marks WHERE StudentId=$studentId ORDER BY Semester;", ("$studentId", StudentSessionManager.StudentId));
    }
}
