using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentAttendanceControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly ComboBox _semester = UiFactory.Combo("Current", "All", "1", "2", "3", "4", "5", "6", "7", "8");
    private readonly DataGridView _grid = new();
    private readonly ProgressBar _overall = new();
    private readonly Label _summary = new();

    public StudentAttendanceControl(StudentDatabase database, ThemePalette theme)
    {
        _database = database;
        _theme = theme;
        Build();
        LoadData();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        Controls.Add(panel);

        var title = UiFactory.SectionTitle("Attendance Module", _theme);
        title.Location = new Point(16, 16);
        panel.Controls.Add(title);

        _semester.Location = new Point(16, 62);
        _semester.SelectedIndexChanged += (_, _) => LoadData();
        panel.Controls.Add(_semester);

        _overall.Location = new Point(220, 66);
        _overall.Size = new Size(360, 18);
        panel.Controls.Add(_overall);

        _summary.Location = new Point(600, 58);
        _summary.Size = new Size(500, 36);
        _summary.ForeColor = _theme.Text;
        _summary.BackColor = Color.Transparent;
        panel.Controls.Add(_summary);

        _grid.Location = new Point(16, 115);
        _grid.Size = new Size(1100, 500);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_grid, _theme);
        panel.Controls.Add(_grid);
    }

    private void LoadData()
    {
        var academic = _database.GetAcademicDetails(StudentSessionManager.StudentId);
        var currentSemester = Math.Clamp(academic.CurrentSemester, 1, 8);
        var selectedSemester = _semester.Text;
        var isCurrent = selectedSemester == "Current";
        var isAll = selectedSemester == "All";
        var semesterFilter = isCurrent
            ? "AND a.Semester = $currentSemester"
            : isAll
                ? ""
                : "AND a.Semester = $semester";

        var table = _database.GetDataTable($"""
            SELECT a.Semester, s.SubjectCode, s.SubjectName, s.FacultyName, a.PresentCount, a.AbsentCount,
                   a.TotalClassesConducted, a.AttendancePercentage, a.MinimumRequired,
                   CASE WHEN a.AttendancePercentage < a.MinimumRequired THEN 'Shortage' ELSE 'OK' END AS Alert
            FROM Attendance a JOIN Subjects s ON s.SubjectId = a.SubjectId
            WHERE a.StudentId = $studentId {semesterFilter}
            ORDER BY a.Semester DESC, s.SubjectName;
            """, ("$studentId", StudentSessionManager.StudentId), ("$currentSemester", currentSemester), ("$semester", selectedSemester));
        _grid.DataSource = table;

        var percentage = Convert.ToDouble(_database.Scalar($"""
            SELECT IFNULL(ROUND(SUM(PresentCount) * 100.0 / NULLIF(SUM(TotalClassesConducted), 0), 2), 0)
            FROM Attendance a WHERE a.StudentId = $studentId {semesterFilter};
            """, ("$studentId", StudentSessionManager.StudentId), ("$currentSemester", currentSemester), ("$semester", selectedSemester)));
        _overall.Value = Math.Min(100, Math.Max(0, (int)Math.Round(percentage)));
        var label = isCurrent
            ? $"Current semester {currentSemester}"
            : isAll
                ? "All semesters"
                : $"Semester {selectedSemester}";
        _summary.Text = $"{label}: {table.Rows.Count} subject(s) | Overall attendance: {percentage:0.00}% | Minimum required: 75%";
        _summary.ForeColor = percentage < 75 ? _theme.Danger : _theme.Success;
    }
}
