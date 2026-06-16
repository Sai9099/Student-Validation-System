using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Models;
using StudentValidationSystem.Services;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyLoginForm : Form
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly AuthService _auth = new(new DBHelper());
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly CheckBox _showPassword = new();
    private readonly Label _message = new();

    public FacultyLoginForm()
    {
        Text = "Faculty Login - Student Learning Evidence & Progress Validation System";
        Size = new Size(980, 620);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.BodyFont;
        Build();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var card = Ui.Card(_theme, 32);
        card.Size = new Size(500, 430);
        Controls.Add(card);
        Resize += (_, _) =>
        {
            card.Left = Math.Max(24, (ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(24, (ClientSize.Height - card.Height) / 2);
        };
        card.Left = 240;
        card.Top = 90;

        var title = Ui.Label("Faculty Login", Ui.TitleFont, _theme.Text);
        title.Location = new Point(32, 26);
        card.Controls.Add(title);

        var note = Ui.Label("Use your registered faculty email address.", Ui.BodyFont, _theme.MutedText);
        note.Location = new Point(34, 66);
        card.Controls.Add(note);

        AddLabel(card, "Email", 118);
        _username.Location = new Point(34, 144);
        _username.Width = 420;
        _username.Text = "faculty1@college.edu";
        card.Controls.Add(_username);

        AddLabel(card, "Password", 198);
        _password.Location = new Point(34, 224);
        _password.Width = 420;
        _password.UseSystemPasswordChar = true;
        _password.Text = "1234";
        card.Controls.Add(_password);

        _showPassword.Text = "Show password";
        _showPassword.Location = new Point(34, 258);
        _showPassword.Width = 150;
        _showPassword.ForeColor = _theme.Text;
        _showPassword.BackColor = Color.Transparent;
        _showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_showPassword.Checked;
        card.Controls.Add(_showPassword);

        var forgot = new LinkLabel
        {
            Text = "Forgot Password?",
            Location = new Point(320, 258),
            Size = new Size(140, 26),
            LinkColor = _theme.Primary,
            BackColor = Color.Transparent
        };
        forgot.Click += (_, _) => MessageBox.Show("Forgot password placeholder. Contact admin to reset your password.", "Forgot Password");
        card.Controls.Add(forgot);

        var login = UiFactory.PrimaryButton("Login", _theme);
        login.Location = new Point(34, 306);
        login.Size = new Size(200, 42);
        login.MouseEnter += (_, _) => login.BackColor = Color.FromArgb(42, 120, 176);
        login.MouseLeave += (_, _) => login.BackColor = _theme.Primary;
        login.Click += (_, _) => Login();
        card.Controls.Add(login);
        AcceptButton = login;

        var back = Ui.Button("Back", _theme.SurfaceAlt, _theme.Text);
        back.Location = new Point(252, 306);
        back.Size = new Size(200, 42);
        back.Click += (_, _) => Close();
        card.Controls.Add(back);

        _message.Location = new Point(34, 366);
        _message.Size = new Size(420, 42);
        _message.ForeColor = _theme.Danger;
        _message.BackColor = Color.Transparent;
        card.Controls.Add(_message);
    }

    private void Login()
    {
        _message.Text = "";
        try
        {
            var result = _auth.LoginFaculty(_username.Text, _password.Text);
            if (!result.Success || result.User == null)
            {
                _message.Text = result.Message;
                return;
            }

            FacultySessionManager.Current = new FacultySession
            {
                FacultyId = result.FacultyId,
                Username = result.User.Username,
                Name = result.FacultyName,
                Department = result.Department,
                Email = result.Email
            };
            SessionManager.Set(result.User);

            Hide();
            var dashboard = new FacultyDashboardForm();
            dashboard.FormClosed += (_, _) => Close();
            dashboard.Show();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }

    private void AddLabel(Control parent, string text, int y)
    {
        var label = UiFactory.SmallLabel(text, _theme);
        label.Location = new Point(34, y);
        parent.Controls.Add(label);
    }
}
