using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PrintIntoBlack;

public partial class MainForm : Form
{
    const int InkHi = 215;
    const int InkLo = 28;

    readonly PictureBox logoThumb = MakeThumb();
    readonly PictureBox printThumb = MakeThumb();
    readonly PictureBox resultBox = MakeThumb();
    readonly Label logoHint = MakeHint();
    readonly Label printHint = MakeHint();
    readonly Label logoName = MakeLabel("No file");
    readonly Label printName = MakeLabel("No file");
    readonly Label sizeLabel = MakeLabel("> Output size matches the logo");
    readonly TermButton saveBtn;
    readonly TermButton logoChoose;
    readonly TermButton printChoose;

    static readonly Font ActionFont = new Font("Consolas", 10f, FontStyle.Bold);
    static readonly Size ActionSize = MeasureAction();

    string? logoPath;
    string? printPath;
    Bitmap? result;
    Bitmap? logoPrev;
    Bitmap? printPrev;

    public MainForm()
    {
        Text = "PRINT INTO BLACK";
        BackColor = Color.Black;
        ForeColor = Color.White;
        Font = new Font("Consolas", 10f);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        AutoScroll = false;
        ClientSize = new Size(980, 760);

        var title = new Label
        {
            Text = "> PRINT INTO BLACK",
            ForeColor = Color.White,
            BackColor = Color.Black,
            Font = new Font("Consolas", 18f, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(16, 18, 16, 16)
        };

        var inputs = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Black,
            Padding = new Padding(12, 0, 12, 8)
        };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        inputs.Controls.Add(MakeSlot("1  BLACK-AND-WHITE LOGO", logoThumb, logoHint, logoName, BrowseLogo, out logoChoose, "CHOOSE LOGO"), 0, 0);
        inputs.Controls.Add(MakeSlot("2  PRINT", printThumb, printHint, printName, BrowsePrint, out printChoose, "CHOOSE PRINT"), 1, 0);

        var resultPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Padding = new Padding(12, 0, 12, 8)
        };
        resultPanel.Controls.Add(resultBox);
        resultPanel.Controls.Add(MakeHeading("3  OUTPUT"));

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = ActionSize.Height + 24,
            BackColor = Color.Black
        };
        sizeLabel.AutoSize = false;
        sizeLabel.TextAlign = ContentAlignment.MiddleLeft;
        saveBtn = new TermButton
        {
            Text = "SAVE PNG",
            Font = ActionFont,
            Size = ActionSize,
            Ready = false
        };
        saveBtn.Activated += SaveClick;
        bottom.Controls.Add(saveBtn);
        bottom.Controls.Add(sizeLabel);
        bottom.Resize += (_, _) => PlaceBottom(bottom);
        PlaceBottom(bottom);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Black
        };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        body.Controls.Add(inputs, 0, 0);
        body.Controls.Add(resultPanel, 0, 1);

        Controls.Add(body);
        Controls.Add(bottom);
        Controls.Add(title);
        MinimumSize = Size;
    }

    void PlaceBottom(Panel bottom)
    {
        saveBtn.Location = new Point(bottom.ClientSize.Width - 16 - saveBtn.Width, (bottom.ClientSize.Height - saveBtn.Height) / 2);
        sizeLabel.Location = new Point(16, 0);
        sizeLabel.Size = new Size(Math.Max(0, saveBtn.Left - 28), bottom.ClientSize.Height);
    }

    static Size MeasureAction()
    {
        var flags = TextFormatFlags.NoPadding;
        var widest = 0;
        var tallest = 0;
        foreach (var label in new[] { "CHOOSE LOGO", "CHOOSE PRINT", "SAVE PNG" })
        {
            var s = TextRenderer.MeasureText(label, ActionFont, Size.Empty, flags);
            widest = Math.Max(widest, s.Width);
            tallest = Math.Max(tallest, s.Height);
        }
        return new Size(widest + 28, Math.Max(36, tallest + 16));
    }

    static PictureBox MakeThumb() => new()
    {
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.Black,
        BorderStyle = BorderStyle.FixedSingle,
        Dock = DockStyle.Fill
    };

    static Label MakeHint() => new()
    {
        Text = "> Drop image",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.White,
        BackColor = Color.Black,
        Font = new Font("Consolas", 10f)
    };

    static Label MakeLabel(string text) => new()
    {
        Text = text,
        ForeColor = Color.White,
        BackColor = Color.Black,
        AutoEllipsis = true,
        Font = new Font("Consolas", 10f)
    };

    static Label MakeHeading(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        AutoSize = true,
        ForeColor = Color.White,
        BackColor = Color.Black,
        Font = new Font("Consolas", 13f, FontStyle.Bold),
        Padding = new Padding(2, 4, 8, 12)
    };

    Panel MakeSlot(string title, PictureBox thumb, Label hint, Label name, EventHandler browse, out TermButton choose, string buttonText)
    {
        var p = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 8, 0),
            BackColor = Color.Black
        };
        var frame = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle
        };
        thumb.BorderStyle = BorderStyle.None;
        thumb.Dock = DockStyle.Fill;
        thumb.Visible = false;
        hint.Visible = true;
        WireDrop(p, title);
        WireDrop(frame, title);
        WireDrop(thumb, title);
        WireDrop(hint, title);

        var head = MakeHeading(title);

        name.Dock = DockStyle.Bottom;
        name.Height = 28;

        choose = new TermButton
        {
            Text = buttonText,
            Font = ActionFont,
            Size = ActionSize,
            Ready = true
        };
        choose.Activated += browse;
        var buttonRow = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = ActionSize.Height + 8,
            BackColor = Color.Black
        };
        choose.Location = new Point(0, 4);
        buttonRow.Controls.Add(choose);

        frame.Controls.Add(thumb);
        frame.Controls.Add(hint);
        p.Controls.Add(frame);
        p.Controls.Add(name);
        p.Controls.Add(buttonRow);
        p.Controls.Add(head);
        return p;
    }
}
