using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class MainForm : Form
{
    private readonly StudentDatabase _database;
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Label _student = new();
    private readonly List<Button> _buttons = [];
    private ThemePalette _theme = ThemePalette.Light;
    private bool _dark;
    private string _page = "Dashboard";

    public MainForm(StudentDatabase database)
    {
        _database = database;
        Text = "Student Learning Evidence & Progress Validation System";
        Size = new Size(1380, 830);
        MinimumSize = new Size(1280, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        LoadPage("Dashboard");
    }

    private void BuildShell()
    {
        Controls.Clear();
        _buttons.Clear();
        BackColor = _theme.Background;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildSidebar(), 0, 0);

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = _theme.Background };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(workspace, 1, 0);

        var top = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface };
        workspace.Controls.Add(top, 0, 0);

        _title.Font = Ui.TitleFont;
        _title.ForeColor = _theme.Text;
        _title.AutoSize = true;
        _title.Location = new Point(28, 22);
        top.Controls.Add(_title);

        _student.Text = StudentSessionManager.StudentName;
        _student.Font = Ui.SmallFont;
        _student.ForeColor = _theme.MutedText;
        _student.TextAlign = ContentAlignment.MiddleRight;
        _student.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _student.Location = new Point(Width - 650, 25);
        _student.Size = new Size(250, 28);
        top.Controls.Add(_student);

        var mode = Ui.Button(_dark ? "Light Mode" : "Dark Mode", _theme.SurfaceAlt, _theme.Text);
        mode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        mode.Location = new Point(Width - 390, 20);
        mode.Width = 120;
        mode.Click += (_, _) =>
        {
            _dark = !_dark;
            _theme = _dark ? ThemePalette.Dark : ThemePalette.Light;
            BuildShell();
            LoadPage(_page);
        };
        top.Controls.Add(mode);

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(22);
        _content.BackColor = _theme.Background;
        workspace.Controls.Add(_content, 0, 1);
    }

    private Control BuildSidebar()
    {
        var side = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Sidebar, Padding = new Padding(18) };
        var brand = new Label
        {
            Text = "Student\nModule",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(18, 24),
            Size = new Size(200, 70)
        };
        side.Controls.Add(brand);

        var y = 122;
        foreach (var page in new[]
        {
            "Dashboard", "Profile", "Attendance", "Subjects", "Marks", "Evidence",
            "Notes", "Campus Events", "Progress", "Notifications", "Documents"
        })
        {
            AddNav(side, page, y);
            y += 45;
        }

        var logout = Ui.Button("Logout", Color.FromArgb(57, 72, 91), Color.White);
        logout.FlatAppearance.BorderSize = 0;
        logout.Location = new Point(18, 700);
        logout.Width = 205;
        logout.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        logout.Click += (_, _) => Close();
        side.Controls.Add(logout);
        return side;
    }

    private void AddNav(Control side, string page, int y)
    {
        var button = Ui.Button(page, Color.Transparent, Color.FromArgb(226, 236, 245));
        button.FlatAppearance.BorderSize = 0;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Location = new Point(18, y);
        button.Size = new Size(205, 37);
        button.Tag = page;
        button.Click += (_, _) => LoadPage(page);
        _buttons.Add(button);
        side.Controls.Add(button);
    }

    private void LoadPage(string page)
    {
        _page = page;
        _title.Text = page;
        _content.Controls.Clear();

        UserControl control = page switch
        {
            "Profile" => new StudentProfileControl(_database, _theme),
            "Attendance" => new StudentAttendanceControl(_database, _theme),
            "Subjects" => new StudentSubjectsControl(_database, _theme),
            "Marks" => new StudentMarksControl(_database, _theme),
            "Evidence" => new StudentEvidenceControl(_database, _theme),
            "Notes" => new StudentNotesControl(_database, _theme),
            "Campus Events" => new StudentEventsControl(_database, _theme),
            "Progress" => new StudentProgressControl(_database, _theme),
            "Notifications" => new StudentNotificationsControl(_database, _theme),
            "Documents" => new StudentDocumentsControl(_database, _theme),
            _ => new StudentDashboardControl(_database, _theme)
        };

        control.Dock = DockStyle.Fill;
        _content.Controls.Add(control);
        foreach (var button in _buttons)
        {
            button.BackColor = button.Tag?.ToString() == page ? Color.FromArgb(48, 91, 124) : Color.Transparent;
        }
    }
}
