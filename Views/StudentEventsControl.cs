using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentEventsControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly DataGridView _events = new();
    private readonly DataGridView _registered = new();
    private readonly ComboBox _type = UiFactory.Combo("All", "Technical", "Cultural", "Sports", "Workshop", "Seminar");

    public StudentEventsControl(StudentDatabase database, ThemePalette theme)
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

        var upcoming = new TabPage("Upcoming Events") { BackColor = _theme.Background };
        var p1 = Ui.Card(_theme, 18);
        p1.Dock = DockStyle.Fill;
        upcoming.Controls.Add(p1);
        tabs.TabPages.Add(upcoming);
        var title = UiFactory.SectionTitle("Campus Events Module", _theme);
        title.Location = new Point(16, 16);
        p1.Controls.Add(title);
        _type.Location = new Point(16, 62);
        _type.SelectedIndexChanged += (_, _) => LoadData();
        p1.Controls.Add(_type);
        var register = UiFactory.PrimaryButton("Register Interest", _theme);
        register.Location = new Point(215, 58);
        register.Width = 160;
        register.Click += (_, _) => Register();
        p1.Controls.Add(register);
        _events.Location = new Point(16, 115);
        _events.Size = new Size(1100, 500);
        _events.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_events, _theme);
        p1.Controls.Add(_events);

        var mine = new TabPage("My Registered Events") { BackColor = _theme.Background };
        var p2 = Ui.Card(_theme, 18);
        p2.Dock = DockStyle.Fill;
        mine.Controls.Add(p2);
        tabs.TabPages.Add(mine);
        _registered.Location = new Point(16, 60);
        _registered.Size = new Size(1100, 520);
        _registered.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_registered, _theme);
        p2.Controls.Add(UiFactory.SectionTitle("My Registered Events List", _theme));
        p2.Controls.Add(_registered);
    }

    private void LoadData()
    {
        var typeFilter = _type.Text == "All" ? "" : "AND EventType=$type";
        _events.DataSource = _database.GetDataTable($"""
            SELECT EventId, EventTitle, EventDate, Venue, ConductedBy, EventDescription, RegistrationDeadline, EventType
            FROM CampusEvents
            WHERE EventDate >= date('now') {typeFilter}
            ORDER BY EventDate;
            """, ("$type", _type.Text));
        if (_events.Columns["EventId"] is DataGridViewColumn c) c.Visible = false;
        _registered.DataSource = _database.GetDataTable("""
            SELECT ce.EventTitle, ce.EventDate, ce.Venue, ce.ConductedBy, ce.EventType, er.RegistrationDate, er.Status
            FROM EventRegistrations er JOIN CampusEvents ce ON ce.EventId=er.EventId
            WHERE er.StudentId=$studentId ORDER BY ce.EventDate DESC;
            """, ("$studentId", StudentSessionManager.StudentId));
    }

    private void Register()
    {
        if (_events.CurrentRow == null) return;
        var id = Convert.ToInt32(_events.CurrentRow.Cells["EventId"].Value);
        _database.RegisterForEvent(StudentSessionManager.StudentId, id);
        LoadData();
        MessageBox.Show("Interest registered successfully.", "Campus Events", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
