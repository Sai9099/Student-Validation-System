using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Services;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class FacultyRegisterForm : Form
{
    private readonly ThemePalette _theme = ThemePalette.Light;
    private readonly AuthService _auth = new(new DBHelper());
    private readonly TextBox _facultyId = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _department = new();
    private readonly TextBox _designation = new();
    private readonly TextBox _email = new();
    private readonly TextBox _mobile = new();
    private readonly TextBox _password = new();
    private readonly TextBox _confirmPassword = new();
    private readonly CheckBox _showPassword = new();
    private readonly Label _message = new();

    public FacultyRegisterForm()
    {
        Text = "Faculty Register - Student Learning Evidence & Progress Validation System";
        Size = new Size(980, 680);
        MinimumSize = new Size(900, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.BodyFont;
        Build();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var card = Ui.Card(_theme, 32);
        card.Size = new Size(560, 560);
        Controls.Add(card);
        Resize += (_, _) =>
        {
            card.Left = Math.Max(24, (ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(24, (ClientSize.Height - card.Height) / 2);
        };
        card.Left = 210;
        card.Top = 50;

        var title = Ui.Label("Faculty Registration", Ui.TitleFont, _theme.Text);
        title.Location = new Point(32, 24);
        card.Controls.Add(title);

        var y = 78;
        AddField(card, "Faculty ID", _facultyId, 32, y);
        AddField(card, "Full Name", _fullName, 292, y);
        y += 76;
        AddField(card, "Department", _department, 32, y);
        AddField(card, "Designation", _designation, 292, y);
        y += 76;
        AddField(card, "Email", _email, 32, y);
        AddField(card, "Mobile Number", _mobile, 292, y);
        y += 76;
        AddField(card, "Password", _password, 32, y, true);
        AddField(card, "Confirm Password", _confirmPassword, 292, y, true);

        _showPassword.Text = "Show password";
        _showPassword.Location = new Point(32, y + 66);
        _showPassword.Width = 160;
        _showPassword.ForeColor = _theme.Text;
        _showPassword.BackColor = Color.Transparent;
        _showPassword.CheckedChanged += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_showPassword.Checked;
            _confirmPassword.UseSystemPasswordChar = !_showPassword.Checked;
        };
        card.Controls.Add(_showPassword);

        var register = UiFactory.PrimaryButton("Register", _theme);
        register.Location = new Point(32, 380);
        register.Size = new Size(230, 42);
        register.Click += (_, _) => Register();
        card.Controls.Add(register);
        AcceptButton = register;

        var back = Ui.Button("Back", _theme.SurfaceAlt, _theme.Text);
        back.Location = new Point(292, 380);
        back.Size = new Size(230, 42);
        back.Click += (_, _) => Close();
        card.Controls.Add(back);

        _message.Location = new Point(32, 444);
        _message.Size = new Size(490, 70);
        _message.ForeColor = _theme.Danger;
        _message.BackColor = Color.Transparent;
        card.Controls.Add(_message);
    }

    private void Register()
    {
        _message.Text = "";
        var result = _auth.RegisterFaculty(
            _facultyId.Text,
            _fullName.Text,
            _department.Text,
            _designation.Text,
            _email.Text,
            _mobile.Text,
            _password.Text,
            _confirmPassword.Text);

        if (!result.Success)
        {
            _message.ForeColor = _theme.Danger;
            _message.Text = result.Message;
            return;
        }

        MessageBox.Show("Registration successful", "Faculty Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Hide();
        var login = new FacultyLoginForm();
        login.FormClosed += (_, _) => Close();
        login.Show();
    }

    private void AddField(Control parent, string label, TextBox box, int x, int y, bool password = false)
    {
        AddLabel(parent, label, x, y);
        box.Location = new Point(x, y + 26);
        box.Width = 230;
        box.UseSystemPasswordChar = password;
        parent.Controls.Add(box);
    }

    private void AddLabel(Control parent, string text, int x, int y)
    {
        var label = UiFactory.SmallLabel(text, _theme);
        label.Location = new Point(x, y);
        parent.Controls.Add(label);
    }
}
