using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Models;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentProfileControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly Dictionary<string, TextBox> _fields = [];
    private FlowLayoutPanel _profilePreview = new();
    private DataGridView _activities = new();
    private DataGridView _achievements = new();

    public StudentProfileControl(StudentDatabase database, ThemePalette theme)
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
        tabs.TabPages.Add(BuildDetailsTab());
        tabs.TabPages.Add(BuildActivitiesTab());
        tabs.TabPages.Add(BuildAchievementsTab());
    }

    private TabPage BuildDetailsTab()
    {
        var page = new TabPage("Profile Details") { BackColor = _theme.Background };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _theme.Background,
            ColumnCount = 2,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);

        var editPanel = Ui.Card(_theme, 0);
        editPanel.Dock = DockStyle.Fill;
        layout.Controls.Add(editPanel, 0, 0);

        var editScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = _theme.Surface,
            Padding = new Padding(18)
        };
        editPanel.Controls.Add(editScroll);

        var y = 18;
        var title = UiFactory.SectionTitle("Fill Student Details", _theme);
        title.Location = new Point(18, y);
        editScroll.Controls.Add(title);
        y += 46;

        AddProfileSection(editScroll, "Personal Details", ref y);
        AddProfileField(editScroll, "FullName", "Full Name", ref y);
        AddProfileField(editScroll, "RegisterNumber", "Register Number", ref y);
        AddProfileField(editScroll, "DateOfBirth", "Date Of Birth", ref y);
        AddProfileField(editScroll, "Gender", "Gender", ref y);
        AddProfileField(editScroll, "Email", "Email", ref y);
        AddProfileField(editScroll, "MobileNumber", "Mobile Number", ref y);
        AddProfileField(editScroll, "Address", "Address", ref y, 70);
        AddProfileField(editScroll, "ProfilePhotoPath", "Profile Photo Path", ref y);

        var browse = Ui.Button("Browse Photo", _theme.SurfaceAlt, _theme.Text);
        browse.Location = new Point(18, y);
        browse.Width = 150;
        browse.Click += (_, _) => BrowseInto("ProfilePhotoPath");
        editScroll.Controls.Add(browse);
        y += 56;

        AddProfileSection(editScroll, "Academic Details", ref y);
        AddProfileField(editScroll, "Department", "Department", ref y);
        AddProfileField(editScroll, "Degree", "Degree", ref y);
        AddProfileField(editScroll, "Year", "Year", ref y);
        AddProfileField(editScroll, "CurrentSemester", "Current Semester", ref y);
        AddProfileField(editScroll, "Section", "Section", ref y);
        AddProfileField(editScroll, "Batch", "Batch", ref y);
        AddProfileField(editScroll, "RollNumber", "Roll Number", ref y);
        AddProfileField(editScroll, "AdmissionNumber", "Admission Number", ref y);
        AddProfileField(editScroll, "MentorName", "Mentor / Faculty Advisor", ref y);
        AddProfileField(editScroll, "CGPA", "CGPA", ref y);
        AddProfileField(editScroll, "Backlogs", "Backlogs", ref y);

        AddProfileSection(editScroll, "Parent / Guardian Details", ref y);
        AddProfileField(editScroll, "FatherName", "Father Name", ref y);
        AddProfileField(editScroll, "MotherName", "Mother Name", ref y);
        AddProfileField(editScroll, "ParentMobileNumber", "Parent Mobile Number", ref y);
        AddProfileField(editScroll, "ParentEmail", "Parent Email", ref y);
        AddProfileField(editScroll, "EmergencyContact", "Emergency Contact", ref y);

        var save = UiFactory.PrimaryButton("Save Profile", _theme);
        save.Location = new Point(18, y + 8);
        save.Width = 180;
        save.Click += (_, _) => Save();
        editScroll.Controls.Add(save);
        editScroll.AutoScrollMinSize = new Size(0, y + 90);

        var viewPanel = Ui.Card(_theme, 0);
        viewPanel.Dock = DockStyle.Fill;
        layout.Controls.Add(viewPanel, 1, 0);

        _profilePreview = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = _theme.Surface,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(24)
        };
        viewPanel.Controls.Add(_profilePreview);

        return page;
    }

    private TabPage BuildActivitiesTab()
    {
        var page = new TabPage("Extra-Curricular") { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);
        AddQuickAdd(panel, "Activity", ["Club participation", "Event participation", "Sports", "Cultural activities", "Volunteering", "Leadership roles"], AddActivity);
        _activities = Grid(panel, 130);
        return page;
    }

    private TabPage BuildAchievementsTab()
    {
        var page = new TabPage("Achievements") { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);
        AddQuickAdd(panel, "Achievement", ["Certifications", "Awards", "Hackathons", "Internships", "Workshops", "Publications", "Projects"], AddAchievement);
        _achievements = Grid(panel, 130);
        return page;
    }

    private void AddQuickAdd(Control panel, string labelText, string[] types, Action<string, string, string, string> save)
    {
        var title = UiFactory.SectionTitle($"Add {labelText}", _theme);
        title.Location = new Point(16, 14);
        panel.Controls.Add(title);
        var type = UiFactory.Combo(types);
        type.Location = new Point(16, 56);
        panel.Controls.Add(type);
        var name = new TextBox { PlaceholderText = $"{labelText} title", Location = new Point(210, 56), Width = 260 };
        panel.Controls.Add(name);
        var description = new TextBox { PlaceholderText = "Description", Location = new Point(486, 56), Width = 280 };
        panel.Controls.Add(description);
        var file = new TextBox { PlaceholderText = "Certificate/file path", Location = new Point(782, 56), Width = 220 };
        panel.Controls.Add(file);
        var browse = Ui.Button("Browse", _theme.SurfaceAlt, _theme.Text);
        browse.Location = new Point(1010, 52);
        browse.Width = 90;
        browse.Click += (_, _) => { using var d = new OpenFileDialog(); if (d.ShowDialog() == DialogResult.OK) file.Text = d.FileName; };
        panel.Controls.Add(browse);
        var add = UiFactory.PrimaryButton("Add", _theme);
        add.Location = new Point(16, 90);
        add.Width = 100;
        add.Click += (_, _) => save(type.Text, name.Text, description.Text, file.Text);
        panel.Controls.Add(add);
    }

    private DataGridView Grid(Control panel, int y)
    {
        var grid = new DataGridView { Location = new Point(16, y), Size = new Size(1080, 470), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        UiFactory.StyleGrid(grid, _theme);
        panel.Controls.Add(grid);
        return grid;
    }

    private void AddProfileSection(Control parent, string text, ref int y)
    {
        var label = Ui.Label(text, Ui.HeadingFont, _theme.Text);
        label.Location = new Point(18, y);
        parent.Controls.Add(label);
        y += 34;
    }

    private void AddProfileField(Control parent, string key, string labelText, ref int y, int height = 28)
    {
        var label = UiFactory.SmallLabel(labelText, _theme);
        label.Location = new Point(18, y);
        parent.Controls.Add(label);

        var box = new TextBox
        {
            Location = new Point(18, y + 22),
            Width = 350,
            Height = height,
            Multiline = height > 32
        };
        UiFactory.StyleEditableTextBox(box);
        box.TextChanged += (_, _) => RefreshProfilePreview();
        _fields[key] = box;
        parent.Controls.Add(box);
        y += height + 44;
    }

    private void LoadData()
    {
        var id = StudentSessionManager.StudentId;
        var p = _database.GetStudentProfile(id);
        var a = _database.GetAcademicDetails(id);
        var g = _database.GetGuardianDetails(id);
        Set("FullName", p.FullName); Set("RegisterNumber", p.RegisterNumber); Set("DateOfBirth", p.DateOfBirth);
        Set("Gender", p.Gender); Set("Email", p.Email); Set("MobileNumber", p.MobileNumber); Set("Address", p.Address); Set("ProfilePhotoPath", p.ProfilePhotoPath);
        Set("Department", a.Department); Set("Degree", a.Degree); Set("Year", a.Year.ToString()); Set("CurrentSemester", a.CurrentSemester.ToString());
        Set("Section", a.Section); Set("Batch", a.Batch); Set("RollNumber", a.RollNumber); Set("AdmissionNumber", a.AdmissionNumber);
        Set("MentorName", a.MentorName); Set("CGPA", a.CGPA.ToString("0.00")); Set("Backlogs", a.Backlogs.ToString());
        Set("FatherName", g.FatherName); Set("MotherName", g.MotherName); Set("ParentMobileNumber", g.ParentMobileNumber); Set("ParentEmail", g.ParentEmail); Set("EmergencyContact", g.EmergencyContact);
        ReloadTables();
        RefreshProfilePreview();
    }

    private void ReloadTables()
    {
        var id = StudentSessionManager.StudentId;
        _activities.DataSource = _database.GetDataTable("SELECT ActivityType, ActivityName, Description, DateFrom, DateTo, CertificatePath FROM ExtraCurricularActivities WHERE StudentId=$studentId ORDER BY ActivityId DESC;", ("$studentId", id));
        _achievements.DataSource = _database.GetDataTable("SELECT AchievementType, Title, Description, AchievementDate, Organization, FilePath FROM Achievements WHERE StudentId=$studentId ORDER BY AchievementId DESC;", ("$studentId", id));
    }

    private void Save()
    {
        try
        {
            var id = StudentSessionManager.StudentId;
            _database.SaveProfile(new StudentProfile
            {
                StudentId = id, FullName = Get("FullName"), RegisterNumber = Get("RegisterNumber"), DateOfBirth = Get("DateOfBirth"),
                Gender = Get("Gender"), Email = Get("Email"), MobileNumber = Get("MobileNumber"), Address = Get("Address"),
                ProfilePhotoPath = Get("ProfilePhotoPath")
            }, new AcademicDetails
            {
                StudentId = id, Department = Get("Department"), Degree = Get("Degree"), Year = Int("Year"),
                CurrentSemester = Int("CurrentSemester"), Section = Get("Section"), Batch = Get("Batch"), RollNumber = Get("RollNumber"),
                AdmissionNumber = Get("AdmissionNumber"), MentorName = Get("MentorName"), CGPA = Double("CGPA"), Backlogs = Int("Backlogs")
            }, new GuardianDetails
            {
                StudentId = id, FatherName = Get("FatherName"), MotherName = Get("MotherName"), ParentMobileNumber = Get("ParentMobileNumber"),
                ParentEmail = Get("ParentEmail"), EmergencyContact = Get("EmergencyContact")
            });
            LoadData();
            MessageBox.Show("Profile saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void RefreshProfilePreview()
    {
        if (_profilePreview.Parent == null) return;

        _profilePreview.SuspendLayout();
        _profilePreview.Controls.Clear();
        _profilePreview.Controls.Add(PreviewTitle("Profile Page"));
        _profilePreview.Controls.Add(PreviewHero());
        _profilePreview.Controls.Add(PreviewSection("Personal Details",
            ("Full Name", GetOrEmpty("FullName")),
            ("Register Number", GetOrEmpty("RegisterNumber")),
            ("Date Of Birth", GetOrEmpty("DateOfBirth")),
            ("Gender", GetOrEmpty("Gender")),
            ("Email", GetOrEmpty("Email")),
            ("Mobile Number", GetOrEmpty("MobileNumber")),
            ("Address", GetOrEmpty("Address")),
            ("Profile Photo Path", GetOrEmpty("ProfilePhotoPath"))));
        _profilePreview.Controls.Add(PreviewSection("Academic Details",
            ("Department", GetOrEmpty("Department")),
            ("Degree", GetOrEmpty("Degree")),
            ("Year", GetOrEmpty("Year")),
            ("Current Semester", GetOrEmpty("CurrentSemester")),
            ("Section", GetOrEmpty("Section")),
            ("Batch", GetOrEmpty("Batch")),
            ("Roll Number", GetOrEmpty("RollNumber")),
            ("Admission Number", GetOrEmpty("AdmissionNumber")),
            ("Mentor / Faculty Advisor", GetOrEmpty("MentorName")),
            ("CGPA", GetOrEmpty("CGPA")),
            ("Backlogs", GetOrEmpty("Backlogs"))));
        _profilePreview.Controls.Add(PreviewSection("Parent / Guardian Details",
            ("Father Name", GetOrEmpty("FatherName")),
            ("Mother Name", GetOrEmpty("MotherName")),
            ("Parent Mobile Number", GetOrEmpty("ParentMobileNumber")),
            ("Parent Email", GetOrEmpty("ParentEmail")),
            ("Emergency Contact", GetOrEmpty("EmergencyContact"))));
        _profilePreview.ResumeLayout();
    }

    private Control PreviewTitle(string text)
    {
        var label = UiFactory.SectionTitle(text, _theme);
        label.Margin = new Padding(0, 0, 0, 14);
        return label;
    }

    private Control PreviewHero()
    {
        var panel = new Panel
        {
            Width = 560,
            Height = 96,
            BackColor = _theme.SurfaceAlt,
            Margin = new Padding(0, 0, 0, 16)
        };

        var initials = new Label
        {
            Text = GetInitials(GetOrEmpty("FullName")),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = _theme.Primary,
            Location = new Point(16, 16),
            Size = new Size(64, 64)
        };
        panel.Controls.Add(initials);

        var name = Ui.Label(GetOrEmpty("FullName"), new Font("Segoe UI", 16, FontStyle.Bold), _theme.Text);
        name.Location = new Point(96, 18);
        name.Size = new Size(430, 28);
        panel.Controls.Add(name);

        var meta = Ui.Label($"{GetOrEmpty("RegisterNumber")}  |  Semester {GetOrEmpty("CurrentSemester")}  |  CGPA {GetOrEmpty("CGPA")}", Ui.BodyFont, _theme.MutedText);
        meta.Location = new Point(98, 52);
        meta.Size = new Size(430, 28);
        panel.Controls.Add(meta);
        return panel;
    }

    private Control PreviewSection(string title, params (string Label, string Value)[] rows)
    {
        var panel = new Panel
        {
            Width = 560,
            Height = 42 + rows.Length * 31,
            BackColor = _theme.Surface,
            Margin = new Padding(0, 0, 0, 16)
        };

        var heading = Ui.Label(title, Ui.HeadingFont, _theme.Text);
        heading.Location = new Point(0, 0);
        panel.Controls.Add(heading);

        var y = 36;
        foreach (var row in rows)
        {
            var key = Ui.Label(row.Label, Ui.SmallFont, _theme.MutedText);
            key.Location = new Point(0, y);
            key.Size = new Size(190, 22);
            panel.Controls.Add(key);

            var value = Ui.Label(string.IsNullOrWhiteSpace(row.Value) ? "-" : row.Value, Ui.BodyFont, _theme.Text);
            value.Location = new Point(205, y);
            value.Size = new Size(335, 22);
            panel.Controls.Add(value);
            y += 31;
        }

        return panel;
    }

    private void AddActivity(string type, string name, string description, string file)
    {
        try { _database.AddActivity(StudentSessionManager.StudentId, type, name, description, file); ReloadTables(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Activity", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void AddAchievement(string type, string name, string description, string file)
    {
        try { _database.AddAchievement(StudentSessionManager.StudentId, type, name, description, file); ReloadTables(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Achievement", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void BrowseInto(string field)
    {
        using var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg|All files|*.*" };
        if (dialog.ShowDialog() == DialogResult.OK) _fields[field].Text = dialog.FileName;
    }

    private string Get(string key) => _fields[key].Text.Trim();
    private string GetOrEmpty(string key) => _fields.TryGetValue(key, out var field) ? field.Text.Trim() : "";
    private void Set(string key, string value) => _fields[key].Text = value;
    private int Int(string key) => int.TryParse(Get(key), out var v) ? v : 0;
    private double Double(string key) => double.TryParse(Get(key), out var v) ? v : 0;
    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "ST";
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }
    private static string SplitName(string value) => string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
