using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Polivoks.Resources.Rendering;

public sealed class BitmapCanvas : IDisposable
{
    private static readonly Dictionary<TextCacheKey, BitmapSource> TextCache = [];
    private readonly byte[] _buffer;
    private readonly int _pixelWidth;
    private readonly int _pixelHeight;
    private bool _dirty = true;

    public BitmapCanvas(int width, int height, int scale = 1)
    {
        Width = width;
        Height = height;
        Scale = Math.Max(1, scale);
        _pixelWidth = width * Scale;
        _pixelHeight = height * Scale;
        Bitmap = new WriteableBitmap(_pixelWidth, _pixelHeight, 96 * Scale, 96 * Scale, PixelFormats.Bgra32, null);
        _buffer = new byte[_pixelWidth * _pixelHeight * 4];
    }

    public WriteableBitmap Bitmap { get; }

    public int Width { get; }

    public int Height { get; }

    public int Scale { get; }

    public void CopyFrom(BitmapCanvas source)
    {
        if (source._pixelWidth != _pixelWidth || source._pixelHeight != _pixelHeight || source.Scale != Scale)
        {
            throw new ArgumentException("Canvas dimensions and scale must match.", nameof(source));
        }

        Buffer.BlockCopy(source._buffer, 0, _buffer, 0, _buffer.Length);
        _dirty = true;
    }

    public void Clear(uint color)
    {
        var c0 = (byte)(color & 0xFF);
        var c1 = (byte)((color >> 8) & 0xFF);
        var c2 = (byte)((color >> 16) & 0xFF);
        var c3 = (byte)((color >> 24) & 0xFF);
        for (var i = 0; i < _buffer.Length; i += 4)
        {
            _buffer[i] = c0;
            _buffer[i + 1] = c1;
            _buffer[i + 2] = c2;
            _buffer[i + 3] = c3;
        }

        _dirty = true;
    }

    public void FillRect(int x, int y, int w, int h, uint color)
    {
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var x0 = Math.Clamp(x * Scale, 0, _pixelWidth);
        var y0 = Math.Clamp(y * Scale, 0, _pixelHeight);
        var x1 = Math.Clamp((x + w) * Scale, 0, _pixelWidth);
        var y1 = Math.Clamp((y + h) * Scale, 0, _pixelHeight);
        if (x1 <= x0 || y1 <= y0)
        {
            return;
        }

        unsafe
        {
            fixed (byte* ptr = _buffer)
            {
                for (var row = y0; row < y1; row++)
                {
                    uint* line = (uint*)(ptr + row * _pixelWidth * 4);
                    for (var col = x0; col < x1; col++)
                    {
                        line[col] = color;
                    }
                }
            }
        }

        _dirty = true;
    }

    public void FillLinearGradientRect(int x, int y, int w, int h, uint left, uint right)
    {
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var x0 = Math.Clamp(x * Scale, 0, _pixelWidth);
        var y0 = Math.Clamp(y * Scale, 0, _pixelHeight);
        var x1 = Math.Clamp((x + w) * Scale, 0, _pixelWidth);
        var y1 = Math.Clamp((y + h) * Scale, 0, _pixelHeight);
        if (x1 <= x0 || y1 <= y0)
        {
            return;
        }

        for (var row = y0; row < y1; row++)
        {
            for (var col = x0; col < x1; col++)
            {
                var t = x1 <= x0 + 1 ? 0 : (col - x0) / (double)(x1 - x0 - 1);
                SetPixelRaw(col, row, BitmapColor.Lerp(left, right, t));
            }
        }
    }

