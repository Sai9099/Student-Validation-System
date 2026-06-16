using System.Data;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyStudentManagementControl : Control
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly StudentDatabase _db = new();
    private DataGridView _studentsGrid = new();
    private ComboBox _sectionCombo = new();
    private ComboBox _departmentCombo = new();
    private ComboBox _yearCombo = new();
    private TextBox _searchBox = new();
    private Button _updateImageButton = new();
    private Button _updateAttendanceButton = new();
    private Button _updateMarksButton = new();
    private Button _viewProfileButton = new();

    public FacultyStudentManagementControl()
    {
        Dock = DockStyle.Fill;
        BackColor = _theme.Background;
        Build();
        LoadFilters();
    }

    private void Build()
    {
        var container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _theme.Background };

        var title = Ui.Label("Student Management", Ui.TitleFont, _theme.Text);
        title.Location = new Point(22, 18);
        title.AutoSize = true;
        container.Controls.Add(title);

        // Filters Panel
        var filterPanel = new Panel { Location = new Point(20, 60), Size = new Size(container.Width - 40, 80), BackColor = _theme.Surface };
        filterPanel.BorderStyle = BorderStyle.FixedSingle;
        container.Controls.Add(filterPanel);

        var labelY = 10;
        var inputX = 120;
        var inputWidth = 140;
        var spacing = 160;

        // Department Filter
        var deptLabel = Ui.Label("Department:", Ui.SmallFont, _theme.Text);
        deptLabel.Location = new Point(10, labelY);
        deptLabel.AutoSize = true;
        filterPanel.Controls.Add(deptLabel);

        _departmentCombo.Location = new Point(inputX, labelY);
        _departmentCombo.Size = new Size(inputWidth, 24);
        _departmentCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        filterPanel.Controls.Add(_departmentCombo);

        // Year Filter
        var yearLabel = Ui.Label("Year:", Ui.SmallFont, _theme.Text);
        yearLabel.Location = new Point(inputX + spacing, labelY);
        yearLabel.AutoSize = true;
        filterPanel.Controls.Add(yearLabel);

        _yearCombo.Location = new Point(inputX + spacing + 60, labelY);
        _yearCombo.Size = new Size(inputWidth, 24);
        _yearCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        filterPanel.Controls.Add(_yearCombo);

        // Section Filter
        var sectionLabel = Ui.Label("Section:", Ui.SmallFont, _theme.Text);
        sectionLabel.Location = new Point(10, labelY + 35);
        sectionLabel.AutoSize = true;
        filterPanel.Controls.Add(sectionLabel);

        _sectionCombo.Location = new Point(inputX, labelY + 35);
        _sectionCombo.Size = new Size(inputWidth, 24);
        _sectionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sectionCombo.SelectedIndexChanged += (_, _) => LoadStudents();
        filterPanel.Controls.Add(_sectionCombo);

        // Search Box
        var searchLabel = Ui.Label("Search:", Ui.SmallFont, _theme.Text);
        searchLabel.Location = new Point(inputX + spacing, labelY + 35);
        searchLabel.AutoSize = true;
        filterPanel.Controls.Add(searchLabel);

        _searchBox.Location = new Point(inputX + spacing + 60, labelY + 35);
        _searchBox.Size = new Size(inputWidth, 24);
        _searchBox.TextChanged += (_, _) => LoadStudents();
        filterPanel.Controls.Add(_searchBox);

        // Students Grid
        _studentsGrid.Location = new Point(20, 160);
        _studentsGrid.Size = new Size(container.Width - 40, 400);
        _studentsGrid.AutoGenerateColumns = true;
        UiFactory.StyleGrid(_studentsGrid, _theme);
        _studentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _studentsGrid.RowTemplate.Height = 30;
        container.Controls.Add(_studentsGrid);

        // Action Buttons Panel
        var buttonsPanel = new Panel { Location = new Point(20, 570), Size = new Size(container.Width - 40, 50), BackColor = Color.Transparent };
        container.Controls.Add(buttonsPanel);

        _viewProfileButton = Ui.Button("View Profile", _theme.Primary, Color.White);
        _viewProfileButton.Location = new Point(0, 0);
        _viewProfileButton.Click += ViewProfileButton_Click;
        buttonsPanel.Controls.Add(_viewProfileButton);

        _updateImageButton = Ui.Button("Update Image", _theme.Primary, Color.White);
        _updateImageButton.Location = new Point(120, 0);
        _updateImageButton.Click += UpdateImageButton_Click;
        buttonsPanel.Controls.Add(_updateImageButton);

        _updateAttendanceButton = Ui.Button("Update Attendance", _theme.Primary, Color.White);
        _updateAttendanceButton.Location = new Point(240, 0);
        _updateAttendanceButton.Click += (_, _) => MessageBox.Show("Go to Attendance Management page", "Info", MessageBoxButtons.OK);
        buttonsPanel.Controls.Add(_updateAttendanceButton);

        _updateMarksButton = Ui.Button("Update Marks", _theme.Primary, Color.White);
        _updateMarksButton.Location = new Point(400, 0);
        _updateMarksButton.Click += (_, _) => MessageBox.Show("Go to Marks Management page", "Info", MessageBoxButtons.OK);
        buttonsPanel.Controls.Add(_updateMarksButton);

        Controls.Add(container);
    }

    private void LoadFilters()
    {
        // Load departments
        var depts = _db.GetDataTable("""
            SELECT DISTINCT Department FROM Sections ORDER BY Department;
            """);

        _departmentCombo.Items.Clear();
        _departmentCombo.Items.Add("All");
        foreach (DataRow row in depts.Rows)
        {
            _departmentCombo.Items.Add(row["Department"].ToString());
        }
        _departmentCombo.SelectedIndex = 0;
        _departmentCombo.SelectedIndexChanged += (_, _) => LoadFilteredSections();

        // Load years
        _yearCombo.Items.Clear();
        _yearCombo.Items.Add("All");
        for (int i = 1; i <= 4; i++)
            _yearCombo.Items.Add(i.ToString());
        _yearCombo.SelectedIndex = 0;
        _yearCombo.SelectedIndexChanged += (_, _) => LoadFilteredSections();

        LoadFilteredSections();
    }

    private void LoadFilteredSections()
    {
        _sectionCombo.Items.Clear();
        var faculty_id = FacultySessionManager.FacultyId;
        var dept = _departmentCombo.SelectedItem.ToString() ?? "";
        var year = _yearCombo.SelectedItem.ToString() ?? "";

        var query = """
            SELECT DISTINCT s.SectionId, s.SectionName
            FROM FacultySections fs
            JOIN Sections s ON fs.SectionId = s.SectionId
            WHERE fs.FacultyId = $facultyId
            """;

        var @params = new List<(string, object?)> { ("$facultyId", faculty_id) };

        if (dept != "All")
        {
            query += " AND s.Department = $dept";
            @params.Add(("$dept", dept));
        }

        if (year != "All")
        {
            query += " AND s.Year = CAST($year AS INTEGER)";
            @params.Add(("$year", int.Parse(year)));
        }

        query += " ORDER BY s.SectionName;";

        var sections = _db.GetDataTable(query, @params.ToArray());
        foreach (DataRow row in sections.Rows)
        {
            _sectionCombo.Items.Add($"{row["SectionName"]} ({row["SectionId"]})");
        }

        if (_sectionCombo.Items.Count > 0)
            _sectionCombo.SelectedIndex = 0;
    }

    private void LoadStudents()
    {
        if (_sectionCombo.SelectedIndex < 0) return;

        var selectedSection = _sectionCombo.SelectedItem.ToString() ?? "";
        var sectionIdStr = selectedSection.Split('(').Last().TrimEnd(')');

        if (!int.TryParse(sectionIdStr, out var sectionId)) return;

        var search = _searchBox.Text.Trim();

        var query = """
            SELECT
                sp.StudentId,
                sp.RegisterNumber,
                sp.FullName,
                ad.Department,
                ad.Year,
                ad.CurrentSemester,
                sp.Email,
                sp.MobileNumber,
                COALESCE((SELECT ROUND(AVG(CASE WHEN Status = 'Present' THEN 100 ELSE 0 END), 2) 
                         FROM FacultyAttendanceDaily 
                         WHERE StudentId = sp.StudentId), 0) as AttendancePercent,
                COALESCE((SELECT ROUND(AVG(MarksObtained), 2) 
                         FROM FacultyMarksRecords 
                         WHERE StudentId = sp.StudentId), 0) as AvgMarks,
                CASE WHEN sp.ProfilePhotoPath IS NOT NULL THEN 'Yes' ELSE 'No' END as HasImage
            FROM StudentSectionMapping ssm
            JOIN StudentProfile sp ON ssm.StudentId = sp.StudentId
            JOIN AcademicDetails ad ON sp.StudentId = ad.StudentId
            WHERE ssm.SectionId = $sectionId
            """;

        var @params = new List<(string, object?)> { ("$sectionId", sectionId) };

        if (!string.IsNullOrEmpty(search))
        {
            query += " AND (sp.RegisterNumber LIKE $search OR sp.FullName LIKE $search)";
            @params.Add(("$search", $"%{search}%"));
        }

        query += " ORDER BY sp.RegisterNumber;";

        var students = _db.GetDataTable(query, @params.ToArray());

        _studentsGrid.DataSource = students;
        if (_studentsGrid.Columns.Count > 0)
        {
            _studentsGrid.Columns[0].Visible = false; // Hide StudentId
            _studentsGrid.Columns[1].HeaderText = "Register Number";
            _studentsGrid.Columns[2].HeaderText = "Student Name";
            _studentsGrid.Columns[3].HeaderText = "Department";
            _studentsGrid.Columns[4].HeaderText = "Year";
            _studentsGrid.Columns[5].HeaderText = "Semester";
            _studentsGrid.Columns[6].HeaderText = "Email";
            _studentsGrid.Columns[7].HeaderText = "Mobile";
            _studentsGrid.Columns[8].HeaderText = "Attendance %";
            _studentsGrid.Columns[9].HeaderText = "Avg Marks";
            _studentsGrid.Columns[10].HeaderText = "Profile Image";

            foreach (DataGridViewColumn col in _studentsGrid.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }
    }

    private void ViewProfileButton_Click(object? sender, EventArgs e)
    {
        if (_studentsGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a student first", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var studentId = (int)_studentsGrid.SelectedRows[0].Cells[0].Value;
        var studentName = _studentsGrid.SelectedRows[0].Cells[2].Value.ToString();
        MessageBox.Show($"View full profile for: {studentName}", "Student Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateImageButton_Click(object? sender, EventArgs e)
    {
        if (_studentsGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a student first", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var studentId = (int)_studentsGrid.SelectedRows[0].Cells[0].Value;
        var studentName = _studentsGrid.SelectedRows[0].Cells[2].Value.ToString();

        using var ofd = new OpenFileDialog
        {
            Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Title = $"Upload profile image for {studentName}"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var imageName = $"Student_{studentId}_Profile.jpg";
                var imagePath = Path.Combine(StudentDatabase.DataDirectory, "StudentProfileImages", imageName);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

                using var originalImage = Image.FromFile(ofd.FileName);
                using var resizedImage = new Bitmap(originalImage, new Size(300, 300));
                resizedImage.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                _db.Execute("""
                    UPDATE StudentProfile SET ProfilePhotoPath = $path WHERE StudentId = $studentId;
                    """, ("$path", imagePath), ("$studentId", studentId));

                MessageBox.Show("Student profile image updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
