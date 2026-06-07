using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.Tests.LongCompression;

namespace Genbox.CSharpFastPFOR.Tests;

/*
 * Edge-cases having caused issue specifically with LongBinaryPacking.
 *
 * @author Benoit Lacelle
 */
public class TestLongBinaryPacking
{
    private const string SkipReason = "Parent class tests are not valid as LongBinaryPacking process by chunks of 64 longs";

    private readonly LongBinaryPacking _codec = new LongBinaryPacking();

    private LongBinaryPacking getCodec() => _codec;

    [Fact(Skip = SkipReason)]
    public void testCodec_Minus1()
    {
        checkConsistency(getCodec(), [-1]);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_ZeroTimes8Minus1()
    {
        checkConsistency(getCodec(), [0, 0, 0, 0, 0, 0, 0, 0, -1]);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_ZeroTimes127Minus1()
    {
        long[] array = ZeroRunThen(-1, 127);

        checkConsistency(getCodec(), array);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_ZeroTimes128Minus1()
    {
        long[] array = ZeroRunThen(-1, 128);

        checkConsistency(getCodec(), array);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_MinValue()
    {
        checkConsistency(getCodec(), [long.MinValue]);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_ZeroMinValue()
    {
        checkConsistency(getCodec(), [0, long.MinValue]);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_allPowerOfTwo()
    {
        checkConsistency(getCodec(), [1L << 42]);
        for (int i = 0; i < 64; i++)
            checkConsistency(getCodec(), [1L << i]);
    }

    [Fact(Skip = SkipReason)]
    public void testCodec_ZeroThenAllPowerOfTwo()
    {
        for (int i = 0; i < 64; i++)
            checkConsistency(getCodec(), [0, 1L << i]);
    }

    private static void checkConsistency(LongCODEC codec, long[] array)
    {
        long[] compressed = LongTestUtils.compress(codec, array);
        long[] uncompressed = LongTestUtils.uncompress(codec, compressed, array.Length);

        Assert.Equal(array, uncompressed);

        if (codec is ByteLongCODEC byteLongCodec)
        {
            byte[] byteCompressed = LongTestUtils.compress(byteLongCodec, array);
            long[] byteUncompressed = LongTestUtils.uncompress(byteLongCodec, byteCompressed, array.Length);

            Assert.Equal(array, byteUncompressed);
        }

        if (codec is SkippableLongCODEC skippableLongCodec)
        {
            long[] headlessCompressed = LongTestUtils.compressHeadless(skippableLongCodec, array);
            long[] headlessUncompressed = LongTestUtils.uncompressHeadless(skippableLongCodec, headlessCompressed, array.Length);

            Assert.Equal(array, headlessUncompressed);
        }
    }

    private static long[] ZeroRunThen(long value, int zeroRunLength)
    {
        long[] data = new long[zeroRunLength + 1];
        data[zeroRunLength] = value;
        return data;
    }
}