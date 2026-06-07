/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression;

/*
 * Edge-cases to be tested on a per-codec basis
 *
 * @author Benoit Lacelle
 */
public abstract class ATestLongCODEC
{
    private void checkConsistency(LongCODEC codec, long[] array)
    {
        {
            long[] compressed = LongTestUtils.compress(codec, array);
            long[] uncompressed = LongTestUtils.uncompress(codec, compressed, array.Length);

            Assert2.assertArrayEquals(array, uncompressed);
        }

        if (codec is ByteLongCODEC byteLongCodec)
        {
            byte[] compressed = LongTestUtils.compress(byteLongCodec, array);
            long[] uncompressed = LongTestUtils.uncompress(byteLongCodec, compressed, array.Length);

            Assert2.assertArrayEquals(array, uncompressed);
        }

        if (codec is SkippableLongCODEC skippableLongCodec)
        {
            long[] compressed = LongTestUtils.compressHeadless(skippableLongCodec, array);
            long[] uncompressed = LongTestUtils.uncompressHeadless(skippableLongCodec, compressed, array.Length);

            Assert2.assertArrayEquals(array, uncompressed);
        }
    }

    protected abstract LongCODEC getCodec();

    [Fact]
    public void testCodec_Minus1()
    {
        checkConsistency(getCodec(), [-1]);
    }

    [Fact]
    public void testCodec_ZeroTimes8Minus1()
    {
        checkConsistency(getCodec(), [0, 0, 0, 0, 0, 0, 0, 0, -1]);
    }

    [Fact]
    public void testCodec_ZeroTimes127Minus1()
    {
        long[] array = new long[128];
        array[127] = -1;

        checkConsistency(getCodec(), array);
    }

    [Fact]
    public void testCodec_ZeroTimes128Minus1()
    {
        long[] array = new long[129];
        array[128] = -1;

        checkConsistency(getCodec(), array);
    }

    [Fact]
    public void testCodec_MinValue()
    {
        checkConsistency(getCodec(), [long.MinValue]);
    }

    [Fact]
    public void testCodec_ZeroMinValue()
    {
        checkConsistency(getCodec(), [0, long.MinValue]);
    }

    [Fact]
    public void testCodec_allPowerOfTwo()
    {
        checkConsistency(getCodec(), [1L << 42]);
        for (int i = 0; i < 64; i++)
            checkConsistency(getCodec(), [1L << i]);
    }

    [Fact]
    public void testCodec_ZeroThenAllPowerOfTwo()
    {
        for (int i = 0; i < 64; i++)
            checkConsistency(getCodec(), [0, 1L << i]);
    }
}