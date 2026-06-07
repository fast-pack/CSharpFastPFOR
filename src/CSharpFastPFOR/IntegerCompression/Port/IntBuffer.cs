using System.Text;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Port;

public class IntBuffer(MemoryStream ms, ByteOrder order)
{
    private long _position = ms.Position;

    public int get()
    {
        long parentPosition = ms.Position;
        ms.Position = _position;
        using BinaryReader br = new BinaryReader(ms, Encoding.UTF8, true);
        int value = br.ReadInt32();
        _position = ms.Position;
        ms.Position = parentPosition;

        if (order == ByteOrder.BIG_ENDIAN)
            value = reverseBytes(value);

        return value;
    }

    public void get(int[] dst, int offset, int length)
    {
        int end = offset + length;

        long parentPosition = ms.Position;
        ms.Position = _position;
        using BinaryReader br = new BinaryReader(ms, Encoding.UTF8, true);

        for (int i = offset; i < end; i++)
        {
            int value = br.ReadInt32();

            if (order == ByteOrder.BIG_ENDIAN)
                value = reverseBytes(value);

            dst[i] = value;
        }

        _position = ms.Position;
        ms.Position = parentPosition;
    }

    public void put(int[] src, int offset, int length)
    {
        int end = offset + length;

        long parentPosition = ms.Position;
        ms.Position = _position;
        using BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8, true);

        for (int i = offset; i < end; i++)
        {
            int value = src[i];

            if (order == ByteOrder.BIG_ENDIAN)
                value = reverseBytes(value);

            bw.Write(value);
        }

        _position = ms.Position;
        ms.Position = parentPosition;
    }

    public static int reverseBytes(int i) => (i >>> 24) |
                                             ((i >> 8) & 0xFF00) |
                                             ((i << 8) & 0xFF0000) |
                                             (i << 24);

}