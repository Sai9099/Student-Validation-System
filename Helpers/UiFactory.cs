using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Helpers;

public static class UiFactory
{
    public static Label SectionTitle(string text, ThemePalette theme) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 15, FontStyle.Bold),
        ForeColor = theme.Text,
        BackColor = Color.Transparent
    };

    public static Label SmallLabel(string text, ThemePalette theme) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = theme.MutedText,
        BackColor = Color.Transparent
    };

    public static Button PrimaryButton(string text, ThemePalette theme) => Ui.Button(text, theme.Primary, Color.White);

    public static ComboBox Combo(params string[] items)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        combo.Items.AddRange(items);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    public static void StyleEditableTextBox(TextBox box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = Ui.BodyFont;
    }

    public static void StyleGrid(DataGridView grid, ThemePalette theme)
    {
        Ui.StyleGrid(grid, theme);
        grid.AutoGenerateColumns = true;
        grid.RowTemplate.Height = 28;
        grid.AllowUserToResizeColumns = true;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        grid.ScrollBars = ScrollBars.Both;
    }
}
