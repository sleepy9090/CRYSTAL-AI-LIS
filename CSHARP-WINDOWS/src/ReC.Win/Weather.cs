// re-c Windows host — weather (playtest 12: "make the trees move depending on weather patterns"). Not in the web port.
//
// One wind for the whole game, from the clock alone (deterministic, no state): a slow cycle of calm, breezy and stormy
// spells (a few minutes each, smooth value noise), gusts on top (seconds), and a direction that drifts between left and
// right. The HD shaders sway the foliage with it (taller canopies more than flat grass) and blow more leaves; the
// ambient wind bed follows the same strength, so what is heard matches what is seen.
namespace ReC.Win;

internal static class Weather
{
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
        double spell = Noise(seconds / 75.0, 11);                       // a spell lasts ~1-3 minutes
        double strength = Math.Clamp((spell - 0.25) / 0.6, 0, 1);
        strength = strength * strength * (3 - 2 * strength);
        double gust = Noise(seconds / 2.5, 23) * 0.6 + Noise(seconds / 0.9, 37) * 0.4;
        double dir = Noise(seconds / 40.0, 53) * 2 - 1;
        dir = Math.Sign(dir) * Math.Min(1, Math.Abs(dir) * 1.8);
        return (strength, gust, dir);
    }
}
