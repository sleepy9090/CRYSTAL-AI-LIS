// re-c Windows host — pixel-art upscale filters for the Video > Upscale menu: Scale2x / Scale3x (the AdvanceMAME
// edge rules, a.k.a. EPX) and Scale4x (= Scale2x twice). They round diagonal steps without blurring: each output pixel
// is a copy of the source pixel or of one of its 4 neighbours. The upscaled picture is then fitted to the window as
// usual (Video.cs Layout; GDI stretch, smooth filter optional). Pure logic, unit-tested on Linux (WinHostTests.cs).
namespace ReC.Win;

internal static class Upscale
{
    public static readonly (int K, string Key, string Label)[] Filters =
    {
        (1, "none", "&None (sharp pixels)"),
        (2, "scale2x", "Scale&2x"),
        (3, "scale3x", "Scale&3x"),
        (4, "scale4x", "Scale&4x"),
    };

    /// <summary>src (w x h) -> dst (w*k x h*k), k = 1..4; dst must hold w*h*k*k pixels</summary>
    public static void Apply(uint[] src, int w, int h, int k, uint[] dst, uint[]? tmp = null)
    {
        switch (k)
        {
            case 2: Scale2x(src, w, h, dst); break;
            case 3: Scale3x(src, w, h, dst); break;
            case 4:
                tmp ??= new uint[w * h * 4];
                Scale2x(src, w, h, tmp);
                Scale2x(tmp, w * 2, h * 2, dst);
                break;
            default: Array.Copy(src, dst, w * h); break;
        }
    }

    public static void Scale2x(uint[] s, int w, int h, uint[] d)
    {
        int dw = w * 2;
        for (int y = 0; y < h; y++)
        {
            int up = Math.Max(y - 1, 0) * w, row = y * w, dn = Math.Min(y + 1, h - 1) * w;
            int o = y * 2 * dw;
            for (int x = 0; x < w; x++)
            {
                int xl = Math.Max(x - 1, 0), xr = Math.Min(x + 1, w - 1);
                uint b = s[up + x], d0 = s[row + xl], e = s[row + x], f = s[row + xr], hh = s[dn + x];
                uint e0 = e, e1 = e, e2 = e, e3 = e;
                if (b != hh && d0 != f)
                {
                    if (d0 == b) e0 = d0;
                    if (b == f) e1 = f;
                    if (d0 == hh) e2 = d0;
                    if (hh == f) e3 = f;
                }
                int p = o + x * 2;
                d[p] = e0; d[p + 1] = e1; d[p + dw] = e2; d[p + dw + 1] = e3;
            }
        }
    }

    public static void Scale3x(uint[] s, int w, int h, uint[] d)
    {
        int dw = w * 3;
        for (int y = 0; y < h; y++)
        {
            int up = Math.Max(y - 1, 0) * w, row = y * w, dn = Math.Min(y + 1, h - 1) * w;
            int o = y * 3 * dw;
            for (int x = 0; x < w; x++)
            {
                int xl = Math.Max(x - 1, 0), xr = Math.Min(x + 1, w - 1);
                uint a = s[up + xl], b = s[up + x], c = s[up + xr];
                uint dd = s[row + xl], e = s[row + x], f = s[row + xr];
                uint g = s[dn + xl], hh = s[dn + x], i = s[dn + xr];
                uint e0 = e, e1 = e, e2 = e, e3 = e, e5 = e, e6 = e, e7 = e, e8 = e;
                if (b != hh && dd != f)
                {
                    if (dd == b) e0 = dd;
                    if ((dd == b && e != c) || (b == f && e != a)) e1 = b;
                    if (b == f) e2 = f;
                    if ((dd == b && e != g) || (dd == hh && e != a)) e3 = dd;
                    if ((b == f && e != i) || (hh == f && e != c)) e5 = f;
                    if (dd == hh) e6 = dd;
                    if ((dd == hh && e != i) || (hh == f && e != g)) e7 = hh;
                    if (hh == f) e8 = f;
                }
                int p = o + x * 3;
                d[p] = e0; d[p + 1] = e1; d[p + 2] = e2;
                d[p + dw] = e3; d[p + dw + 1] = e; d[p + dw + 2] = e5;
                d[p + 2 * dw] = e6; d[p + 2 * dw + 1] = e7; d[p + 2 * dw + 2] = e8;
            }
        }
    }
}
