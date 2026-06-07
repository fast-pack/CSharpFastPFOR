using System.Text;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Port;

public class LongBuffer(MemoryStream ms, ByteOrder order)
{
    private long _position = ms.Position;

    public void get(long[] dst, int offset, int length)
    {
        int end = offset + length;

        long parentPosition = ms.Position;
        ms.Position = _position;
        using BinaryReader br = new BinaryReader(ms, Encoding.UTF8, true);

        for (int i = offset; i < end; i++)
        {
            long value = br.ReadInt64();

            if (order == ByteOrder.BIG_ENDIAN)
                value = reverseBytes(value);

            dst[i] = value;
        }

        _position = ms.Position;
        ms.Position = parentPosition;
    }

    public static long reverseBytes(long i) => ((long)IntBuffer.reverseBytes((int)i) << 32) |
                                               (uint)IntBuffer.reverseBytes((int)(i >> 32));
}