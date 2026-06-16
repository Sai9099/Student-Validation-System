using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentDashboardControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;

    public StudentDashboardControl(StudentDatabase database, ThemePalette theme)
    {
        _database = database;
        _theme = theme;
        Build();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        AutoScroll = true;
        var id = StudentSessionManager.StudentId;
        var summary = _database.GetDashboardSummary(id);

        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, BackColor = _theme.Background };
        for (var i = 0; i < 4; i++) root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        Controls.Add(root);

        root.Controls.Add(Card("Current Semester", summary.CurrentSemester.ToString(), _theme.Primary), 0, 0);
        root.Controls.Add(Card("Attendance", $"{summary.AttendancePercentage:0.0}%", summary.AttendancePercentage < 75 ? _theme.Danger : _theme.Success), 1, 0);
        root.Controls.Add(Card("CGPA", $"{summary.CGPA:0.00}", _theme.Primary), 2, 0);
        root.Controls.Add(Card("Total Subjects", summary.TotalSubjects.ToString(), _theme.Primary), 3, 0);
        root.Controls.Add(Card("Pending Evidence", summary.PendingEvidence.ToString(), _theme.Warning), 0, 1);
        root.Controls.Add(Card("Approved Evidence", summary.ApprovedEvidence.ToString(), _theme.Success), 1, 1);
        root.Controls.Add(Card("Upcoming Events", summary.UpcomingEvents.ToString(), _theme.Primary), 2, 1);
        root.Controls.Add(Card("Latest Internal Marks", $"{summary.LatestInternalMarks:0.0}", _theme.Primary), 3, 1);

        var latestMarks = PanelWithGrid("Latest Internal Marks", """
            SELECT s.SubjectCode, s.SubjectName, im.AssignmentMarks, im.QuizMarks, im.MidExamMarks, im.TotalMarks, im.Grade
            FROM InternalMarks im JOIN Subjects s ON s.SubjectId = im.SubjectId
            WHERE im.StudentId = $studentId
            ORDER BY im.InternalMarkId DESC LIMIT 5;
            """);
        root.SetColumnSpan(latestMarks, 2);
        root.Controls.Add(latestMarks, 0, 2);

        var events = PanelWithGrid("Upcoming Campus Events", """
            SELECT EventTitle, EventDate, Venue, ConductedBy, EventType, RegistrationDeadline
            FROM CampusEvents
            WHERE EventDate >= date('now')
            ORDER BY EventDate LIMIT 5;
            """);
        root.SetColumnSpan(events, 2);
        root.Controls.Add(events, 2, 2);

        var notifications = PanelWithGrid("Recent Notifications", """
            SELECT NotificationType, Title, Message, IsRead, CreatedAt
            FROM Notifications
            WHERE StudentId = $studentId
            ORDER BY CreatedAt DESC LIMIT 6;
            """);
        root.SetColumnSpan(notifications, 4);
        root.Controls.Add(notifications, 0, 3);
    }

    private Control Card(string title, string value, Color accent)
    {
        var card = Ui.Card(_theme, 16);
        card.Height = 118;
        card.Dock = DockStyle.Fill;
        var t = Ui.Label(title, Ui.BodyFont, _theme.MutedText);
        t.Location = new Point(14, 12);
        card.Controls.Add(t);
        var v = Ui.Label(value, new Font("Segoe UI", 26, FontStyle.Bold), accent);
        v.Location = new Point(14, 44);
        card.Controls.Add(v);
        return card;
    }

    private Control PanelWithGrid(string title, string sql)
    {
        var panel = Ui.Card(_theme, 16);
        panel.Height = 270;
        panel.Dock = DockStyle.Fill;
        var label = UiFactory.SectionTitle(title, _theme);
        label.Location = new Point(12, 10);
        panel.Controls.Add(label);

        var grid = new DataGridView { Location = new Point(12, 52), Size = new Size(520, 180), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        UiFactory.StyleGrid(grid, _theme);
        grid.DataSource = _database.GetDataTable(sql, ("$studentId", StudentSessionManager.StudentId));
        panel.Controls.Add(grid);
        return panel;
    }
}
