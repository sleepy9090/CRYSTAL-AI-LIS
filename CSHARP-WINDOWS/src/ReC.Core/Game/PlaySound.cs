// re-c C# port — PlaySound 30:C125 (= web/src/game/playsound.js, s55): the RAM side of the sound API (queue
// $0100-$0108). The driver that consumes the queue is Sound/SoundUpdate.cs + Sound/Driver.cs.
//
//   PlaySound.Play(m, id, y)   y = the caller's Y (the ROM saves it in snd_saved_y $010F and reloads it)
//
// ids < $20 are music: dropped if equal to snd_cur_music $0102, else $0102 = id. $80-$9F lose b7. When the
// queue holds 5 entries, an id >= $20 overwrites the last one ($0108) and music is dropped.
namespace ReC.Game;

public static class PlaySound
{
    // js: game/playsound.js playSound
    /// <summary>PlaySound 30:C125</summary>
    public static void Play(Machine m, int id, int y = 0)
    {
        int a = id & 0xFF;
        m.Wr(0x010F, y);                                         // C125 STY snd_saved_y
        if (a < 0xA0)
        {
            if ((a & 0x80) != 0) a &= 0x7F;                      // C13D
            else if (a < 0x20)
            {
                if (a == m.Rd(0x0102)) return;                   // C132 same music: C165
                m.Wr(0x0102, a);
            }
        }
        m.Wr(0x0101, 1);                                         // C13F snd_queue_lock
        int q = m.Rd(0x0100);
        if (q == 5)
        {
            if (a < 0x20) { m.Wr(0x0101, 0); return; }           // C14E full: music dropped
            q = 5;
        }
        m.Wr(0x0103 + q, a);                                     // C154
        bool added = q < 5;
        q = (q + 1) & 0xFF;
        if (q == 6) q--;
        m.Wr(0x0100, q);
        m.Wr(0x0101, 0);                                         // C160
        if (added) LagModel.LateSoundCheck(m);                   // s215: queued after tools.emu's HUD IRQ? (lagmodel.js)
    }
}
