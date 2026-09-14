using System.IO.Compression;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// PURE decoder for the elevation tiles VR-TheWorld serves. No I/O, no MAK, no bridge:
/// bytes in, pixels out. The python pre-flight (tools/preflight/leg_check.py) leans on
/// Pillow; this port cannot, so the one shape the server actually streams is decoded here
/// and nothing else is claimed to work.
///
/// Dataset 149 level 13: a 257x257 single-band float32 GeoTIFF, little-endian,
/// Compression 8 (Adobe Deflate = a zlib stream), 37 strips of RowsPerStrip 7, Predictor 3
/// (the FLOATING-POINT predictor), SampleFormat 3. Verified value-for-value against Pillow
/// on the cached tiles (2026-09-14) before a line of this was written.
/// </summary>
public static class GeoTiffFloat
{
    /// <summary>One decoded single-band float raster, row 0 = the image's NORTH edge.</summary>
    public sealed record Raster(int Width, int Height, float[] Pixels)
    {
        public float At(int col, int row) => Pixels[row * Width + col];
    }

    /// <summary>
    /// Decode a single-band 32-bit float TIFF. Returns null - never throws - when the bytes
    /// are not a TIFF this decoder handles; the caller treats that exactly like a missing
    /// tile (NaN samples, and NO VERDICT once too many of them land on one leg).
    /// </summary>
    public static Raster Decode(byte[] d)
    {
        try { return DecodeCore(d); }
        catch { return null; }
    }

    private static Raster DecodeCore(byte[] d)
    {
        if (d == null || d.Length < 8) return null;
        bool le;
        if (d[0] == 0x49 && d[1] == 0x49) le = true;
        else if (d[0] == 0x4D && d[1] == 0x4D) le = false;
        else return null;
        if (U16(d, 2, le) != 42) return null;

        uint ifd = U32(d, 4, le);
        if (ifd + 2 > d.Length) return null;
        int n = U16(d, (int)ifd, le);

        int width = 0, height = 0, bits = 0, compression = 1, predictor = 1;
        int samplesPerPixel = 1, sampleFormat = 1, rowsPerStrip = int.MaxValue;
        uint[] stripOffsets = null, stripCounts = null;

        for (int i = 0; i < n; i++)
        {
            int o = (int)ifd + 2 + 12 * i;
            if (o + 12 > d.Length) return null;
            int tag = U16(d, o, le);
            int type = U16(d, o + 2, le);
            uint count = U32(d, o + 4, le);
            switch (tag)
            {
                case 256: width = (int)ScalarOf(d, o, type, le); break;
                case 257: height = (int)ScalarOf(d, o, type, le); break;
                case 258: bits = (int)ScalarOf(d, o, type, le); break;
                case 259: compression = (int)ScalarOf(d, o, type, le); break;
                case 273: stripOffsets = ArrayOf(d, o, type, count, le); break;
                case 277: samplesPerPixel = (int)ScalarOf(d, o, type, le); break;
                case 278: rowsPerStrip = (int)ScalarOf(d, o, type, le); break;
                case 279: stripCounts = ArrayOf(d, o, type, count, le); break;
                case 317: predictor = (int)ScalarOf(d, o, type, le); break;
                case 339: sampleFormat = (int)ScalarOf(d, o, type, le); break;
            }
        }

        // Only the shape the server actually serves is accepted. Anything else is "no tile"
        // rather than a silently wrong elevation.
        if (width <= 0 || height <= 0 || bits != 32 || samplesPerPixel != 1 || sampleFormat != 3)
            return null;
        if (stripOffsets == null || stripCounts == null || stripOffsets.Length != stripCounts.Length)
            return null;
        if (compression != 1 && compression != 8 && compression != 32946) return null;
        if (predictor != 1 && predictor != 3) return null;
        if (rowsPerStrip <= 0) return null;

        int rowBytes = width * 4;
        var outBytes = new byte[checked(rowBytes * height)];
        int written = 0;
        for (int s = 0; s < stripOffsets.Length && written < outBytes.Length; s++)
        {
            int off = (int)stripOffsets[s], cnt = (int)stripCounts[s];
            if (off < 0 || cnt < 0 || off + cnt > d.Length) return null;
            byte[] raw = compression == 1 ? Slice(d, off, cnt) : Inflate(d, off, cnt);
            if (raw == null) return null;
            int take = Math.Min(raw.Length, outBytes.Length - written);
            Buffer.BlockCopy(raw, 0, outBytes, written, take);
            written += take;
        }
        if (written < outBytes.Length) return null;

        if (predictor == 3) UndoFloatPredictor(outBytes, width, height, rowBytes);

        var px = new float[width * height];
        if (le && BitConverter.IsLittleEndian)
            Buffer.BlockCopy(outBytes, 0, px, 0, outBytes.Length);
        else
            for (int i = 0; i < px.Length; i++)
                px[i] = BitConverter.ToSingle(Ordered(outBytes, i * 4, le), 0);
        return new Raster(width, height, px);
    }

