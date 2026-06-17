namespace Polivoks.Resources.Rendering;

internal static class BitmapColor
{
    public const uint Chassis = 0xFF070708;
    public const uint Frame = 0xFF0B0B0D;
    public const uint ModuleFace = 0xFF182728;
    public const uint ModuleHeader = 0xFF0F1719;
    public const uint Gold = 0xFF4FD7D6;
    public const uint GoldDim = 0xFF225A5E;
    public const uint Text = 0xFFE4F3F4;
    public const uint KnobFace = 0xFF07090C;
    public const uint KnobRing = 0xFF4FD7D6;
    public const uint Pointer = 0xFF7AEAE8;
    public const uint KeyWhite = 0xFFD8E1DE;
    public const uint KeyBlack = 0xFF090B0F;
    public const uint LedOn = 0xFF4FD7D6;
    public const uint LedOff = 0xFF0B1E24;
    public const uint ButtonFace = 0xFF101A1C;
    public const uint ButtonActive = 0xFF1E656A;
    public const uint Track = 0xFF05080B;
    public const uint Shadow = 0xAA000000;
    public const uint Glass = 0xAA0B1114;
    public const uint Grid = 0xFF173236;
    public const uint GradientLeft = 0xFF071015;
    public const uint GradientRight = 0xFF102226;
    public const uint GlowCyan = 0xFF4FD7D6;
    public const uint GlowBlue = 0xFF327C96;

    public static uint FromRgb(byte r, byte g, byte b) => 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;

    public static uint AlphaBlend(uint bg, uint fg, byte alpha)
    {
        var a = alpha / 255.0;
        var inv = 1.0 - a;
        byte Br(uint c) => (byte)((c >> 16) & 0xFF);
        byte Bg(uint c) => (byte)((c >> 8) & 0xFF);
        byte Bb(uint c) => (byte)(c & 0xFF);
        var r = (byte)(Br(fg) * a + Br(bg) * inv);
        var g = (byte)(Bg(fg) * a + Bg(bg) * inv);
        var b = (byte)(Bb(fg) * a + Bb(bg) * inv);
        return FromRgb(r, g, b);
    }

    public static uint Lerp(uint from, uint to, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte R(uint c) => (byte)((c >> 16) & 0xFF);
        byte G(uint c) => (byte)((c >> 8) & 0xFF);
        byte B(uint c) => (byte)(c & 0xFF);
        var r = (byte)(R(from) + (R(to) - R(from)) * t);
        var g = (byte)(G(from) + (G(to) - G(from)) * t);
        var b = (byte)(B(from) + (B(to) - B(from)) * t);
        return FromRgb(r, g, b);
    }
}
