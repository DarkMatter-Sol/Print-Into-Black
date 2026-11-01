namespace PrintIntoBlack;

sealed class TermButton : Control, IButtonControl
{
    bool hot;
    bool down;
    bool ready;

    public event EventHandler? Activated;

    public TermButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Color.Black;
        ForeColor = Color.White;
        TabStop = true;
    }

    public bool Ready
    {
        get => ready;
        set
        {
            ready = value;
            Cursor = ready ? Cursors.Hand : Cursors.Default;
            if (!ready)
            {
                hot = false;
                down = false;
            }
            Invalidate();
        }
    }

    public DialogResult DialogResult { get; set; }

    public void NotifyDefault(bool value)
    {
    }

    public void PerformClick()
    {
        if (!ready) return;
        Activated?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!ready) return;
        hot = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hot = false;
        down = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!ready || e.Button != MouseButtons.Left) return;
        down = true;
        Focus();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var fire = ready && down && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location);
        down = false;
        Invalidate();
        base.OnMouseUp(e);
        if (fire) PerformClick();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            if (ready) PerformClick();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Color fill;
        Color text;
        if (!ready)
        {
            fill = Color.Black;
            text = Color.White;
        }
        else if (down)
        {
            fill = Color.FromArgb(190, 190, 190);
            text = Color.Black;
        }
        else if (hot)
        {
            fill = Color.FromArgb(232, 232, 232);
            text = Color.Black;
        }
        else
        {
            fill = Color.White;
            text = Color.Black;
        }

        using var brush = new SolidBrush(fill);
        g.FillRectangle(brush, ClientRectangle);
        using var pen = new Pen(Color.White);
        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        TextRenderer.DrawText(
            g,
            Text,
            Font,
            ClientRectangle,
            text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
