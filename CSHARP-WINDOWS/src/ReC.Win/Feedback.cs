// re-c Windows host — damage feedback (T57, Game > Damage feedback). Not in the web port; pure logic, unit-tested
// (win_feedback). Watches the player's life ($03C1, max $03C0, the HudHpBar inputs) once per frame during gameplay
// (main_state 1, sub_state 8 = SubstPlay): a drop = a hit -> Hurt jumps to 1 and decays over 27 frames (~0.45 s) (the HD engine
// flashes a red vignette with it) and the controller rumbles (bigger hits rumble harder); life at a quarter of the
// maximum or less (not 0) = Low (the HD engine pulses the screen edges like a heartbeat). Cheats that refill life
// only raise it, which never counts as a hit.
namespace ReC.Win;

internal sealed class DamageFeedback
{
    private int _prevHp = -1;
    private long _hitAt = long.MinValue / 2;
    public float Hurt { get; private set; }
    public bool Low { get; private set; }
    /// <summary>the size of the last hit relative to the maximum (0..1), for the rumble strength</summary>
    public double LastHit { get; private set; }

    /// <summary>one game frame (its number); returns true on the frame of a hit</summary>
    public bool Update(int hp, int max, bool playing, long frame)
    {
        bool hit = false;
        if (playing && _prevHp >= 0 && hp < _prevHp && max > 0)
        {
            hit = true;
            _hitAt = frame;
            LastHit = Math.Clamp((_prevHp - hp) / (double)max, 0, 1);
        }
        _prevHp = playing ? hp : -1;
        Hurt = (float)Math.Max(0, 1 - (frame - _hitAt) / 27.0);    // game frames: ~0.45 s, deterministic
        Low = playing && max > 0 && hp > 0 && hp * 4 <= max;
        return hit;
    }
}
