using System.Data;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Models;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyProfileControl : Control
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly StudentDatabase _db = new();
    private Panel _mainContainer = new();
    private PictureBox _profileImage = new();
    private Label _nameLabel = new();
    private Label _emailLabel = new();
    private Label _mobileLabel = new();
    private Button _editButton = new();
    private Button _uploadPhotoButton = new();
    private DataGridView _subjectsGrid = new();
    private DataGridView _sectionsGrid = new();

    public FacultyProfileControl()
    {
        Dock = DockStyle.Fill;
        BackColor = _theme.Background;
        Build();
        LoadFacultyData();
    }

    private void Build()
    {
        _mainContainer.Dock = DockStyle.Fill;
        _mainContainer.AutoScroll = true;
        _mainContainer.BackColor = _theme.Background;
        Controls.Add(_mainContainer);

        var y = 20;

        // Profile Header Card
        var profileCard = Ui.Card(_theme, 16);
        profileCard.Location = new Point(20, y);
        profileCard.Size = new Size(900, 220);
        _mainContainer.Controls.Add(profileCard);

        // Profile Image
        _profileImage.Size = new Size(160, 160);
        _profileImage.Location = new Point(20, 20);
        _profileImage.SizeMode = PictureBoxSizeMode.StretchImage;
        _profileImage.BackColor = _theme.Background;
        _profileImage.BorderStyle = BorderStyle.FixedSingle;
        profileCard.Controls.Add(_profileImage);

        // Faculty Info
        _nameLabel.Font = Ui.TitleFont;
        _nameLabel.ForeColor = _theme.Text;
        _nameLabel.AutoSize = true;
        _nameLabel.Location = new Point(200, 20);
        profileCard.Controls.Add(_nameLabel);

        var deptLabel = Ui.Label("Department:", Ui.SmallFont, _theme.MutedText);
        deptLabel.Location = new Point(200, 60);
        profileCard.Controls.Add(deptLabel);
        var dept = Ui.Label(FacultySessionManager.Department, Ui.SmallFont, _theme.Text);
        dept.Location = new Point(350, 60);
        dept.AutoSize = true;
        profileCard.Controls.Add(dept);

        _emailLabel.AutoSize = true;
        _emailLabel.Location = new Point(200, 85);
        profileCard.Controls.Add(_emailLabel);

        _mobileLabel.AutoSize = true;
        _mobileLabel.Location = new Point(200, 110);
        profileCard.Controls.Add(_mobileLabel);

        var qualLabel = Ui.Label("Qualification:", Ui.SmallFont, _theme.MutedText);
        qualLabel.Location = new Point(200, 135);
        profileCard.Controls.Add(qualLabel);
        var qual = Ui.Label("", Ui.SmallFont, _theme.Text);
        qual.Location = new Point(350, 135);
        qual.AutoSize = true;
        profileCard.Controls.Add(qual);

        var expLabel = Ui.Label("Experience:", Ui.SmallFont, _theme.MutedText);
        expLabel.Location = new Point(200, 160);
        profileCard.Controls.Add(expLabel);
        var exp = Ui.Label("", Ui.SmallFont, _theme.Text);
        exp.Location = new Point(350, 160);
        exp.AutoSize = true;
        profileCard.Controls.Add(exp);

        // Buttons
        _editButton = Ui.Button("Edit Profile", _theme.Primary, Color.White);
        _editButton.Location = new Point(700, 130);
        _editButton.Click += EditButton_Click;
        profileCard.Controls.Add(_editButton);

        _uploadPhotoButton = Ui.Button("Upload Photo", _theme.Primary, Color.White);
        _uploadPhotoButton.Location = new Point(820, 130);
        _uploadPhotoButton.Click += UploadPhotoButton_Click;
        profileCard.Controls.Add(_uploadPhotoButton);

        y = 260;

        // Subjects Handled Section
        var subjectsTitle = UiFactory.SectionTitle("Subjects Handled by Faculty", _theme);
        subjectsTitle.Location = new Point(20, y);
        _mainContainer.Controls.Add(subjectsTitle);

        y += 40;
        _subjectsGrid.Location = new Point(20, y);
        _subjectsGrid.Size = new Size(900, 150);
        _subjectsGrid.AutoGenerateColumns = true;
        UiFactory.StyleGrid(_subjectsGrid, _theme);
        _mainContainer.Controls.Add(_subjectsGrid);

        y += 170;

        // Sections Assigned Section
        var sectionsTitle = UiFactory.SectionTitle("Sections Assigned to Faculty", _theme);
        sectionsTitle.Location = new Point(20, y);
        _mainContainer.Controls.Add(sectionsTitle);

        y += 40;
        _sectionsGrid.Location = new Point(20, y);
        _sectionsGrid.Size = new Size(900, 200);
        _sectionsGrid.AutoGenerateColumns = true;
        UiFactory.StyleGrid(_sectionsGrid, _theme);
        _mainContainer.Controls.Add(_sectionsGrid);
    }

    private void LoadFacultyData()
    {
        using var conn = _db.CreateConnection();
        conn.Open();

        // Load faculty details
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT FacultyId, Name, Email, MobileNumber, ProfileImagePath, Qualification, Experience
            FROM Faculty WHERE FacultyId = $facultyId;
            """;
        cmd.Parameters.AddWithValue("$facultyId", FacultySessionManager.FacultyId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            _nameLabel.Text = reader.GetString(1);
            _emailLabel.Text = $"Email: {reader.GetString(2)}";
            _mobileLabel.Text = $"Mobile: {reader.GetString(3)}";
            var imagePath = reader.GetString(4);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                _profileImage.Image = Image.FromFile(imagePath);
            }
            else
            {
                _profileImage.BackColor = Color.LightGray;
            }

            // Update labels with qualification and experience
            if (_mainContainer.Controls.Count > 0)
            {
                var controls = _mainContainer.Controls;
                foreach (Control ctrl in controls)
                {
                    if (ctrl is Label lbl && lbl.Text.Contains("Qualification"))
                    {
                        // Find the corresponding value label
                    }
                }
            }
        }

        // Load subjects handled
        var subjectsTable = _db.GetDataTable("""
            SELECT DISTINCT
                s.SubjectId,
                s.SubjectCode,
                s.SubjectName,
                s.Credits,
                s.SubjectType
            FROM FacultySections fs
            JOIN Subjects s ON fs.SubjectId = s.SubjectId
            WHERE fs.FacultyId = $facultyId
            ORDER BY s.SubjectCode;
            """, ("$facultyId", FacultySessionManager.FacultyId));

        _subjectsGrid.DataSource = subjectsTable;
        if (_subjectsGrid.Columns.Count > 0)
        {
            _subjectsGrid.Columns[0].Visible = false;
            _subjectsGrid.Columns[1].HeaderText = "Subject Code";
            _subjectsGrid.Columns[2].HeaderText = "Subject Name";
            _subjectsGrid.Columns[3].HeaderText = "Credits";
            _subjectsGrid.Columns[4].HeaderText = "Type";
            foreach (DataGridViewColumn col in _subjectsGrid.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        // Load sections assigned
        var sectionsTable = _db.GetDataTable("""
            SELECT DISTINCT
                s.SectionId,
                s.SectionName,
                s.Department,
                s.Year,
                s.Semester,
                s.TotalStudents,
                (SELECT COUNT(*) FROM FacultySections WHERE SectionId = s.SectionId AND FacultyId = $facultyId) as SubjectCount
            FROM FacultySections fs
            JOIN Sections s ON fs.SectionId = s.SectionId
            WHERE fs.FacultyId = $facultyId
            ORDER BY s.SectionName;
            """, ("$facultyId", FacultySessionManager.FacultyId));

        _sectionsGrid.DataSource = sectionsTable;
        if (_sectionsGrid.Columns.Count > 0)
        {
            _sectionsGrid.Columns[0].Visible = false;
            _sectionsGrid.Columns[1].HeaderText = "Section Name";
            _sectionsGrid.Columns[2].HeaderText = "Department";
            _sectionsGrid.Columns[3].HeaderText = "Year";
            _sectionsGrid.Columns[4].HeaderText = "Semester";
            _sectionsGrid.Columns[5].HeaderText = "Total Students";
            _sectionsGrid.Columns[6].HeaderText = "Subjects";
            foreach (DataGridViewColumn col in _sectionsGrid.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Edit profile functionality - To be implemented", "Faculty Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UploadPhotoButton_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Select Profile Photo"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // Save image to Data folder
                var imageName = $"Faculty_{FacultySessionManager.FacultyId}_Profile.jpg";
                var imagePath = Path.Combine(StudentDatabase.DataDirectory, "ProfileImages", imageName);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

                // Resize and save image
                using var originalImage = Image.FromFile(ofd.FileName);
                using var resizedImage = new Bitmap(originalImage, new Size(400, 400));
                resizedImage.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                // Update database
                _db.Execute("""
                    UPDATE Faculty SET ProfileImagePath = $path WHERE FacultyId = $facultyId;
                    """, ("$path", imagePath), ("$facultyId", FacultySessionManager.FacultyId));

                // Update UI
                _profileImage.Image = Image.FromFile(imagePath);
                MessageBox.Show("Profile photo updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading photo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
