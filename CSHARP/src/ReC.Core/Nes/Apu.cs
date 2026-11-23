// re-c C# port — 2A03 APU (= web/src/nes/apu.js). STUB until TASKS.md T2: register writes are kept, no synthesis.
namespace ReC.Nes;

public sealed class Apu
{
    private readonly Func<int, int> _dmcRead;
    private readonly byte[] _regs = new byte[0x18];
    public int SampleRate;
    public Action<float[], int>? Out;

    public Apu(Func<int, int>? dmcRead = null) { _dmcRead = dmcRead ?? (_ => 0); }

    public void SetOutput(int rate, Action<float[], int>? sink) { SampleRate = rate; Out = sink; }

    public void Write(int a, int v) { if (a >= 0x4000 && a < 0x4018) _regs[a - 0x4000] = (byte)v; }

    public int Read(int a) => 0;

    public void Run(int n) { _ = _dmcRead; }
}
