// re-c Windows host — weather (playtest 12: "make the trees move depending on weather patterns"). Not in the web port.
//
// One wind for the whole game, from the clock alone (deterministic, no state): a slow cycle of calm, breezy and stormy
// spells (a few minutes each, smooth value noise), gusts on top (seconds), and a direction that drifts between left and
// right. The HD shaders sway the foliage with it (taller canopies more than flat grass) and blow more leaves; the
// ambient wind bed follows the same strength, so what is heard matches what is seen.
namespace ReC.Win;

internal static class Weather
{
    /// <summary>playtest 13: Cheats > Weather: 0 auto (the cycle), 1 clear, 2 rain (snow in icy places), 3 storm</summary>
    public static int Force;
    public static readonly string[] ForceNames = { "auto", "clear", "rain", "storm" };

    private static double Hash(long i)
    {
        ulong x = (ulong)i * 0x9E3779B97F4A7C15UL;
        x ^= x >> 29; x *= 0xBF58476D1CE4E5B9UL; x ^= x >> 32;
        return (x & 0xFFFFFF) / (double)0xFFFFFF;
    }

    /// <summary>smooth value noise in 0..1 over t (one random value per unit, cosine-eased between them)</summary>
    public static double Noise(double t, long seed)
    {
        long i = (long)Math.Floor(t);
        double f = t - i, u = f * f * (3 - 2 * f);
        return Hash(i * 7919 + seed) * (1 - u) + Hash((i + 1) * 7919 + seed) * u;
    }

    /// <summary>(strength 0 calm .. 1 storm, gust 0..1, direction -1 left .. +1 right) at a time in seconds</summary>
    public static (double Strength, double Gust, double Dir) Wind(double seconds)
    {
        if (Force == 1) return (0.12, Noise(seconds / 2.5, 23), Noise(seconds / 40.0, 53) > 0.5 ? 1 : -1);
        if (Force == 2) return (0.45, Noise(seconds / 2.5, 23), 0.6);
        if (Force == 3) return (1.0, 0.6 + 0.4 * Noise(seconds / 1.2, 23), 1.0);
        double spell = Noise(seconds / 75.0, 11);                       // a spell lasts ~1-3 minutes
        double strength = Math.Clamp((spell - 0.25) / 0.6, 0, 1);
        strength = strength * strength * (3 - 2 * strength);
        double gust = Noise(seconds / 2.5, 23) * 0.6 + Noise(seconds / 0.9, 37) * 0.4;
        double dir = Noise(seconds / 40.0, 53) * 2 - 1;
        dir = Math.Sign(dir) * Math.Min(1, Math.Abs(dir) * 1.8);
        return (strength, gust, dir);
    }

    /// <summary>playtest 13 ("weather patterns: storms, rain, snow, heat"): precipitation 0..1 (rainy / snowy spells, a third of
    /// the time, heavier in strong wind), a lightning flash 0..1 in a storm (precipitation and wind both high: strikes a few
    /// seconds apart, a double flicker each) and the time of the latest strike (for its thunder)</summary>
    public static (double Precip, double Flash, double Strike) Sky(double seconds)
    {
        var (ws, _, _) = Wind(seconds);
        double n = Noise(seconds / 95.0, 71);
        double precip = Math.Clamp((n - 0.55) / 0.25, 0, 1);
        precip = precip * precip * (3 - 2 * precip);
        precip = Math.Min(1, precip * (0.7 + 0.5 * ws));
        if (Force == 1) precip = 0; else if (Force == 2) precip = 0.75; else if (Force == 3) precip = 1;
        double storm = Math.Clamp((ws - 0.55) / 0.3, 0, 1) * Math.Clamp((precip - 0.4) / 0.4, 0, 1);
        double flash = 0, strike = -1;
        if (storm > 0)
            for (long k = (long)Math.Floor(seconds) - 2; k <= (long)Math.Floor(seconds); k++)
            {
                if (Hash(k * 104729 + 99) > 0.12 + 0.25 * storm) continue;   // about one strike in 4-8 seconds
                double t0 = k + Hash(k * 104729 + 7);
                double u = seconds - t0;
                if (u < 0) continue;
                strike = t0;
                double f = Math.Exp(-u * 9) + 0.6 * Math.Exp(-Math.Abs(u - 0.16) * 30);   // a double flicker
                flash = Math.Max(flash, Math.Min(1, f) * storm);
            }
        return (precip, flash, strike);
    }
}
