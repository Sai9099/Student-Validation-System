using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Models;
using StudentValidationSystem.Services;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class LoginForm : Form
{
    private readonly StudentDatabase _database = new();
    private readonly AuthService _auth = new(new DBHelper());
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly TextBox _login = new();
    private readonly TextBox _password = new();
    private readonly CheckBox _showPassword = new();
    private readonly Label _message = new();

    public LoginForm()
    {
        Text = "Student Learning Evidence & Progress Validation System";
        Size = new Size(1120, 700);
        MinimumSize = new Size(1040, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.BodyFont;
        Build();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 470));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 58, 83), Padding = new Padding(44) };
        root.Controls.Add(hero, 0, 0);

        var title = new Label
        {
            Text = "Student Learning\nEvidence & Progress\nValidation System",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(44, 88),
            Size = new Size(380, 150)
        };
        hero.Controls.Add(title);

        var desc = new Label
        {
            Text = "Student-only workspace for profile, attendance, subjects, marks, evidence, events, documents, notifications, and progress review.",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(219, 234, 245),
            BackColor = Color.Transparent,
            Location = new Point(46, 260),
            Size = new Size(355, 110)
        };
        hero.Controls.Add(desc);

        var sample = new Label
        {
            Text = "Sample student login\nRegister: RA2311033010096\nEmail   : sai.kumar@student.edu\nPassword: student123",
            Font = new Font("Consolas", 10),
            ForeColor = Color.FromArgb(220, 236, 247),
            BackColor = Color.Transparent,
            Location = new Point(46, 470),
            Size = new Size(360, 110)
        };
        hero.Controls.Add(sample);

        var right = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Background };
        root.Controls.Add(right, 1, 0);

        var card = Ui.Card(_theme, 30);
        card.Size = new Size(470, 430);
        right.Controls.Add(card);
        right.Resize += (_, _) =>
        {
            card.Left = Math.Max(32, (right.ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(32, (right.ClientSize.Height - card.Height) / 2);
        };
        card.Left = 80;
        card.Top = 110;

        var welcome = Ui.Label("Student Login", Ui.TitleFont, _theme.Text);
        welcome.Location = new Point(30, 28);
        card.Controls.Add(welcome);

        var note = Ui.Label("Use your register number and password.", Ui.BodyFont, _theme.MutedText);
        note.Location = new Point(32, 66);
        card.Controls.Add(note);

        AddLabel(card, "Register Number", 122);
        _login.Location = new Point(32, 148);
        _login.Width = 400;
        _login.Text = "RA2311033010096";
        card.Controls.Add(_login);

        AddLabel(card, "Password", 202);
        _password.Location = new Point(32, 228);
        _password.Width = 400;
        _password.UseSystemPasswordChar = true;
        _password.Text = "student123";
        card.Controls.Add(_password);

        _showPassword.Text = "Show password";
        _showPassword.Location = new Point(32, 258);
        _showPassword.Width = 150;
        _showPassword.ForeColor = _theme.Text;
        _showPassword.BackColor = Color.Transparent;
        _showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_showPassword.Checked;
        card.Controls.Add(_showPassword);

        var button = UiFactory.PrimaryButton("Login to Student Portal", _theme);
        button.Location = new Point(32, 292);
        button.Size = new Size(400, 42);
        button.Click += (_, _) => Login();
        card.Controls.Add(button);
        AcceptButton = button;

        _message.Location = new Point(32, 354);
        _message.Size = new Size(400, 44);
        _message.ForeColor = _theme.Danger;
        _message.BackColor = Color.Transparent;
        card.Controls.Add(_message);
    }

    private void AddLabel(Control parent, string text, int y)
    {
        var label = UiFactory.SmallLabel(text, _theme);
        label.Location = new Point(32, y);
        parent.Controls.Add(label);
    }

    private void Login()
    {
        try
        {
            var result = _auth.LoginStudent(_login.Text, _password.Text);
            if (!result.Success || result.User == null)
            {
                _message.Text = result.Message;
                return;
            }

            var student = _database.Authenticate(_login.Text, _password.Text);
            if (student == null)
            {
                _message.Text = "Invalid register number or password.";
                return;
            }

            SessionManager.Set(result.User);
            StudentSessionManager.Current = new StudentSession { User = student };
            Hide();
            var main = new MainForm(_database);
            main.FormClosed += (_, _) => Close();
            main.Show();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }
}
