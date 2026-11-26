// re-c Windows host — display colour settings (T60, Video > Display). Not in the web port; pure logic, unit-tested
// (win_color). One 3x3 matrix on RGB (0..1, as shown) from three settings:
//   Brightness  5 levels: x 0.80 / 0.90 / 1.00 / 1.12 / 1.25
//   Colour      Natural, or Vivid (saturation x 1.25)
//   Colour-blind assist  Off, Protanopia, Deuteranopia, Tritanopia: daltonization — the colour as the deficiency sees
//               it (Machado et al. 2009, severity 1), the lost difference moved into channels that are still seen
//               (red-green loss into blue / green, blue-yellow loss into red / green), so colours the game tells apart
//               only by hue stay apart.
// Original / CRT apply it to the 64 NES colours (the palette lookup), the 16/32-bit looks to the upscaled picture, HD in
// PSFinal (constant rows 21-23).
namespace ReC.Win;

internal static class ColorAdjust
{
    public static readonly string[] BlindNames = { "off", "protan", "deutan", "tritan" };
    public static readonly float[] Gains = { 0.80f, 0.90f, 1.00f, 1.12f, 1.25f };

    private static readonly float[][] Sim =
    {
        new[] { 1f, 0, 0, 0, 1, 0, 0, 0, 1 },
        new[] { 0.152286f, 1.052583f, -0.204868f, 0.114503f, 0.786281f, 0.099216f, -0.003882f, -0.048116f, 1.051998f },
        new[] { 0.367322f, 0.860646f, -0.227968f, 0.280085f, 0.672501f, 0.047413f, -0.011820f, 0.042940f, 0.968881f },
        new[] { 1.255528f, -0.076749f, -0.178779f, -0.078411f, 0.930809f, 0.147602f, 0.004733f, 0.691367f, 0.303900f },
    };

    // where the lost difference goes (columns: the error's r, g, b). Tritan: the classic daltonize rows. Red-green: the
    // red error back into red (half), into green and into blue against the green error — chosen by searching on the 64
    // NES colours for the fewest pairs that differ (> 0.25) but look alike (< 0.1) to the simulated deficiency (win_color:
    // the classic rows left 20 such pairs for deuteranopia, these 4)
    private static readonly float[][] Shift =
    {
        new[] { 0f, 0, 0, 0, 0, 0, 0, 0, 0 },
        new[] { 0.5f, 0, 0, 0.7f, 0, 0, -0.35f, 0.35f, 0 },
        new[] { 0.5f, 0, 0, 0.7f, 0, 0, -0.7f, -0.7f, 0 },
        new[] { 1f, 0, 0.7f, 0, 1, 0.7f, 0, 0, 0 },
    };

    public static float[] Mul(float[] a, float[] b)
    {
        var r = new float[9];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                r[i * 3 + j] = a[i * 3] * b[j] + a[i * 3 + 1] * b[3 + j] + a[i * 3 + 2] * b[6 + j];
        return r;
    }

    public static float[] Simulate(int blind) => Sim[Math.Clamp(blind, 0, 3)];

    /// <summary>the combined matrix (row-major); identity for the defaults (brightness 2, natural, off)</summary>
    public static float[] Matrix(int brightness, bool vivid, int blind)
    {
        float g = Gains[Math.Clamp(brightness, 0, 4)], s = vivid ? 1.25f : 1f;
        const float lr = 0.299f, lg = 0.587f, lb = 0.114f;
        var sat = new[]
        {
            s + (1 - s) * lr, (1 - s) * lg, (1 - s) * lb,
            (1 - s) * lr, s + (1 - s) * lg, (1 - s) * lb,
            (1 - s) * lr, (1 - s) * lg, s + (1 - s) * lb,
        };
        int b = Math.Clamp(blind, 0, 3);
        // daltonize: c + Shift (c - Sim c) = (I + Shift (I - Sim)) c
        var iMinusSim = new float[9];
        for (int i = 0; i < 9; i++) iMinusSim[i] = (i % 4 == 0 ? 1 : 0) - Sim[b][i];
        var dal = Mul(Shift[b], iMinusSim);
        for (int i = 0; i < 9; i += 4) dal[i] += 1;
        var m = Mul(dal, sat);
        for (int i = 0; i < 9; i++) m[i] *= g;
        return m;
    }

    public static bool IsIdentity(float[] m)
    {
        for (int i = 0; i < 9; i++) if (Math.Abs(m[i] - (i % 4 == 0 ? 1 : 0)) > 1e-6f) return false;
        return true;
    }

    /// <summary>0x00RRGGBB through the matrix (clamped)</summary>
    public static uint Apply(float[] m, uint c)
    {
        float r = (c >> 16 & 255) / 255f, g = (c >> 8 & 255) / 255f, b = (c & 255) / 255f;
        int R = (int)MathF.Round(Math.Clamp(m[0] * r + m[1] * g + m[2] * b, 0, 1) * 255);
        int G = (int)MathF.Round(Math.Clamp(m[3] * r + m[4] * g + m[5] * b, 0, 1) * 255);
        int B = (int)MathF.Round(Math.Clamp(m[6] * r + m[7] * g + m[8] * b, 0, 1) * 255);
        return (uint)(R << 16 | G << 8 | B);
    }

    /// <summary>a whole picture, with a small cache (pictures have few distinct colours in runs)</summary>
    public static void ApplyAll(float[] m, uint[] src, uint[] dst, int n)
    {
        uint last = 0xFFFFFFFF, lastOut = 0;
        for (int i = 0; i < n; i++)
        {
            uint c = src[i] & 0xFFFFFF;
            if (c != last) { last = c; lastOut = Apply(m, c); }
            dst[i] = lastOut;
        }
    }
}
