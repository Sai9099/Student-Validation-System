using System.Data;
using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentNotesControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;
    private readonly ComboBox _uploadSemester = UiFactory.Combo("1", "2", "3", "4", "5", "6", "7", "8");
    private readonly ComboBox _uploadSubject = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly ComboBox _unit = UiFactory.Combo("1", "2", "3", "4", "5", "6");
    private readonly TextBox _title = new() { PlaceholderText = "Note title" };
    private readonly TextBox _description = new() { PlaceholderText = "Description", Multiline = true };
    private readonly TextBox _filePath = new() { PlaceholderText = "Scanned note / PDF file path" };
    private readonly Label _editState = new();
    private int? _editingNoteId;

    private readonly DataGridView _myNotes = new();
    private readonly DataGridView _studentNotes = new();
    private readonly DataGridView _facultyNotes = new();
    private readonly DataGridView _searchResults = new();
    private readonly DataGridView _bookmarks = new();
    private readonly TextBox _search = new() { PlaceholderText = "Search subject, title, unit, uploader" };
    private readonly ComboBox _searchSemester = UiFactory.Combo("All", "1", "2", "3", "4", "5", "6", "7", "8");
    private readonly ComboBox _searchRole = UiFactory.Combo("All", "My notes", "Student uploaded notes", "Faculty uploaded notes");
    private readonly ComboBox _searchSubject = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };

    public StudentNotesControl(StudentDatabase database, ThemePalette theme)
    {
        _database = database;
        _theme = theme;
        Build();
        LoadSubjectsForUpload();
        LoadSubjectsForSearch();
        LoadData();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);
        tabs.TabPages.Add(BuildUploadTab());
        tabs.TabPages.Add(BuildGridTab("My Notes Page", _myNotes, includeOwnerActions: true));
        tabs.TabPages.Add(BuildGridTab("Student Shared Notes Page", _studentNotes));
        tabs.TabPages.Add(BuildGridTab("Faculty Notes Page", _facultyNotes));
        tabs.TabPages.Add(BuildSearchTab());
        tabs.TabPages.Add(BuildGridTab("Bookmarked Notes Page", _bookmarks));
    }

    private TabPage BuildUploadTab()
    {
        var page = new TabPage("Upload Notes Page") { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);

        var heading = UiFactory.SectionTitle("Upload Handwritten / Scanned / PDF Notes", _theme);
        heading.Location = new Point(16, 16);
        panel.Controls.Add(heading);

        var y = 64;
        AddLabel(panel, "Note Title", 16, y);
        _title.Location = new Point(16, y + 22);
        _title.Width = 360;
        panel.Controls.Add(_title);

        AddLabel(panel, "Semester", 400, y);
        _uploadSemester.Location = new Point(400, y + 22);
        panel.Controls.Add(_uploadSemester);

        AddLabel(panel, "Subject", 610, y);
        _uploadSubject.Location = new Point(610, y + 22);
        panel.Controls.Add(_uploadSubject);

        y += 72;
        AddLabel(panel, "Unit / Module Number", 16, y);
        _unit.Location = new Point(16, y + 22);
        panel.Controls.Add(_unit);

        AddLabel(panel, "Upload File Path", 230, y);
        _filePath.Location = new Point(230, y + 22);
        _filePath.Width = 500;
        panel.Controls.Add(_filePath);

        var browse = Ui.Button("Browse", _theme.SurfaceAlt, _theme.Text);
        browse.Location = new Point(750, y + 18);
        browse.Width = 100;
        browse.Click += (_, _) => BrowseFile();
        panel.Controls.Add(browse);

        y += 72;
        AddLabel(panel, "Description", 16, y);
        _description.Location = new Point(16, y + 22);
        _description.Size = new Size(835, 96);
        panel.Controls.Add(_description);

        y += 136;
        var upload = UiFactory.PrimaryButton("Upload Note", _theme);
        upload.Location = new Point(16, y);
        upload.Width = 135;
        upload.Click += (_, _) => UploadNote();
        panel.Controls.Add(upload);

        var update = Ui.Button("Update Selected", _theme.SurfaceAlt, _theme.Text);
        update.Location = new Point(165, y);
        update.Width = 145;
        update.Click += (_, _) => UpdateSelectedNote();
        panel.Controls.Add(update);

        var clear = Ui.Button("Clear", _theme.SurfaceAlt, _theme.Text);
        clear.Location = new Point(324, y);
        clear.Width = 95;
        clear.Click += (_, _) => ClearForm();
        panel.Controls.Add(clear);

        _editState.Location = new Point(440, y + 8);
        _editState.Size = new Size(420, 28);
        _editState.ForeColor = _theme.MutedText;
        _editState.BackColor = Color.Transparent;
        _editState.Text = "New uploads are saved as Pending until faculty/admin approval.";
        panel.Controls.Add(_editState);

        return page;
    }

    private TabPage BuildGridTab(string title, DataGridView grid, bool includeOwnerActions = false)
    {
        var page = new TabPage(title) { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);

        var heading = UiFactory.SectionTitle(title, _theme);
        heading.Location = new Point(16, 16);
        panel.Controls.Add(heading);

        var x = 16;
        if (includeOwnerActions)
        {
            var load = UiFactory.PrimaryButton("Load Selected For Edit", _theme);
            load.Location = new Point(x, 58);
            load.Width = 175;
            load.Click += (_, _) => LoadSelectedMyNote();
            panel.Controls.Add(load);
            x += 188;

            var delete = Ui.Button("Delete Selected", _theme.SurfaceAlt, _theme.Text);
            delete.Location = new Point(x, 58);
            delete.Width = 140;
            delete.Click += (_, _) => DeleteSelectedMyNote();
            panel.Controls.Add(delete);
            x += 154;
        }

        AddNoteActions(panel, grid, x, 58);

        grid.Location = new Point(16, 112);
        grid.Size = new Size(1100, 500);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(grid, _theme);
        panel.Controls.Add(grid);
        return page;
    }

    private TabPage BuildSearchTab()
    {
        var page = new TabPage("Notes Search and Filter Page") { BackColor = _theme.Background };
        var panel = Ui.Card(_theme, 18);
        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);

        var heading = UiFactory.SectionTitle("Notes Search and Filter Page", _theme);
        heading.Location = new Point(16, 16);
        panel.Controls.Add(heading);

        _search.Location = new Point(16, 62);
        _search.Width = 260;
        _search.TextChanged += (_, _) => LoadSearchResults();
        panel.Controls.Add(_search);

        _searchRole.Location = new Point(292, 62);
        _searchRole.Width = 180;
        _searchRole.SelectedIndexChanged += (_, _) => LoadSearchResults();
        panel.Controls.Add(_searchRole);

        _searchSemester.Location = new Point(488, 62);
        _searchSemester.Width = 100;
        _searchSemester.SelectedIndexChanged += (_, _) => LoadSearchResults();
        panel.Controls.Add(_searchSemester);

        _searchSubject.Location = new Point(604, 62);
        _searchSubject.SelectedIndexChanged += (_, _) => LoadSearchResults();
        panel.Controls.Add(_searchSubject);

        AddNoteActions(panel, _searchResults, 852, 58);

        _searchResults.Location = new Point(16, 112);
        _searchResults.Size = new Size(1100, 500);
        _searchResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        UiFactory.StyleGrid(_searchResults, _theme);
        panel.Controls.Add(_searchResults);
        return page;
    }

    private void AddNoteActions(Control panel, DataGridView grid, int x, int y)
    {
        var open = Ui.Button("Open", _theme.SurfaceAlt, _theme.Text);
        open.Location = new Point(x, y);
        open.Width = 80;
        open.Click += (_, _) => OpenSelected(grid);
        panel.Controls.Add(open);

        var like = Ui.Button("Like", _theme.SurfaceAlt, _theme.Text);
        like.Location = new Point(x + 90, y);
        like.Width = 80;
        like.Click += (_, _) => NoteAction(grid, _database.LikeNote);
        panel.Controls.Add(like);

        var bookmark = Ui.Button("Bookmark", _theme.SurfaceAlt, _theme.Text);
        bookmark.Location = new Point(x + 180, y);
        bookmark.Width = 110;
        bookmark.Click += (_, _) => NoteAction(grid, _database.ToggleBookmarkNote);
        panel.Controls.Add(bookmark);

        var report = Ui.Button("Report", _theme.SurfaceAlt, _theme.Text);
        report.Location = new Point(x + 300, y);
        report.Width = 90;
        report.Click += (_, _) => NoteAction(grid, _database.ReportNote);
        panel.Controls.Add(report);
    }

    private void LoadData()
    {
        var studentId = StudentSessionManager.StudentId;
        _myNotes.DataSource = NotesTable("n.StudentId = $studentId", ("$studentId", studentId));
        _studentNotes.DataSource = NotesTable("n.UploadedByRole = 'Student' AND n.StudentId <> $studentId AND n.ApprovalStatus = 'Approved'", ("$studentId", studentId));
        _facultyNotes.DataSource = NotesTable("n.UploadedByRole = 'Faculty' AND n.ApprovalStatus = 'Approved'");
        _bookmarks.DataSource = NotesTable("n.IsBookmarked = 1 AND (n.StudentId = $studentId OR n.ApprovalStatus = 'Approved')", ("$studentId", studentId));
        LoadSearchResults();
        HideNoteIds(_myNotes, _studentNotes, _facultyNotes, _searchResults, _bookmarks);
    }

    private void LoadSearchResults()
    {
        if (_searchSubject.Items.Count == 0) return;

        var studentId = StudentSessionManager.StudentId;
        var subject = _searchSubject.SelectedItem as SubjectItem;
        var roleFilter = _searchRole.Text switch
        {
            "My notes" => "AND n.StudentId = $studentId",
            "Student uploaded notes" => "AND n.UploadedByRole = 'Student' AND n.ApprovalStatus = 'Approved'",
            "Faculty uploaded notes" => "AND n.UploadedByRole = 'Faculty' AND n.ApprovalStatus = 'Approved'",
            _ => "AND (n.StudentId = $studentId OR n.ApprovalStatus = 'Approved')"
        };
        var semesterFilter = _searchSemester.Text == "All" ? "" : "AND n.Semester = $semester";
        var subjectFilter = subject is { SubjectId: > 0 } ? "AND n.SubjectId = $subjectId" : "";

        _searchResults.DataSource = NotesTable($"""
            (n.StudentId = $studentId OR n.ApprovalStatus = 'Approved')
            {roleFilter}
            {semesterFilter}
            {subjectFilter}
            AND ($search = ''
                 OR lower(n.Title || s.SubjectName || n.Description || n.UnitNumber || COALESCE(sp.FullName, 'Faculty ' || n.FacultyId)) LIKE '%' || lower($search) || '%')
            """,
            ("$studentId", studentId),
            ("$semester", _searchSemester.Text),
            ("$subjectId", subject?.SubjectId ?? 0),
            ("$search", _search.Text.Trim()));
        HideNoteIds(_searchResults);
    }

    private DataTable NotesTable(string where, params (string Name, object? Value)[] parameters)
    {
        return _database.GetDataTable($"""
            SELECT n.NoteId,
                   n.UploadedByRole AS Role,
                   COALESCE(sp.FullName, 'Faculty ' || n.FacultyId) AS Uploader,
                   n.Title,
                   n.Semester,
                   s.SubjectCode,
                   s.SubjectName,
                   n.UnitNumber AS Unit,
                   n.Description,
                   n.FilePath,
                   n.UploadDate,
                   n.ApprovalStatus,
                   n.ApprovedBy,
                   n.FacultyComments,
                   n.LikesCount,
                   CASE WHEN n.IsBookmarked = 1 THEN 'Yes' ELSE 'No' END AS Bookmarked,
                   n.ReportStatus
            FROM Notes n
            JOIN Subjects s ON s.SubjectId = n.SubjectId
            LEFT JOIN StudentProfile sp ON sp.StudentId = n.StudentId
            WHERE {where}
            ORDER BY n.UploadDate DESC, n.NoteId DESC;
            """, parameters);
    }

    private void UploadNote()
    {
        try
        {
            var subject = SelectedUploadSubject();
            _database.AddNote(StudentSessionManager.StudentId, subject.SubjectId, int.Parse(_uploadSemester.Text),
                int.Parse(_unit.Text), _title.Text, _description.Text, _filePath.Text);
            ClearForm();
            LoadData();
            MessageBox.Show("Note uploaded with Pending approval status.", "Notes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Notes", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void UpdateSelectedNote()
    {
        if (_editingNoteId == null)
        {
            MessageBox.Show("Load one of your notes for editing first.", "Notes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var subject = SelectedUploadSubject();
            _database.UpdateOwnNote(_editingNoteId.Value, StudentSessionManager.StudentId, subject.SubjectId,
                int.Parse(_uploadSemester.Text), int.Parse(_unit.Text), _title.Text, _description.Text, _filePath.Text);
            ClearForm();
            LoadData();
            MessageBox.Show("Note updated and moved back to Pending approval.", "Notes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Notes", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void LoadSelectedMyNote()
    {
        if (_myNotes.CurrentRow == null) return;
        _editingNoteId = Convert.ToInt32(_myNotes.CurrentRow.Cells["NoteId"].Value);
        _title.Text = _myNotes.CurrentRow.Cells["Title"].Value?.ToString() ?? "";
        _uploadSemester.Text = _myNotes.CurrentRow.Cells["Semester"].Value?.ToString() ?? "1";
        LoadSubjectsForUpload();
        SelectSubjectByCode(_myNotes.CurrentRow.Cells["SubjectCode"].Value?.ToString() ?? "");
        _unit.Text = _myNotes.CurrentRow.Cells["Unit"].Value?.ToString() ?? "1";
        _description.Text = _myNotes.CurrentRow.Cells["Description"].Value?.ToString() ?? "";
        _filePath.Text = _myNotes.CurrentRow.Cells["FilePath"].Value?.ToString() ?? "";
        _editState.Text = $"Editing Note #{_editingNoteId}. Save with Update Selected.";
    }

    private void DeleteSelectedMyNote()
    {
        if (_myNotes.CurrentRow == null) return;
        var noteId = Convert.ToInt32(_myNotes.CurrentRow.Cells["NoteId"].Value);
        if (MessageBox.Show("Delete this note?", "Notes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _database.DeleteOwnNote(noteId, StudentSessionManager.StudentId);
        ClearForm();
        LoadData();
    }

    private void NoteAction(DataGridView grid, Action<int> action)
    {
        var noteId = SelectedNoteId(grid);
        if (noteId == null) return;
        action(noteId.Value);
        LoadData();
    }

    private void OpenSelected(DataGridView grid)
    {
        if (grid.CurrentRow == null) return;
        var path = grid.CurrentRow.Cells["FilePath"].Value?.ToString() ?? "";
        if (!File.Exists(path))
        {
            MessageBox.Show($"File path stored in database:\n{path}\n\nThe file does not exist on this computer.", "Open Notes");
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void LoadSubjectsForUpload()
    {
        _uploadSubject.Items.Clear();
        var rows = _database.GetDataTable("""
            SELECT s.SubjectId, s.SubjectCode, s.SubjectName
            FROM Subjects s
            ORDER BY s.SubjectName;
            """);

        foreach (DataRow row in rows.Rows)
        {
            _uploadSubject.Items.Add(new SubjectItem(Convert.ToInt32(row["SubjectId"]), row["SubjectCode"].ToString() ?? "", row["SubjectName"].ToString() ?? ""));
        }
        if (_uploadSubject.Items.Count > 0) _uploadSubject.SelectedIndex = 0;
    }

    private void LoadSubjectsForSearch()
    {
        _searchSubject.Items.Clear();
        _searchSubject.Items.Add(new SubjectItem(0, "All", "All subjects"));
        var rows = _database.GetDataTable("SELECT SubjectId, SubjectCode, SubjectName FROM Subjects ORDER BY SubjectName;");
        foreach (DataRow row in rows.Rows)
        {
            _searchSubject.Items.Add(new SubjectItem(Convert.ToInt32(row["SubjectId"]), row["SubjectCode"].ToString() ?? "", row["SubjectName"].ToString() ?? ""));
        }
        _searchSubject.SelectedIndex = 0;
    }

    private void BrowseFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Notes|*.pdf;*.jpg;*.jpeg;*.png|All files|*.*" };
        if (dialog.ShowDialog() == DialogResult.OK) _filePath.Text = dialog.FileName;
    }

    private SubjectItem SelectedUploadSubject()
    {
        if (_uploadSubject.SelectedItem is SubjectItem subject) return subject;
        throw new InvalidOperationException("Select a subject.");
    }

    private int? SelectedNoteId(DataGridView grid)
    {
        return grid.CurrentRow == null ? null : Convert.ToInt32(grid.CurrentRow.Cells["NoteId"].Value);
    }

    private void SelectSubjectByCode(string code)
    {
        for (var i = 0; i < _uploadSubject.Items.Count; i++)
        {
            if (_uploadSubject.Items[i] is SubjectItem item && item.Code == code)
            {
                _uploadSubject.SelectedIndex = i;
                return;
            }
        }
    }

    private void ClearForm()
    {
        _editingNoteId = null;
        _title.Clear();
        _description.Clear();
        _filePath.Clear();
        _unit.SelectedIndex = 0;
        _editState.Text = "New uploads are saved as Pending until faculty/admin approval.";
    }

    private void AddLabel(Control parent, string text, int x, int y)
    {
        var label = UiFactory.SmallLabel(text, _theme);
        label.Location = new Point(x, y);
        parent.Controls.Add(label);
    }

    private static void HideNoteIds(params DataGridView[] grids)
    {
        foreach (var grid in grids)
        {
            if (grid.Columns["NoteId"] is DataGridViewColumn noteId) noteId.Visible = false;
        }
    }

    private sealed record SubjectItem(int SubjectId, string Code, string Name)
    {
        public override string ToString() => SubjectId == 0 ? Name : $"{Code} - {Name}";
    }
}
