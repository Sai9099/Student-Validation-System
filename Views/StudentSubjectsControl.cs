using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentSubjectsControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly ComboBox _semester = UiFactory.Combo("Current", "All", "1", "2", "3", "4", "5", "6", "7", "8");
    private readonly TextBox _search = new() { PlaceholderText = "Search subject/code/faculty" };
    private readonly Label _count = new();
    private readonly DataGridView _grid = new();

    public StudentSubjectsControl(StudentDatabase database, ThemePalette theme)
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
        var title = UiFactory.SectionTitle("Semester Subjects Module", _theme);
        title.Location = new Point(16, 16);
        panel.Controls.Add(title);
        _semester.Location = new Point(16, 62);
        _semester.SelectedIndexChanged += (_, _) => LoadData();
        panel.Controls.Add(_semester);
        _search.Location = new Point(215, 62);
        _search.Width = 300;
        _search.TextChanged += (_, _) => LoadData();
        panel.Controls.Add(_search);
        _count.Location = new Point(535, 62);
        _count.Size = new Size(360, 28);
        _count.ForeColor = _theme.MutedText;
        _count.BackColor = Color.Transparent;
        panel.Controls.Add(_count);
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
            ? "ss.Semester = $currentSemester"
            : isAll
                ? "1 = 1"
                : "ss.Semester = $semester";

        var table = _database.GetDataTable($"""
            SELECT ss.Semester, s.SubjectCode, s.SubjectName, s.FacultyName, s.Credits, s.SubjectType, s.SyllabusPath, s.CourseOutcomes
            FROM SemesterSubjects ss JOIN Subjects s ON s.SubjectId = ss.SubjectId
            WHERE {semesterFilter}
              AND ($search = '' OR lower(s.SubjectCode || s.SubjectName || s.FacultyName) LIKE '%' || lower($search) || '%')
            ORDER BY ss.Semester DESC, s.SubjectName;
            """, ("$currentSemester", currentSemester), ("$semester", selectedSemester), ("$search", _search.Text.Trim()));
        _grid.DataSource = table;
        _count.Text = isCurrent
            ? $"Current semester {currentSemester}: {table.Rows.Count} subject(s)"
            : isAll
                ? $"All semesters: {table.Rows.Count} subject(s)"
            : $"Semester {selectedSemester}: {table.Rows.Count} subject(s)";
    }
}
