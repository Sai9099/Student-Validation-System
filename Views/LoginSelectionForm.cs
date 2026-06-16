using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class LoginSelectionForm : Form
{
    private readonly ThemePalette _theme = ThemePalette.Light;

    public LoginSelectionForm()
    {
        Text = "Student Learning Evidence & Progress Validation System";
        Size = new Size(980, 620);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Ui.BodyFont;
        Build();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 58, 83), Padding = new Padding(42) };
        root.Controls.Add(hero, 0, 0);
        hero.Controls.Add(new Label
        {
            Text = "Student Learning\nEvidence & Progress\nValidation System",
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(42, 100),
            Size = new Size(340, 150)
        });
        hero.Controls.Add(new Label
        {
            Text = "Choose a login role to continue.",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.FromArgb(219, 234, 245),
            BackColor = Color.Transparent,
            Location = new Point(44, 280),
            Size = new Size(320, 80)
        });

        var content = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Background };
        root.Controls.Add(content, 1, 0);

        var card = Ui.Card(_theme, 32);
        card.Size = new Size(430, 460);
        content.Controls.Add(card);
        content.Resize += (_, _) =>
        {
            card.Left = Math.Max(24, (content.ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(24, (content.ClientSize.Height - card.Height) / 2);
        };
        card.Left = 70;
        card.Top = 110;

        var title = Ui.Label("Login Selection", Ui.TitleFont, _theme.Text);
        title.Location = new Point(32, 28);
        card.Controls.Add(title);

        var subtitle = Ui.Label("Select login or register for your role.", Ui.BodyFont, _theme.MutedText);
        subtitle.Location = new Point(34, 68);
        subtitle.Size = new Size(350, 32);
        card.Controls.Add(subtitle);

        AddRoleButton(card, "Student Login", 120, OpenStudentLogin);
        AddRoleButton(card, "Student Register", 178, OpenStudentRegister);
        AddRoleButton(card, "Faculty Login", 236, OpenFacultyLogin);
        AddRoleButton(card, "Faculty Register", 294, OpenFacultyRegister);
        AddRoleButton(card, "Admin Login", 352, OpenAdminLogin);
    }

    private void AddRoleButton(Control parent, string text, int y, Action click)
    {
        var button = UiFactory.PrimaryButton(text, _theme);
        button.Location = new Point(34, y);
        button.Size = new Size(350, 42);
        button.MouseEnter += (_, _) => button.BackColor = Color.FromArgb(42, 120, 176);
        button.MouseLeave += (_, _) => button.BackColor = _theme.Primary;
        button.Click += (_, _) => click();
        parent.Controls.Add(button);
    }

    private void OpenStudentLogin()
    {
        Hide();
        var student = new StudentLoginForm();
        student.FormClosed += (_, _) => Show();
        student.Show();
    }

    private void OpenStudentRegister()
    {
        Hide();
        var student = new StudentRegisterForm();
        student.FormClosed += (_, _) => Show();
        student.Show();
    }

    private void OpenFacultyLogin()
    {
        Hide();
        var faculty = new FacultyLoginForm();
        faculty.FormClosed += (_, _) => Show();
        faculty.Show();
    }

    private void OpenFacultyRegister()
    {
        Hide();
        var faculty = new FacultyRegisterForm();
        faculty.FormClosed += (_, _) => Show();
        faculty.Show();
    }

    private void OpenAdminLogin()
    {
        Hide();
        var admin = new AdminLoginForm();
        admin.FormClosed += (_, _) => Show();
        admin.Show();
    }
}
