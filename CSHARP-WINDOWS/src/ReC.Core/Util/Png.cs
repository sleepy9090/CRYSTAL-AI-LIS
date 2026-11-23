// re-c C# port — PNG writer + framebuffer -> RGB (= web/test/harness.mjs encodePng / fbToRgb) and CRC-32
// (= node:zlib crc32, used by the compare CSV's pal_crc / oam_crc and PNG chunks). BCL only (ZLibStream).
using System.IO.Compression;

namespace ReC.Util;

public static class Crc32
{
    private static readonly uint[] Table = Build();

    private static uint[] Build()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    public static uint Compute(ReadOnlySpan<byte> data, uint crc = 0)
    {
        crc ^= 0xFFFFFFFFu;
        foreach (byte b in data) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}

public static class Png
{
    private static void Chunk(Stream s, string tag, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        s.Write(len);
        var td = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) td[i] = (byte)tag[i];
        data.CopyTo(td.AsSpan(4));
        s.Write(td);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(len, Crc32.Compute(td));
        s.Write(len);
    }

    /// <summary>rgb: w*h*3 bytes -> PNG bytes (colour type 2, filter 0 on every row)</summary>
    public static byte[] Encode(int w, int h, byte[] rgb)
    {
        var raw = new byte[(w * 3 + 1) * h];
        for (int y = 0; y < h; y++)
        {
            raw[y * (w * 3 + 1)] = 0;
            Buffer.BlockCopy(rgb, y * w * 3, raw, y * (w * 3 + 1) + 1, w * 3);
        }
        var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.SmallestSize, leaveOpen: true)) zs.Write(raw);
        var ihdr = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0), (uint)w);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)h);
        ihdr[8] = 8; ihdr[9] = 2;
        var o = new MemoryStream();
        o.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        Chunk(o, "IHDR", ihdr);
        Chunk(o, "IDAT", z.ToArray());
        Chunk(o, "IEND", ReadOnlySpan<byte>.Empty);
        return o.ToArray();
    }

    /// <summary>256x240 NES colour indices -> RGB bytes (palettes.json nesRgb, 0xRRGGBB), nearest-neighbour scale</summary>
    public static (int W, int H, byte[] Rgb) FbToRgb(byte[] fb, int[] nesRgb, int scale = 1)
    {
        int W = 256 * scale, H = 240 * scale;
        var @out = new byte[W * H * 3];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int c = nesRgb[fb[(y / scale) * 256 + (x / scale)] & 0x3F], o = (y * W + x) * 3;
                @out[o] = (byte)(c >> 16); @out[o + 1] = (byte)(c >> 8); @out[o + 2] = (byte)c;
            }
        return (W, H, @out);
    }

    /// <summary>Write the framebuffer as a PNG file (run.mjs --dump-screen)</summary>
    public static void WriteFb(string path, byte[] fb, int[] nesRgb, int scale = 1)
    {
        var (w, h, rgb) = FbToRgb(fb, nesRgb, scale);
        File.WriteAllBytes(path, Encode(w, h, rgb));
    }
}
