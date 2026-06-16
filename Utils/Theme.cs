namespace StudentValidationSystem.Utils;

public class ThemePalette
{
    public Color Background { get; init; }
    public Color Surface { get; init; }
    public Color SurfaceAlt { get; init; }
    public Color Sidebar { get; init; }
    public Color Text { get; init; }
    public Color MutedText { get; init; }
    public Color Primary { get; init; }
    public Color Border { get; init; }
    public Color Success { get; init; }
    public Color Warning { get; init; }
    public Color Danger { get; init; }

    public static ThemePalette Light => new()
    {
        Background = Color.FromArgb(244, 247, 251),
        Surface = Color.White,
        SurfaceAlt = Color.FromArgb(235, 241, 248),
        Sidebar = Color.FromArgb(22, 33, 47),
        Text = Color.FromArgb(28, 38, 52),
        MutedText = Color.FromArgb(101, 116, 139),
        Primary = Color.FromArgb(30, 96, 145),
        Border = Color.FromArgb(214, 224, 236),
        Success = Color.FromArgb(39, 160, 112),
        Warning = Color.FromArgb(218, 151, 32),
        Danger = Color.FromArgb(210, 82, 82)
    };

    public static ThemePalette Dark => new()
    {
        Background = Color.FromArgb(18, 24, 33),
        Surface = Color.FromArgb(30, 40, 54),
        SurfaceAlt = Color.FromArgb(39, 51, 68),
        Sidebar = Color.FromArgb(12, 18, 27),
        Text = Color.FromArgb(232, 238, 247),
        MutedText = Color.FromArgb(154, 166, 184),
        Primary = Color.FromArgb(78, 166, 222),
        Border = Color.FromArgb(65, 79, 99),
        Success = Color.FromArgb(66, 190, 140),
        Warning = Color.FromArgb(232, 176, 64),
        Danger = Color.FromArgb(232, 100, 100)
    };
}

public static class Ui
{
    public static Font TitleFont => new("Segoe UI", 18, FontStyle.Bold);
    public static Font HeadingFont => new("Segoe UI", 12, FontStyle.Bold);
    public static Font BodyFont => new("Segoe UI", 9.5f, FontStyle.Regular);
    public static Font SmallFont => new("Segoe UI", 8.5f, FontStyle.Regular);

    public static Label Label(string text, Font? font, Color color, bool autoSize = true)
    {
        return new Label
        {
            Text = text,
            Font = font ?? BodyFont,
            ForeColor = color,
            AutoSize = autoSize,
            BackColor = Color.Transparent
        };
    }

    public static Button Button(string text, Color background, Color foreground)
    {
        return new Button
        {
            Text = text,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = background,
            ForeColor = foreground,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
    }

    public static void StyleGrid(DataGridView grid, ThemePalette theme)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = theme.Surface;
        grid.GridColor = theme.Border;
        grid.EnableHeadersVisualStyles = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.Font = BodyFont;
        grid.ColumnHeadersDefaultCellStyle.BackColor = theme.SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = theme.Surface;
        grid.DefaultCellStyle.ForeColor = theme.Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(57, 120, 168);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(2);
        grid.ColumnHeadersHeight = 32;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
    }

    public static Panel Card(ThemePalette theme, int padding = 16)
    {
        return new Panel
        {
            BackColor = theme.Surface,
            Padding = new Padding(padding),
            Margin = new Padding(0, 0, 14, 14)
        };
    }
}
