using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentNotificationsControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _filter = UiFactory.Combo("All", "Unread only", "LowAttendance", "PendingEvidence", "UpcomingEvent", "MarksUpdated", "ResultPublished", "ProfileIncomplete");

    public StudentNotificationsControl(StudentDatabase database, ThemePalette theme)
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
        var title = UiFactory.SectionTitle("Notifications Module", _theme);
        title.Location = new Point(16, 16);
        panel.Controls.Add(title);
        _filter.Location = new Point(16, 62);
        _filter.SelectedIndexChanged += (_, _) => LoadData();
        panel.Controls.Add(_filter);
        var read = UiFactory.PrimaryButton("Mark Selected Read", _theme);
        read.Location = new Point(215, 58);
        read.Width = 170;
        read.Click += (_, _) => MarkRead();
        panel.Controls.Add(read);
        _grid.Location = new Point(16, 115);
        _grid.Size = new Size(1100, 500);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_grid, _theme);
        panel.Controls.Add(_grid);
    }

    private void LoadData()
    {
        var extra = _filter.Text switch
        {
            "Unread only" => "AND IsRead = 0",
            "All" => "",
            _ => "AND NotificationType = $type"
        };
        _grid.DataSource = _database.GetDataTable($"""
            SELECT NotificationId, NotificationType, Title, Message, IsRead, CreatedAt
            FROM Notifications
            WHERE StudentId=$studentId {extra}
            ORDER BY CreatedAt DESC;
            """, ("$studentId", StudentSessionManager.StudentId), ("$type", _filter.Text));
        if (_grid.Columns["NotificationId"] is DataGridViewColumn c) c.Visible = false;
    }

    private void MarkRead()
    {
        if (_grid.CurrentRow == null) return;
        _database.MarkNotificationRead(Convert.ToInt32(_grid.CurrentRow.Cells["NotificationId"].Value));
        LoadData();
    }
}
