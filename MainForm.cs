using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;

namespace MarasalBossTakip;

public sealed class MainForm : Form
{
    private sealed class ChannelTimer
    {
        public int Channel { get; init; }
        public DateTime Next { get; set; }
    }

    private readonly Dictionary<int, ChannelTimer> timers = new();
    private readonly DataGridView grid = new();
    private readonly NumericUpDown hours = new();
    private readonly NumericUpDown minutes = new();
    private readonly Label nextBoss = new();
    private readonly Label nextChannel = new();
    private readonly Label status = new();
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly string[] rowColors = { "#168BFF", "#18C76B", "#FF9700", "#9C4DFF", "#FFB300", "#FF3D2E" };

    public MainForm()
    {
        Text = "MARAŞAL - Boss / Metin Takip";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1200, 720);
        MinimumSize = new Size(1050, 650);
        BackColor = Color.FromArgb(7, 8, 10);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        DoubleBuffered = true;

        BuildUi();
        LoadDefaults();

        timer.Interval = 1000;
        timer.Tick += (_, _) => UpdateTimers();
        timer.Start();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var bg = new SolidBrush(Color.FromArgb(7, 8, 10));
        e.Graphics.FillRectangle(bg, ClientRectangle);
    }

    private void BuildUi()
    {
        var title = new Label {
            Text = "♜  MARAŞAL  -  Boss / Metin Takip",
            Font = new Font("Segoe UI Semibold", 17, FontStyle.Bold),
            ForeColor = Color.FromArgb(232, 195, 116),
            AutoSize = true,
            Location = new Point(24, 18)
        };
        Controls.Add(title);

        var line = new Panel { Location = new Point(20, 55), Size = new Size(1160, 1), BackColor = Color.FromArgb(80, 60, 25) };
        Controls.Add(line);

        // Sol sanat paneli
        var art = new PictureBox {
            Location = new Point(20, 75),
            Size = new Size(335, 570),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = LoadArt()
        };
        Controls.Add(art);

        var artShade = new Panel {
            Location = art.Location,
            Size = art.Size,
            BackColor = Color.FromArgb(80, 0, 0, 0)
        };
        Controls.Add(artShade);
        artShade.BringToFront();

        var marasal = new Label {
            Text = "MARAŞAL",
            Font = new Font("Georgia", 25, FontStyle.Bold),
            ForeColor = Color.FromArgb(235, 199, 123),
            AutoSize = true,
            Location = new Point(100, 575),
            BackColor = Color.Transparent
        };
        Controls.Add(marasal);
        marasal.BringToFront();

        // Üst ayarlar
        var settings = MakePanel(new Point(375, 75), new Size(805, 78));
        Controls.Add(settings);

        AddLabel(settings, "TEKRAR ARALIĞI", new Point(20, 10), Color.FromArgb(225, 184, 105), true);
        AddLabel(settings, "Saat", new Point(20, 38), Color.LightGray);
        hours.Location = new Point(55, 34);
        hours.Size = new Size(52, 28);
        hours.Maximum = 99;
        hours.Value = 1;
        settings.Controls.Add(hours);

        AddLabel(settings, "Dakika", new Point(125, 38), Color.LightGray);
        minutes.Location = new Point(180, 34);
        minutes.Size = new Size(52, 28);
        minutes.Maximum = 59;
        settings.Controls.Add(minutes);

        var start = MakeButton("TAKİBİ BAŞLAT", new Point(265, 31), new Size(145, 34));
        start.Click += (_, _) => StartTracking();
        settings.Controls.Add(start);

        var reset = MakeButton("SÜRELERİ SIFIRLA", new Point(420, 31), new Size(145, 34));
        reset.Click += (_, _) => ResetTracking();
        settings.Controls.Add(reset);

        var sound = MakeButton("🔊  SES AÇIK", new Point(580, 31), new Size(110, 34));
        settings.Controls.Add(sound);

        // Kanal tablosu
        var tablePanel = MakePanel(new Point(375, 170), new Size(805, 360));
        Controls.Add(tablePanel);

        grid.Location = new Point(8, 45);
        grid.Size = new Size(789, 305);
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.ReadOnly = true;
        grid.BackgroundColor = Color.FromArgb(8, 9, 11);
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Color.FromArgb(35, 35, 35);
        grid.DefaultCellStyle = new DataGridViewCellStyle {
            BackColor = Color.FromArgb(12, 13, 16),
            ForeColor = Color.White,
            SelectionBackColor = Color.FromArgb(35, 35, 35),
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10),
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
            BackColor = Color.FromArgb(18, 17, 15),
            ForeColor = Color.FromArgb(225, 184, 105),
            Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
            Padding = new Padding(8)
        };
        grid.ColumnHeadersHeight = 40;
        grid.RowTemplate.Height = 42;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add("Channel", "KANAL");
        grid.Columns.Add("Next", "SONRAKİ ÇIKIŞ");
        grid.Columns.Add("Remaining", "KALAN SÜRE");
        grid.Columns.Add("State", "DURUM");
        tablePanel.Controls.Add(grid);

        AddLabel(tablePanel, "KANALLAR", new Point(18, 12), Color.FromArgb(225, 184, 105), true);

        // Sonraki boss paneli
        var boss = MakePanel(new Point(375, 545), new Size(805, 100));
        Controls.Add(boss);
        AddLabel(boss, "SONRAKİ BOSS", new Point(22, 14), Color.FromArgb(225, 184, 105), true);
        nextBoss.Text = "--:--:--";
        nextBoss.Font = new Font("Segoe UI", 27, FontStyle.Bold);
        nextBoss.ForeColor = Color.FromArgb(20, 145, 255);
        nextBoss.AutoSize = true;
        nextBoss.Location = new Point(250, 27);
        boss.Controls.Add(nextBoss);
        nextChannel.Text = "CH -";
        nextChannel.Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold);
        nextChannel.ForeColor = Color.White;
        nextChannel.AutoSize = true;
        nextChannel.Location = new Point(475, 42);
        boss.Controls.Add(nextChannel);

        status.Text = "● Hazır";
        status.ForeColor = Color.FromArgb(50, 200, 100);
        status.AutoSize = true;
        status.Location = new Point(1040, 25);
        Controls.Add(status);
    }

    private Image? LoadArt()
{
    string path = System.IO.Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "marasal_background.jpg"
    );

    return System.IO.File.Exists(path)
        ? Image.FromFile(path)
        : null;
}

    private static Panel MakePanel(Point location, Size size)
    {
        return new Panel {
            Location = location,
            Size = size,
            BackColor = Color.FromArgb(13, 14, 17),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static void AddLabel(Control parent, string text, Point location, Color color, bool bold = false)
    {
        var l = new Label {
            Text = text,
            Location = location,
            AutoSize = true,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5f, bold ? FontStyle.Bold : FontStyle.Regular)
        };
        parent.Controls.Add(l);
    }

    private static Button MakeButton(string text, Point location, Size size)
    {
        return new Button {
            Text = text,
            Location = location,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(23, 22, 20),
            ForeColor = Color.FromArgb(225, 184, 105),
            FlatAppearance = { BorderColor = Color.FromArgb(95, 72, 30), BorderSize = 1 }
        };
    }

    private void LoadDefaults()
    {
        int[] channels = { 1, 2, 3, 4, 5, 6 };
        string[] times = { "00:26:10", "00:32:02", "00:26:30", "00:32:25", "00:26:55", "00:32:40" };

        for (int i = 0; i < channels.Length; i++)
        {
            int row = grid.Rows.Add($"CH{channels[i]}", times[i], "--:--:--", "HAZIR");
            grid.Rows[row].Cells["Channel"].Style.ForeColor = ColorTranslator.FromHtml(rowColors[i]);
        }
    }

    private void StartTracking()
    {
        TimeSpan interval = new((int)hours.Value, (int)minutes.Value, 0);
        if (interval <= TimeSpan.Zero)
        {
            MessageBox.Show("Tekrar aralığı 0 olamaz.");
            return;
        }

        timers.Clear();

        foreach (DataGridViewRow row in grid.Rows)
        {
            int ch = int.Parse(row.Cells["Channel"].Value!.ToString()!.Replace("CH", ""));
            if (!TimeSpan.TryParse(row.Cells["Next"].Value?.ToString(), out TimeSpan first))
                continue;

            DateTime next = DateTime.Today.Add(first);
            while (next <= DateTime.Now) next = next.Add(interval);
            timers[ch] = new ChannelTimer { Channel = ch, Next = next };
        }

        status.Text = "● TAKİP AKTİF";
        status.ForeColor = Color.FromArgb(50, 210, 105);
        UpdateTimers();
    }

    private void ResetTracking()
    {
        timers.Clear();
        foreach (DataGridViewRow row in grid.Rows)
        {
            row.Cells["Remaining"].Value = "--:--:--";
            row.Cells["State"].Value = "HAZIR";
        }
        nextBoss.Text = "--:--:--";
        nextChannel.Text = "CH -";
        status.Text = "● Hazır";
    }

    private void UpdateTimers()
    {
        if (timers.Count == 0) return;
        TimeSpan interval = new((int)hours.Value, (int)minutes.Value, 0);
        DateTime now = DateTime.Now;
        DateTime? soonest = null;
        int soonestChannel = 0;

        foreach (DataGridViewRow row in grid.Rows)
        {
            int ch = int.Parse(row.Cells["Channel"].Value!.ToString()!.Replace("CH", ""));
            if (!timers.TryGetValue(ch, out var ct)) continue;

            if (ct.Next <= now)
            {
                SystemSounds.Exclamation.Play();
                while (ct.Next <= now) ct.Next = ct.Next.Add(interval);
            }

            TimeSpan left = ct.Next - now;
            row.Cells["Next"].Value = ct.Next.ToString("HH:mm:ss");
            row.Cells["Remaining"].Value = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
            row.Cells["State"].Value = "BEKLENİYOR";

            if (soonest is null || ct.Next < soonest)
            {
                soonest = ct.Next;
                soonestChannel = ch;
            }
        }

        if (soonest.HasValue)
        {
            TimeSpan left = soonest.Value - now;
            nextBoss.Text = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
            nextChannel.Text = $"CH {soonestChannel}";
        }
    }
}
