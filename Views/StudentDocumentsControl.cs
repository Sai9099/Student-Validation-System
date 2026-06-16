using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentDocumentsControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _type = UiFactory.Combo("Resume", "ID card copy", "Certificates", "Bonafide certificate", "Offer letters", "Internship letters");
    private readonly TextBox _name = new();
    private readonly TextBox _path = new();

    public StudentDocumentsControl(StudentDatabase database, ThemePalette theme)
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
        var title = UiFactory.SectionTitle("Documents Module", _theme);
        title.Location = new Point(16, 16);
        panel.Controls.Add(title);
        _type.Location = new Point(16, 62);
        panel.Controls.Add(_type);
        _name.PlaceholderText = "Document name";
        _name.Location = new Point(215, 62);
        _name.Width = 240;
        panel.Controls.Add(_name);
        _path.PlaceholderText = "File path";
        _path.Location = new Point(470, 62);
        _path.Width = 330;
        panel.Controls.Add(_path);
        var browse = Ui.Button("Browse", _theme.SurfaceAlt, _theme.Text);
        browse.Location = new Point(815, 58);
        browse.Width = 100;
        browse.Click += (_, _) => { using var d = new OpenFileDialog(); if (d.ShowDialog() == DialogResult.OK) _path.Text = d.FileName; };
        panel.Controls.Add(browse);
        var upload = UiFactory.PrimaryButton("Upload", _theme);
        upload.Location = new Point(930, 58);
        upload.Width = 100;
        upload.Click += (_, _) => Upload();
        panel.Controls.Add(upload);
        var preview = Ui.Button("Preview", _theme.SurfaceAlt, _theme.Text);
        preview.Location = new Point(1040, 58);
        preview.Width = 100;
        preview.Click += (_, _) => Preview();
        panel.Controls.Add(preview);
        _grid.Location = new Point(16, 115);
        _grid.Size = new Size(1100, 500);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_grid, _theme);
        panel.Controls.Add(_grid);
    }

    private void LoadData()
    {
        _grid.DataSource = _database.GetDataTable("SELECT DocumentType, DocumentName, FilePath, UploadDate FROM StudentDocuments WHERE StudentId=$studentId ORDER BY UploadDate DESC;", ("$studentId", StudentSessionManager.StudentId));
    }

    private void Upload()
    {
        try
        {
            _database.AddDocument(StudentSessionManager.StudentId, _type.Text, _name.Text, _path.Text);
            _name.Clear(); _path.Clear(); LoadData();
            MessageBox.Show("Document stored successfully.", "Documents", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Documents", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void Preview()
    {
        var path = _path.Text;
        if (string.IsNullOrWhiteSpace(path) && _grid.CurrentRow != null) path = _grid.CurrentRow.Cells["FilePath"].Value?.ToString() ?? "";
        if (!File.Exists(path)) { MessageBox.Show("File path does not exist on this computer.", "Preview"); return; }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }
}
