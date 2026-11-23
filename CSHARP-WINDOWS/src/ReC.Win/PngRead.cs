// re-c Windows host — PNG decode (graphics pack tiles, T10) and RGBA encode (--export-tiles). Stdlib only
// (System.IO.Compression.ZLibStream). Decodes 8-bit non-interlaced greyscale (+alpha), RGB(A) and palette (+tRNS).
using System.Buffers.Binary;
using System.IO.Compression;
using ReC.Util;

namespace ReC.Win;

internal static class PngRead
{
    private static readonly byte[] Sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>PNG bytes -> (w, h, pixels 0xAARRGGBB); InvalidDataException on anything unsupported</summary>
    public static (int W, int H, uint[] Px) Decode(byte[] data)
    {
        if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(Sig)) throw new InvalidDataException("not a PNG");
        int pos = 8, w = 0, h = 0, type = -1;
        byte[]? plte = null, trns = null;
        var idat = new MemoryStream();
        while (pos + 8 <= data.Length)
        {
            int n = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos));
            string tag = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            if (n < 0 || pos + 12 + n > data.Length) throw new InvalidDataException("truncated chunk");
            var body = data.AsSpan(pos + 8, n);
            switch (tag)
            {
                case "IHDR":
                    w = (int)BinaryPrimitives.ReadUInt32BigEndian(body);
                    h = (int)BinaryPrimitives.ReadUInt32BigEndian(body[4..]);
                    if (body[8] != 8 || body[12] != 0) throw new InvalidDataException("only 8-bit non-interlaced PNGs");
                    type = body[9];
                    if (type is not (0 or 2 or 3 or 4 or 6)) throw new InvalidDataException($"colour type {type}");
                    if (w <= 0 || h <= 0 || w > 4096 || h > 4096) throw new InvalidDataException("bad size");
                    break;
                case "PLTE": plte = body.ToArray(); break;
                case "tRNS": trns = body.ToArray(); break;
                case "IDAT": idat.Write(body); break;
            }
            pos += 12 + n;
            if (tag == "IEND") break;
        }
        if (type < 0) throw new InvalidDataException("no IHDR");
        if (type == 3 && plte == null) throw new InvalidDataException("palette PNG without PLTE");
        int bpp = type switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, _ => 4 };
        int stride = w * bpp;
        var raw = new byte[(stride + 1) * h];
        idat.Position = 0;
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
        {
            int got = 0, k;
            while (got < raw.Length && (k = z.Read(raw, got, raw.Length - got)) > 0) got += k;
            if (got < raw.Length) throw new InvalidDataException("short image data");
        }
        var px = new uint[w * h];
        var prev = new byte[stride];
        var line = new byte[stride];
        for (int y = 0; y < h; y++)
        {
            int f = raw[y * (stride + 1)];
            Buffer.BlockCopy(raw, y * (stride + 1) + 1, line, 0, stride);
            for (int i = 0; i < stride; i++)
            {
                int a = i >= bpp ? line[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                int v = f switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) >> 1,
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException($"filter {f}"),
                };
                line[i] = (byte)(line[i] + v);
            }
            for (int x = 0; x < w; x++)
            {
                int o = x * bpp;
                uint r, g, bl, al = 255;
                switch (type)
                {
                    case 0: r = g = bl = line[o]; if (trns is { Length: >= 2 } && line[o] == trns[1]) al = 0; break;
                    case 4: r = g = bl = line[o]; al = line[o + 1]; break;
                    case 2:
                        r = line[o]; g = line[o + 1]; bl = line[o + 2];
                        if (trns is { Length: >= 6 } && r == trns[1] && g == trns[3] && bl == trns[5]) al = 0;
                        break;
                    case 3:
                        int ix = line[o];
                        if (ix * 3 + 2 >= plte!.Length) throw new InvalidDataException("palette index out of range");
                        r = plte[ix * 3]; g = plte[ix * 3 + 1]; bl = plte[ix * 3 + 2];
                        if (trns != null && ix < trns.Length) al = trns[ix];
                        break;
                    default: r = line[o]; g = line[o + 1]; bl = line[o + 2]; al = line[o + 3]; break;
                }
                px[y * w + x] = al << 24 | r << 16 | g << 8 | bl;
            }
            (prev, line) = (line, prev);
        }
        return (w, h, px);
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    /// <summary>rgba: w*h*4 bytes -> PNG (colour type 6, filter 0)</summary>
    public static byte[] EncodeRgba(int w, int h, byte[] rgba)
    {
        var raw = new byte[(w * 4 + 1) * h];
        for (int y = 0; y < h; y++) Buffer.BlockCopy(rgba, y * w * 4, raw, y * (w * 4 + 1) + 1, w * 4);
        var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.SmallestSize, leaveOpen: true)) zs.Write(raw);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0), (uint)w);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)h);
        ihdr[8] = 8; ihdr[9] = 6;
        var o = new MemoryStream();
        o.Write(Sig);
        Chunk(o, "IHDR", ihdr);
        Chunk(o, "IDAT", z.ToArray());
        Chunk(o, "IEND", ReadOnlySpan<byte>.Empty);
        return o.ToArray();
    }

    private static void Chunk(Stream s, string tag, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        s.Write(len);
        var td = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) td[i] = (byte)tag[i];
        data.CopyTo(td.AsSpan(4));
        s.Write(td);
        BinaryPrimitives.WriteUInt32BigEndian(len, Crc32.Compute(td));
        s.Write(len);
    }
}
