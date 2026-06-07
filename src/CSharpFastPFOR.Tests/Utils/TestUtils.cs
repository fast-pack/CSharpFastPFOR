/*
 * Static utility methods for test.
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests.Utils;

public class TestUtils
{
    [Fact]
    public void testPacking()
    {
        int[] outputarray = new int[32];
        for (int b = 1; b < 32; ++b)
        {
            int[] data = new int[32];
            int[] newdata = new int[32];
            int mask = (1 << b) - 1;
            for (int j = 0; j < data.Length; ++j)
                data[j] = mask - (j % mask);

            for (int n = 0; n <= 32; ++n)
            {
                Array.Fill(outputarray, 0);
                int howmany = Util.pack(outputarray, 0, data, 0, n, b);
                if (howmany != Util.packsize(n, b)) throw new InvalidOperationException("bug " + n + " " + b);
                Util.unpack(Arrays.copyOf(outputarray, howmany), 0, newdata, 0, n, b);
                for (int i = 0; i < n; ++i)
                {
                    if (newdata[i] != data[i])
                        throw new InvalidOperationException("bug " + b + " " + n);
                }
            }
        }
    }

    [Fact]
    public void testPackingw()
    {
        int[] outputarray = new int[32];
        for (int b = 1; b < 32; ++b)
        {
            int[] data = new int[32];
            int[] newdata = new int[32];
            int mask = (1 << b) - 1;
            for (int j = 0; j < data.Length; ++j)
                data[j] = mask - (j % mask);

            for (int n = 0; n <= 32; ++n)
            {
                Array.Fill(outputarray, 0);
                int howmany = Util.packw(outputarray, 0, data, n, b);
                if (howmany != Util.packsizew(n, b)) throw new InvalidOperationException("bug " + n + " " + b);
                Util.unpackw(Arrays.copyOf(outputarray, howmany), 0, newdata, n, b);
                for (int i = 0; i < n; ++i)
                {
                    if (newdata[i] != data[i])
                        throw new InvalidOperationException("bug " + b + " " + n);
                }
            }
        }
    }

    /*
     * Check that compress and uncompress keep original array.
     *
     * @param codec CODEC to test.
     * @param orig  original integers
     */
    internal static void assertSymmetry(IntegerCODEC codec, params int[] orig)
    {
        // There are some cases that compressed array is bigger than original
        // array.  So output array for compress must be larger.
        //
        // Example:
        //  - VariableByte compresses an array like [ -1 ].
        //  - Composition compresses a short array.

        const int EXTEND = 1;

        int[] compressed = new int[orig.Length + EXTEND];
        IntWrapper c_inpos = new IntWrapper(0);
        IntWrapper c_outpos = new IntWrapper(0);
        codec.compress(orig, c_inpos, orig.Length, compressed,
            c_outpos);

        Assert2.assertTrue(c_outpos.get() <= orig.Length + EXTEND);

        // Uncompress an array.
        int[] uncompressed = new int[orig.Length];
        IntWrapper u_inpos = new IntWrapper(0);
        IntWrapper u_outpos = new IntWrapper(0);
        codec.uncompress(compressed, u_inpos, c_outpos.get(),
            uncompressed, u_outpos);

        // Compare between uncompressed and orig arrays.
        int[] target = Arrays.copyOf(uncompressed, u_outpos.get());
        Assert2.assertArrayEquals(orig, target);
    }

    public static int[] compress(IntegerCODEC codec, int[] data)
    {
        int[] outBuf = new int[data.Length * 4];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.compress(data, inPos, data.Length, outBuf, outPos);
        return Arrays.copyOf(outBuf, outPos.get());
    }

    public static int[] uncompress(IntegerCODEC codec, int[] data, int len)
    {
        int[] outBuf = new int[len + 1024];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.uncompress(data, inPos, data.Length, outBuf, outPos);
        return Arrays.copyOf(outBuf, outPos.get());
    }

    public static sbyte[] compress(ByteIntegerCODEC codec, int[] data)
    {
        sbyte[] outBuf = new sbyte[data.Length * 4 * 4];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.compress(data, inPos, data.Length, outBuf, outPos);
        return Arrays.copyOf(outBuf, outPos.get());
    }

    public static int[] uncompress(ByteIntegerCODEC codec, sbyte[] data, int len)
    {
        int[] outBuf = new int[len + 1024];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.uncompress(data, inPos, data.Length, outBuf, outPos);
        return Arrays.copyOf(outBuf, outPos.get());
    }

    public static int[] compressHeadless(SkippableIntegerCODEC codec, int[] data)
    {
        int[] outBuf = new int[codec.maxHeadlessCompressedLength(new IntWrapper(), data.Length)];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.headlessCompress(data, inPos, data.Length, outBuf, outPos);
        return Arrays.copyOf(outBuf, outPos.get());
    }

    public static int[] uncompressHeadless(SkippableIntegerCODEC codec, int[] data, int len)
    {
        int[] outBuf = new int[len + 1024];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.headlessUncompress(data, inPos, data.Length, outBuf, outPos, len);
        if (outPos.get() < len) throw new InvalidOperationException("Insufficient output.");
        return Arrays.copyOf(outBuf, outPos.get());
    }
}