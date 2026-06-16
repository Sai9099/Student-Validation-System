using StudentValidationSystem.Database;
using StudentValidationSystem.Helpers;
using StudentValidationSystem.Utils;

namespace StudentValidationSystem.Views;

public class StudentProgressControl : UserControl
{
    private readonly StudentDatabase _database;
    private readonly ThemePalette _theme;

    public StudentProgressControl(StudentDatabase database, ThemePalette theme)
    {
        _database = database;
        _theme = theme;
        Build();
    }

    private void Build()
    {
        BackColor = _theme.Background;
        AutoScroll = true;
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = _theme.Background };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        Controls.Add(root);

        var attendance = MetricPanel("Attendance Progress", "AttendancePercentage", "Attendance", "Semester");
        root.Controls.Add(attendance, 0, 0);
        var marks = ChartPanel("Marks Progress", "SELECT Semester, GPA FROM Marks WHERE StudentId=$studentId ORDER BY Semester;", "GPA");
        root.Controls.Add(marks, 1, 0);
        var evidence = EvidencePanel();
        root.Controls.Add(evidence, 0, 1);
        var timeline = TimelinePanel();
        root.Controls.Add(timeline, 1, 1);
        var notes = NotesPanel();
        root.SetColumnSpan(notes, 2);
        root.Controls.Add(notes, 0, 2);
    }

    private Control MetricPanel(string title, string valueColumn, string tableName, string groupColumn)
    {
        var panel = Ui.Card(_theme, 18);
        panel.Height = 290;
        panel.Dock = DockStyle.Fill;
        var label = UiFactory.SectionTitle(title, _theme);
        label.Location = new Point(12, 12);
        panel.Controls.Add(label);
        var rows = _database.GetDataTable($"""
            SELECT {groupColumn} AS Semester, ROUND(AVG({valueColumn}), 1) AS Progress
            FROM {tableName} WHERE StudentId=$studentId GROUP BY {groupColumn} ORDER BY {groupColumn};
            """, ("$studentId", StudentSessionManager.StudentId));
        var y = 60;
        foreach (System.Data.DataRow row in rows.Rows)
        {
            var sem = row["Semester"].ToString();
            var pct = Convert.ToDouble(row["Progress"]);
            var text = Ui.Label($"Semester {sem}: {pct:0.0}%", Ui.BodyFont, _theme.Text);
            text.Location = new Point(14, y);
            panel.Controls.Add(text);
            var bar = new ProgressBar { Location = new Point(170, y + 4), Width = 330, Height = 16, Value = Math.Min(100, Math.Max(0, (int)pct)) };
            panel.Controls.Add(bar);
            y += 36;
        }
        return panel;
    }

    private Control ChartPanel(string title, string sql, string seriesName)
    {
        var panel = Ui.Card(_theme, 18);
        panel.Height = 290;
        panel.Dock = DockStyle.Fill;
        var label = UiFactory.SectionTitle(title, _theme);
        label.Location = new Point(12, 12);
        panel.Controls.Add(label);

        var rows = _database.GetDataTable(sql, ("$studentId", StudentSessionManager.StudentId));
        var points = new List<ProgressPoint>();
        foreach (System.Data.DataRow row in rows.Rows)
        {
            points.Add(new ProgressPoint($"Sem {row[0]}", Convert.ToDouble(row[1])));
        }

        var chart = new SimpleLineChart(points, seriesName, _theme.Primary, _theme.Border, _theme.MutedText)
        {
            Location = new Point(12, 54),
            Size = new Size(520, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = _theme.Surface,
            ForeColor = _theme.Text
        };
        panel.Controls.Add(chart);
        return panel;
    }

    private Control EvidencePanel()
    {
        var panel = Ui.Card(_theme, 18);
        panel.Height = 290;
        panel.Dock = DockStyle.Fill;
        var label = UiFactory.SectionTitle("Evidence Completion Progress", _theme);
        label.Location = new Point(12, 12);
        panel.Controls.Add(label);
        var rows = _database.GetDataTable("""
            SELECT ValidationStatus, COUNT(*) AS Count FROM LearningEvidence
            WHERE StudentId=$studentId GROUP BY ValidationStatus;
            """, ("$studentId", StudentSessionManager.StudentId));
        var y = 60;
        foreach (System.Data.DataRow row in rows.Rows)
        {
            var text = Ui.Label($"{row["ValidationStatus"]}: {row["Count"]}", new Font("Segoe UI", 13, FontStyle.Bold), _theme.Text);
            text.Location = new Point(14, y);
            panel.Controls.Add(text);
            y += 42;
        }
        return panel;
    }

    private Control TimelinePanel()
    {
        var panel = Ui.Card(_theme, 18);
        panel.Height = 290;
        panel.Dock = DockStyle.Fill;
        var label = UiFactory.SectionTitle("Achievement Timeline", _theme);
        label.Location = new Point(12, 12);
        panel.Controls.Add(label);
        var grid = new DataGridView { Location = new Point(12, 54), Size = new Size(520, 190), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        UiFactory.StyleGrid(grid, _theme);
        grid.DataSource = _database.GetDataTable("SELECT AchievementDate, AchievementType, Title FROM Achievements WHERE StudentId=$studentId ORDER BY AchievementDate DESC;", ("$studentId", StudentSessionManager.StudentId));
        panel.Controls.Add(grid);
        return panel;
    }

    private Control NotesPanel()
    {
        var panel = Ui.Card(_theme, 18);
        panel.Height = 150;
        panel.Dock = DockStyle.Fill;
        var summary = _database.GetDashboardSummary(StudentSessionManager.StudentId);
        var strengths = summary.CGPA >= 8 ? "Strong academic consistency and validated project evidence." : "Steady progress with room to improve GPA.";
        var improvement = summary.AttendancePercentage < 75 ? "Improve attendance immediately, especially low-attendance subjects." : "Continue evidence uploads before review deadlines.";
        var text = Ui.Label($"Strengths: {strengths}\nImprovement Areas: {improvement}", new Font("Segoe UI", 12, FontStyle.Bold), _theme.Text, false);
        text.Location = new Point(16, 20);
        text.Size = new Size(1000, 90);
        panel.Controls.Add(text);
        return panel;
    }

    private sealed record ProgressPoint(string Label, double Value);

    private sealed class SimpleLineChart : Control
    {
        private readonly List<ProgressPoint> _points;
        private readonly string _seriesName;
        private readonly Color _lineColor;
        private readonly Color _gridColor;
        private readonly Color _mutedTextColor;

        public SimpleLineChart(List<ProgressPoint> points, string seriesName, Color lineColor, Color gridColor, Color mutedTextColor)
        {
            _points = points;
            _seriesName = seriesName;
            _lineColor = lineColor;
            _gridColor = gridColor;
            _mutedTextColor = mutedTextColor;
            DoubleBuffered = true;
            Font = Ui.SmallFont;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var plot = new Rectangle(46, 18, Math.Max(80, Width - 70), Math.Max(70, Height - 54));
            using var gridPen = new Pen(_gridColor);
            using var axisPen = new Pen(_mutedTextColor);
            using var textBrush = new SolidBrush(ForeColor);
            using var mutedBrush = new SolidBrush(_mutedTextColor);

            g.DrawRectangle(axisPen, plot);
            for (var i = 1; i <= 4; i++)
            {
                var y = plot.Top + plot.Height * i / 5;
                g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            }

            g.DrawString(_seriesName, new Font(Font, FontStyle.Bold), textBrush, plot.Left, 0);
            if (_points.Count == 0)
            {
                g.DrawString("No progress data available.", Font, mutedBrush, plot.Left + 12, plot.Top + 55);
                return;
            }

            var min = Math.Min(0, _points.Min(p => p.Value));
            var max = Math.Max(10, _points.Max(p => p.Value));
            if (Math.Abs(max - min) < 0.01) max = min + 1;

            PointF ToPoint(int index, double value)
            {
                var x = _points.Count == 1
                    ? plot.Left + plot.Width / 2f
                    : plot.Left + index * plot.Width / (float)(_points.Count - 1);
                var y = plot.Bottom - (float)((value - min) / (max - min) * plot.Height);
                return new PointF(x, y);
            }

            using var linePen = new Pen(_lineColor, 3);
            using var pointBrush = new SolidBrush(_lineColor);
            var chartPoints = _points.Select((point, index) => ToPoint(index, point.Value)).ToArray();
            if (chartPoints.Length > 1) g.DrawLines(linePen, chartPoints);

            for (var i = 0; i < _points.Count; i++)
            {
                var point = chartPoints[i];
                g.FillEllipse(pointBrush, point.X - 4, point.Y - 4, 8, 8);
                g.DrawString(_points[i].Value.ToString("0.00"), Font, textBrush, point.X - 16, point.Y - 24);
                g.DrawString(_points[i].Label, Font, mutedBrush, point.X - 18, plot.Bottom + 8);
            }

            g.DrawString(max.ToString("0.0"), Font, mutedBrush, 8, plot.Top - 6);
            g.DrawString(min.ToString("0.0"), Font, mutedBrush, 8, plot.Bottom - 10);
        }
    }
}
