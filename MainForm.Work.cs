using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PrintIntoBlack;

public partial class MainForm
{
    void WireDrop(Control target, string title)
    {
        target.AllowDrop = true;
        target.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        target.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
            if (title.StartsWith("1")) SetLogo(files[0]);
            else SetPrint(files[0]);
        };
    }

    void BrowseLogo(object? s, EventArgs e)
    {
        var path = Pick("Choose black-and-white logo");
        if (path != null) SetLogo(path);
    }

    void BrowsePrint(object? s, EventArgs e)
    {
        var path = Pick("Choose print");
        if (path != null) SetPrint(path);
    }

    static string? Pick(string title)
    {
        using var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp|All files|*.*"
        };
        return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
    }

    void SetLogo(string path)
    {
        try
        {
            ShowThumb(logoThumb, logoName, path, ref logoPrev);
            logoPath = path;
            logoHint.Visible = false;
            logoThumb.Visible = true;
            TryRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PRINT INTO BLACK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void SetPrint(string path)
    {
        try
        {
            ShowThumb(printThumb, printName, path, ref printPrev);
            printPath = path;
            printHint.Visible = false;
            printThumb.Visible = true;
            TryRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PRINT INTO BLACK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    static void ShowThumb(PictureBox box, Label caption, string path, ref Bitmap? held)
    {
        var next = LoadArgb(path);
        var old = held;
        held = next;
        box.Image = held;
        old?.Dispose();
        caption.Text = Path.GetFileName(path);
    }

    void TryRender()
    {
        if (logoPath is null || printPath is null) return;
        UseWaitCursor = true;
        try
        {
            var next = Compose(logoPath, printPath);
            var old = result;
            result = next;
            resultBox.Image = result;
            old?.Dispose();
            sizeLabel.Text = $"> Output is {result.Width} x {result.Height}";
            saveBtn.Ready = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PRINT INTO BLACK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    void SaveClick(object? s, EventArgs e)
    {
        if (!saveBtn.Ready || result is null) return;
        var suggested = logoPath is null
            ? "print.png"
            : Path.GetFileNameWithoutExtension(logoPath) + "-print.png";
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG|*.png",
            DefaultExt = "png",
            FileName = suggested
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            result.Save(dlg.FileName, ImageFormat.Png);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        result?.Dispose();
        logoPrev?.Dispose();
        printPrev?.Dispose();
        base.OnFormClosed(e);
    }

    static Bitmap Compose(string logoFile, string printFile)
    {
        using var logo = LoadArgb(logoFile);
        using var print = LoadArgb(printFile);
        using var fitted = Cover(print, logo.Width, logo.Height);
        return Blend(logo, fitted);
    }

    static Bitmap LoadArgb(string path)
    {
        using var raw = new Bitmap(path);
        ApplyExif(raw);
        var copy = new Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(copy);
        g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
        return copy;
    }

    static void ApplyExif(Image img)
    {
        try
        {
            const int id = 0x0112;
            if (Array.IndexOf(img.PropertyIdList, id) < 0) return;
            var o = img.GetPropertyItem(id)!.Value![0];
            var flip = o switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.RotateNoneFlipY,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone
            };
            if (flip != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(flip);
        }
        catch
        {
            // Broken orientation tags are ignored.
        }
    }

    static Bitmap Cover(Bitmap src, int w, int h)
    {
        var dest = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(dest);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Black);
        var scale = Math.Max((float)w / src.Width, (float)h / src.Height);
        var dw = src.Width * scale;
        var dh = src.Height * scale;
        g.DrawImage(src, (w - dw) / 2f, (h - dh) / 2f, dw, dh);
        return dest;
    }

    static Bitmap Blend(Bitmap logo, Bitmap print)
    {
        int w = logo.Width, h = logo.Height;
        var rect = new Rectangle(0, 0, w, h);
        var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        BitmapData? ld = null, pd = null, od = null;
        try
        {
            ld = logo.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            pd = print.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            od = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int ls = ld.Stride, ps = pd.Stride, os = od.Stride;
            var lb = new byte[Math.Abs(ls) * h];
            var pb = new byte[Math.Abs(ps) * h];
            var ob = new byte[Math.Abs(os) * h];
            Marshal.Copy(ld.Scan0, lb, 0, lb.Length);
            Marshal.Copy(pd.Scan0, pb, 0, pb.Length);

            float span = InkHi - InkLo;
            for (int y = 0; y < h; y++)
            {
                int lrow = y * ls, prow = y * ps, orow = y * os;
                for (int x = 0; x < w; x++)
                {
                    int li = lrow + x * 4, pi = prow + x * 4, oi = orow + x * 4;
                    int b = lb[li], gch = lb[li + 1], r = lb[li + 2], a = lb[li + 3];
                    float ink = (InkHi - (r + gch + b) / 3f) / span;
                    if (ink < 0) ink = 0;
                    if (ink > 1) ink = 1;
                    ink *= a / 255f;
                    float keep = 1f - ink;
                    ob[oi] = (byte)(pb[pi] * ink + b * keep + 0.5f);
                    ob[oi + 1] = (byte)(pb[pi + 1] * ink + gch * keep + 0.5f);
                    ob[oi + 2] = (byte)(pb[pi + 2] * ink + r * keep + 0.5f);
                    ob[oi + 3] = (byte)a;
                }
            }
            Marshal.Copy(ob, 0, od.Scan0, ob.Length);
        }
        finally
        {
            if (ld != null) logo.UnlockBits(ld);
            if (pd != null) print.UnlockBits(pd);
            if (od != null) result.UnlockBits(od);
        }
        return result;
    }
}
