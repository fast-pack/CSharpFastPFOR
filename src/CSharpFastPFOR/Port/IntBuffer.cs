using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Genbox.CSharpFastPFOR.Port;

public class IntBuffer(MemoryStream ms, ByteOrder order)
{
    public void get(int[] dst, int offset, int length)
    {
        int end = offset + length;

        using BinaryReader br = new BinaryReader(ms, Encoding.UTF8, true);

        for (int i = offset; i < end; i++)
        {
            int value = br.ReadInt32();

            if (BitConverter.IsLittleEndian && order == ByteOrder.BIG_ENDIAN)
                value = reverseBytes(value);

            dst[i] = value;
        }
    }

    public static int reverseBytes(int i)
    {
        return (i >>> 24) |
               ((i >> 8) & 0xFF00) |
               ((i << 8) & 0xFF0000) |
               ((i << 24));
    }

    private static void checkBounds(int off, int len, int size)
    {
        if ((off | len | (off + len) | (size - (off + len))) < 0)
            throw new IndexOutOfRangeException();
    }
}