    /// <summary>
    /// TIFF Predictor 3, per libtiff's fpAcc, applied ONE SCANLINE AT A TIME
    /// (PredictorDecodeTile loops over rows of TIFFScanlineSize bytes):
    ///   1. horizontal byte differencing is undone across the whole row (stride = 1 here,
    ///      one sample per pixel);
    ///   2. the row is stored as BYTE PLANES - every sample's byte 0, then every sample's
    ///      byte 1, ... - so it is de-shuffled back into per-sample order, most significant
    ///      byte first, which on a little-endian host means writing it in reverse.
    /// </summary>
    private static void UndoFloatPredictor(byte[] buf, int width, int height, int rowBytes)
    {
        const int bps = 4;
        var tmp = new byte[rowBytes];
        for (int r = 0; r < height; r++)
        {
            int s = r * rowBytes;
            for (int i = 1; i < rowBytes; i++)
                buf[s + i] = (byte)(buf[s + i] + buf[s + i - 1]);
            Buffer.BlockCopy(buf, s, tmp, 0, rowBytes);
            for (int c = 0; c < width; c++)
                for (int b = 0; b < bps; b++)
                    buf[s + bps * c + (bps - b - 1)] = tmp[b * width + c];
        }
    }

    private static byte[] Inflate(byte[] d, int off, int cnt)
    {
        try
        {
            using var src = new MemoryStream(d, off, cnt, writable: false);
            using var z = new ZLibStream(src, CompressionMode.Decompress);
            using var dst = new MemoryStream();
            z.CopyTo(dst);
            return dst.ToArray();
        }
        catch { return null; }
    }

    private static byte[] Slice(byte[] d, int off, int cnt)
    {
        var b = new byte[cnt];
        Buffer.BlockCopy(d, off, b, 0, cnt);
        return b;
    }

    private static byte[] Ordered(byte[] d, int off, bool le)
    {
        var b = new byte[4];
        for (int i = 0; i < 4; i++) b[i] = d[off + (le ? i : 3 - i)];
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static uint ScalarOf(byte[] d, int entry, int type, bool le)
        => type == 3 ? U16(d, entry + 8, le) : U32(d, entry + 8, le);

    private static uint[] ArrayOf(byte[] d, int entry, int type, uint count, bool le)
    {
        int size = type == 3 ? 2 : 4;
        var v = new uint[count];
        if (count * size <= 4)
        {
            for (int i = 0; i < count; i++)
                v[i] = size == 2 ? U16(d, entry + 8 + 2 * i, le) : U32(d, entry + 8 + 4 * i, le);
            return v;
        }
        int at = (int)U32(d, entry + 8, le);
        for (int i = 0; i < count; i++)
        {
            int o = at + i * size;
            if (o + size > d.Length) return null;
            v[i] = size == 2 ? U16(d, o, le) : U32(d, o, le);
        }
        return v;
    }

    private static ushort U16(byte[] d, int o, bool le)
        => (ushort)(le ? d[o] | (d[o + 1] << 8) : (d[o] << 8) | d[o + 1]);

    private static uint U32(byte[] d, int o, bool le)
        => le ? (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24))
              : (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);
}

