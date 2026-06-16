using System.Data;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultySectionsControl : Control
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly StudentDatabase _db = new();
    private FlowLayoutPanel _sectionsPanel = new();
    private List<(int SectionId, string SectionName)> _sections = [];

    public FacultySectionsControl()
    {
        Dock = DockStyle.Fill;
        BackColor = _theme.Background;
        Build();
        LoadSections();
    }

    private void Build()
    {
        var container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _theme.Background };

        var title = Ui.Label("My Sections", Ui.TitleFont, _theme.Text);
        title.Location = new Point(22, 18);
        title.AutoSize = true;
        container.Controls.Add(title);

        _sectionsPanel.FlowDirection = FlowDirection.LeftToRight;
        _sectionsPanel.WrapContents = true;
        _sectionsPanel.Location = new Point(20, 60);
        _sectionsPanel.Size = new Size(container.Width - 40, container.Height - 80);
        _sectionsPanel.Dock = DockStyle.Fill;
        _sectionsPanel.Padding = new Padding(10);
        _sectionsPanel.AutoScroll = true;
        _sectionsPanel.BackColor = _theme.Background;
        container.Controls.Add(_sectionsPanel);

        Controls.Add(container);
    }

    private void LoadSections()
    {
        _sectionsPanel.Controls.Clear();
        _sections.Clear();

        var sectionsData = _db.GetDataTable("""
            SELECT DISTINCT
                s.SectionId,
                s.SectionName,
                s.Department,
                s.Year,
                s.Semester,
                s.TotalStudents,
                GROUP_CONCAT(DISTINCT subj.SubjectName, ', ') as SubjectsHandled,
                (SELECT COUNT(*) FROM StudentSectionMapping WHERE SectionId = s.SectionId) as StudentCount,
                COALESCE(ROUND(AVG(fad.Status = 'Present'), 2) * 100, 0) as AvgAttendance,
                COALESCE(ROUND(AVG(fmr.MarksObtained), 2), 0) as AvgMarks
            FROM FacultySections fs
            JOIN Sections s ON fs.SectionId = s.SectionId
            JOIN Subjects subj ON fs.SubjectId = subj.SubjectId
            LEFT JOIN FacultyAttendanceDaily fad ON fad.SectionId = s.SectionId AND fad.FacultyId = $facultyId
            LEFT JOIN FacultyMarksRecords fmr ON fmr.SectionId = s.SectionId AND fmr.FacultyId = $facultyId
            WHERE fs.FacultyId = $facultyId
            GROUP BY s.SectionId
            ORDER BY s.SectionName;
            """, ("$facultyId", FacultySessionManager.FacultyId));

        foreach (DataRow row in sectionsData.Rows)
        {
            var sectionId = (int)row["SectionId"];
            var sectionName = row["SectionName"].ToString() ?? "";
            var department = row["Department"].ToString() ?? "";
            var year = (int)row["Year"];
            var semester = (int)row["Semester"];
            var totalStudents = (int)row["TotalStudents"];
            var studentCount = (int)row["StudentCount"];
            var subjectsHandled = row["SubjectsHandled"].ToString() ?? "";
            var avgAttendance = Convert.ToDouble(row["AvgAttendance"]);
            var avgMarks = Convert.ToDouble(row["AvgMarks"]);

            var card = CreateSectionCard(sectionId, sectionName, department, year, semester, totalStudents, 
                studentCount, subjectsHandled, avgAttendance, avgMarks);
            _sectionsPanel.Controls.Add(card);
            _sections.Add((sectionId, sectionName));
        }
    }

    private Panel CreateSectionCard(int sectionId, string sectionName, string department, int year, 
        int semester, int totalStudents, int studentCount, string subjectsHandled, 
        double avgAttendance, double avgMarks)
    {
        var card = Ui.Card(_theme, 12);
        card.Size = new Size(350, 320);
        card.Padding = new Padding(16);

        var y = 10;

        // Section Name (Title)
        var titleLabel = Ui.Label(sectionName, new Font("Segoe UI", 14, FontStyle.Bold), _theme.Text);
        titleLabel.Location = new Point(10, y);
        titleLabel.AutoSize = true;
        card.Controls.Add(titleLabel);

        y += 35;

        // Department and Semester
        var infoLabel = Ui.Label($"{department} | Year {year} | Semester {semester}", Ui.SmallFont, _theme.MutedText);
        infoLabel.Location = new Point(10, y);
        infoLabel.AutoSize = true;
        card.Controls.Add(infoLabel);

        y += 25;

        // Subjects Handled
        var subjLabel = Ui.Label("Subjects Handled:", Ui.BodyFont, _theme.MutedText);
        subjLabel.Location = new Point(10, y);
        card.Controls.Add(subjLabel);

        y += 22;
        var subjects = new Label
        {
            Text = subjectsHandled,
            Font = Ui.SmallFont,
            ForeColor = _theme.Text,
            Location = new Point(10, y),
            AutoSize = true,
            MaximumSize = new Size(320, 60)
        };
        card.Controls.Add(subjects);

        y = 160;

        // Statistics
        var statsPanel = new Panel { Location = new Point(10, y), Size = new Size(330, 90), BackColor = Color.Transparent };
        card.Controls.Add(statsPanel);

        var statY = 0;
        CreateStatLine(statsPanel, $"Students: {studentCount}/{totalStudents}", statY);
        statY += 22;
        CreateStatLine(statsPanel, $"Avg Attendance: {avgAttendance:F1}%", statY);
        statY += 22;
        CreateStatLine(statsPanel, $"Avg Internal Marks: {avgMarks:F2}", statY);
        statY += 22;
        CreateStatLine(statsPanel, $"Pending Evidence: 0", statY);

        // View Details Button
        var btnView = Ui.Button("View Section", _theme.Primary, Color.White);
        btnView.Location = new Point(10, 260);
        btnView.Width = 330;
        btnView.Click += (_, _) => ViewSectionDetails(sectionId, sectionName);
        card.Controls.Add(btnView);

        return card;
    }

    private void CreateStatLine(Panel parent, string text, int y)
    {
        var label = Ui.Label(text, Ui.SmallFont, _theme.Text);
        label.Location = new Point(0, y);
        label.AutoSize = true;
        parent.Controls.Add(label);
    }

    private void ViewSectionDetails(int sectionId, string sectionName)
    {
        MessageBox.Show($"View section details for: {sectionName}\n\nSection ID: {sectionId}", "Section Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        // This will navigate to student management page for this section
    }
}