    public void AddRadialGlow(int cx, int cy, int radius, uint color, double strength)
    {
        if (radius <= 0 || strength <= 0)
        {
            return;
        }

        var pcx = cx * Scale;
        var pcy = cy * Scale;
        var pradius = radius * Scale;
        var r2 = pradius * pradius;
        var x0 = Math.Clamp(pcx - pradius, 0, _pixelWidth - 1);
        var y0 = Math.Clamp(pcy - pradius, 0, _pixelHeight - 1);
        var x1 = Math.Clamp(pcx + pradius, 0, _pixelWidth - 1);
        var y1 = Math.Clamp(pcy + pradius, 0, _pixelHeight - 1);

        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                var dx = x - pcx;
                var dy = y - pcy;
                var d2 = dx * dx + dy * dy;
                if (d2 > r2)
                {
                    continue;
                }

                var distance = Math.Sqrt(d2) / pradius;
                var alpha = (byte)Math.Clamp((1.0 - distance) * (1.0 - distance) * strength * 255.0, 0.0, 255.0);
                if (alpha == 0)
                {
                    continue;
                }

                SetPixelRaw(x, y, BitmapColor.AlphaBlend(GetPixelRaw(x, y), color, alpha));
            }
        }
    }

    public void DrawRectBorder(int x, int y, int w, int h, uint color, int thickness = 1)
    {
        FillRect(x, y, w, thickness, color);
        FillRect(x, y + h - thickness, w, thickness, color);
        FillRect(x, y, thickness, h, color);
        FillRect(x + w - thickness, y, thickness, h, color);
    }

    public void FillCircle(int cx, int cy, int radius, uint color)
    {
        var pcx = cx * Scale;
        var pcy = cy * Scale;
        var pradius = radius * Scale;
        var r2 = pradius * pradius;
        for (var y = -pradius; y <= pradius; y++)
        {
            for (var x = -pradius; x <= pradius; x++)
            {
                if (x * x + y * y > r2)
                {
                    continue;
                }

                SetPixelRaw(pcx + x, pcy + y, color);
            }
        }
    }

    public void DrawLine(int x0, int y0, int x1, int y1, uint color)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
        SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void FillRoundedRect(int x, int y, int w, int h, double radius, uint fill, uint? stroke = null, double strokeThickness = 1)
    {
        if (w <= 0 || h <= 0)
        {
            return;
        }

        if (radius <= 0)
        {
            FillRect(x, y, w, h, fill);
            if (stroke is not null)
            {
                DrawRectBorder(x, y, w, h, stroke.Value, Math.Max(1, (int)Math.Round(strokeThickness)));
            }

            return;
        }

        RenderVector(x, y, w, h, dc =>
        {
            var rect = new Rect(strokeThickness / 2, strokeThickness / 2, Math.Max(0, w - strokeThickness), Math.Max(0, h - strokeThickness));
            dc.DrawRoundedRectangle(ToBrush(fill), stroke is null ? null : ToPen(stroke.Value, strokeThickness), rect, radius, radius);
        });
    }

    public void FillEllipse(int cx, int cy, int radiusX, int radiusY, uint fill, uint? stroke = null, double strokeThickness = 1)
    {
        if (radiusX <= 0 || radiusY <= 0)
        {
            return;
        }

        var w = radiusX * 2 + 4;
        var h = radiusY * 2 + 4;
        RenderVector(cx - radiusX - 2, cy - radiusY - 2, w, h, dc =>
        {
            dc.DrawEllipse(
                ToBrush(fill),
                stroke is null ? null : ToPen(stroke.Value, strokeThickness),
                new Point(radiusX + 2, radiusY + 2),
                radiusX,
                radiusY);
        });
    }

    public void DrawLineSmooth(double x0, double y0, double x1, double y1, uint color, double thickness = 1)
    {
        var minX = (int)Math.Floor(Math.Min(x0, x1) - thickness - 2);
        var minY = (int)Math.Floor(Math.Min(y0, y1) - thickness - 2);
        var maxX = (int)Math.Ceiling(Math.Max(x0, x1) + thickness + 2);
        var maxY = (int)Math.Ceiling(Math.Max(y0, y1) + thickness + 2);
        var w = Math.Max(1, maxX - minX);
        var h = Math.Max(1, maxY - minY);

        RenderVector(minX, minY, w, h, dc =>
        {
            dc.DrawLine(ToPen(color, thickness), new Point(x0 - minX, y0 - minY), new Point(x1 - minX, y1 - minY));
        });
    }

    public void DrawPolylineSmooth(IReadOnlyList<Point> points, uint color, double thickness = 1)
    {
        if (points.Count < 2)
        {
            return;
        }

        var minX = (int)Math.Floor(points.Min(p => p.X) - thickness - 2);
        var minY = (int)Math.Floor(points.Min(p => p.Y) - thickness - 2);
        var maxX = (int)Math.Ceiling(points.Max(p => p.X) + thickness + 2);
        var maxY = (int)Math.Ceiling(points.Max(p => p.Y) + thickness + 2);
        var w = Math.Max(1, maxX - minX);
        var h = Math.Max(1, maxY - minY);

        RenderVector(minX, minY, w, h, dc =>
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(points[0].X - minX, points[0].Y - minY), false, false);
                for (var i = 1; i < points.Count; i++)
                {
                    context.LineTo(new Point(points[i].X - minX, points[i].Y - minY), true, false);
                }
            }

            geometry.Freeze();
            dc.DrawGeometry(null, ToPen(color, thickness), geometry);
        });
    }

    public void SetPixel(int x, int y, uint color)
    {
        SetPixelRaw(x * Scale, y * Scale, color);
    }

    private void SetPixelRaw(int x, int y, uint color)
    {
        if (x < 0 || y < 0 || x >= _pixelWidth || y >= _pixelHeight)
        {
            return;
        }

        var i = (y * _pixelWidth + x) * 4;
        _buffer[i] = (byte)(color & 0xFF);
        _buffer[i + 1] = (byte)((color >> 8) & 0xFF);
        _buffer[i + 2] = (byte)((color >> 16) & 0xFF);
        _buffer[i + 3] = (byte)((color >> 24) & 0xFF);
        _dirty = true;
    }

    public void DrawText(string text, int x, int y, double fontSize, uint color, bool center = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var bitmap = GetTextBitmap(text, fontSize, color, Scale);
        var drawX = center ? x * Scale - bitmap.PixelWidth / 2 : x * Scale;
        Blit(drawX, y * Scale, bitmap);
    }

    private static BitmapSource GetTextBitmap(string text, double fontSize, uint color, int scale)
    {
        var key = new TextCacheKey(text, CultureInfo.CurrentUICulture.Name, fontSize, color, scale);
        if (TextCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)((color >> 24) & 0xFF),
            (byte)((color >> 16) & 0xFF),
            (byte)((color >> 8) & 0xFF),
            (byte)(color & 0xFF)));
        brush.Freeze();

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            fontSize,
            brush,
            1.0);

        var w = Math.Max(1, (int)Math.Ceiling(formatted.Width * scale) + 2 * scale);
        var h = Math.Max(1, (int)Math.Ceiling(formatted.Height * scale) + 2 * scale);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawText(formatted, new Point(1, 1));
        }

        var rtb = new RenderTargetBitmap(w, h, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        TextCache[key] = rtb;
        return rtb;
    }

    public void Blit(int destX, int destY, BitmapSource source)
    {
        var w = source.PixelWidth;
        var h = source.PixelHeight;
        var stride = w * 4;
        var src = new byte[stride * h];
        source.CopyPixels(src, stride, 0);

        for (var row = 0; row < h; row++)
        {
            var dy = destY + row;
                if (dy < 0 || dy >= _pixelHeight)
            {
                continue;
            }

            for (var col = 0; col < w; col++)
            {
                var dx = destX + col;
                if (dx < 0 || dx >= _pixelWidth)
                {
                    continue;
                }

                var si = row * stride + col * 4;
                var alpha = src[si + 3];
                if (alpha == 0)
                {
                    continue;
                }

                var fg = (uint)(src[si] | (src[si + 1] << 8) | (src[si + 2] << 16) | (src[si + 3] << 24));
                if (alpha == 255)
                {
                    SetPixelRaw(dx, dy, fg);
                }
                else
                {
                    var bg = GetPixelRaw(dx, dy);
                    SetPixelRaw(dx, dy, BitmapColor.AlphaBlend(bg, fg, alpha));
                }
            }
        }
    }

    public void Flush()
    {
        if (!_dirty)
        {
            return;
        }

        Bitmap.WritePixels(new Int32Rect(0, 0, _pixelWidth, _pixelHeight), _buffer, _pixelWidth * 4, 0);
        _dirty = false;
    }

    private uint GetPixelRaw(int x, int y)
    {
        var i = (y * _pixelWidth + x) * 4;
        return (uint)(_buffer[i] | (_buffer[i + 1] << 8) | (_buffer[i + 2] << 16) | (_buffer[i + 3] << 24));
    }

    private void RenderVector(int destX, int destY, int w, int h, Action<DrawingContext> draw)
    {
        var pixelW = Math.Max(1, w * Scale);
        var pixelH = Math.Max(1, h * Scale);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            draw(dc);
        }

        var rtb = new RenderTargetBitmap(pixelW, pixelH, 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
        rtb.Render(visual);
        Blit(destX * Scale, destY * Scale, rtb);
    }

    private static SolidColorBrush ToBrush(uint color)
    {
        var brush = new SolidColorBrush(ToColor(color));
        brush.Freeze();
        return brush;
    }

    private static Pen ToPen(uint color, double thickness)
    {
        var pen = new Pen(ToBrush(color), thickness)
        {
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat,
            LineJoin = PenLineJoin.Miter,
        };
        pen.Freeze();
        return pen;
    }

    private static Color ToColor(uint color) => Color.FromArgb(
        (byte)((color >> 24) & 0xFF),
        (byte)((color >> 16) & 0xFF),
        (byte)((color >> 8) & 0xFF),
        (byte)(color & 0xFF));

    public void Dispose() => Bitmap.Freeze();

    private readonly record struct TextCacheKey(string Text, string Culture, double FontSize, uint Color, int Scale);
}