/// <summary>
/// PURE decoder for the land-cover tiles (datasets 154/165/188): 256x256, bit depth 8,
/// colour type 6 (RGBA), no interlace, one IDAT. Pillow opens these as mode "RGBA", so
/// leg_check.py's `v[0] if isinstance(v, tuple) else v` takes the RED channel - that red
/// byte IS the land-cover class value the osgEarth catalogues are keyed on, and this
/// decoder returns exactly that byte.
///
/// Colour types 0 (grey), 2 (RGB), 3 (palette) and 4 (grey+alpha) are decoded too, and each
/// returns what Pillow's getpixel would hand leg_check.py for that mode: the grey level, the
/// red channel, or - for a palette image, which Pillow opens as mode "P" - the palette INDEX.
/// Anything else (bit depth != 8, interlaced) returns null = "no tile here".
/// </summary>
public static class PngImage
{
    /// <summary>One decoded 8-bit image; Value(x, y) is what Pillow's getpixel returns.</summary>
    public sealed record Image(int Width, int Height, int Channels, byte[] Pixels, bool Palette)
    {
        public int Value(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return -1;
            return Pixels[(y * Width + x) * Channels];
        }
    }

    private static readonly byte[] Magic = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>Decode; null (never a throw) when these bytes are not a PNG we handle.</summary>
    public static Image Decode(byte[] d)
    {
        try { return DecodeCore(d); }
        catch { return null; }
    }

    private static Image DecodeCore(byte[] d)
    {
        if (d == null || d.Length < 8 + 25) return null;
        for (int i = 0; i < 8; i++) if (d[i] != Magic[i]) return null;

        int width = 0, height = 0, bitDepth = 0, colourType = 0, interlace = 0;
        var idat = new MemoryStream();
        int o = 8;
        while (o + 8 <= d.Length)
        {
            int len = (int)BE32(d, o);
            if (len < 0 || o + 12 + len > d.Length) break;
            string type = System.Text.Encoding.ASCII.GetString(d, o + 4, 4);
            int body = o + 8;
            if (type == "IHDR" && len >= 13)
            {
                width = (int)BE32(d, body);
                height = (int)BE32(d, body + 4);
                bitDepth = d[body + 8];
                colourType = d[body + 9];
                interlace = d[body + 12];
            }
            else if (type == "IDAT") idat.Write(d, body, len);
            else if (type == "IEND") break;
            o = body + len + 4;
        }
        if (width <= 0 || height <= 0 || bitDepth != 8 || interlace != 0) return null;
        int channels = colourType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => 0 };
        if (channels == 0) return null;

        byte[] raw;
        try
        {
            idat.Position = 0;
            using var z = new ZLibStream(idat, CompressionMode.Decompress);
            using var dst = new MemoryStream();
            z.CopyTo(dst);
            raw = dst.ToArray();
        }
        catch { return null; }

        int stride = width * channels;
        if (raw.Length < (stride + 1) * height) return null;

        var outPx = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            int filter = raw[y * (stride + 1)];
            int src = y * (stride + 1) + 1;
            int dstRow = y * stride;
            int prevRow = dstRow - stride;
            for (int x = 0; x < stride; x++)
            {
                int a = x >= channels ? outPx[dstRow + x - channels] : 0;
                int b = y > 0 ? outPx[prevRow + x] : 0;
                int c = (x >= channels && y > 0) ? outPx[prevRow + x - channels] : 0;
                int v = raw[src + x];
                outPx[dstRow + x] = (byte)(filter switch
                {
                    0 => v,
                    1 => v + a,
                    2 => v + b,
                    3 => v + ((a + b) >> 1),
                    4 => v + Paeth(a, b, c),
                    _ => v,
                });
            }
        }
        return new Image(width, height, channels, outPx, colourType == 3);
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
    }

    private static uint BE32(byte[] d, int o)
        => (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);
}
