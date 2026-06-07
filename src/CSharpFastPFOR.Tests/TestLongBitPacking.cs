using Genbox.CSharpFastPFOR.LongCompression;

namespace Genbox.CSharpFastPFOR.Tests;

public class TestLongBitPacking
{
    private const int Block = 64;
    private const int Trials = 64;

    private static long[] randomBlock(Random random, int bit)
    {
        long mask = bit == 64 ? -1L : (1L << bit) - 1;
        long[] data = new long[Block];
        byte[] bytes = new byte[sizeof(long)];
        for (int i = 0; i < Block; i++)
        {
            random.NextBytes(bytes);
            data[i] = BitConverter.ToInt64(bytes, 0) & mask;
        }
        return data;
    }

    [Fact]
    public void roundTrip()
    {
        Random random = new Random(0);
        for (int bit = 0; bit <= 64; bit++)
        for (int trial = 0; trial < Trials; trial++)
        {
            long[] data = randomBlock(random, bit);
            long[] packed = new long[bit];
            LongBitPacking.fastpackwithoutmask(data, 0, packed, 0, bit);
            long[] unpacked = new long[Block];
            LongBitPacking.fastunpack(packed, 0, unpacked, 0, bit);
            Assert.Equal(data, unpacked);
        }
    }

    [Fact]
    public void layoutMatchesSlowPack()
    {
        Random random = new Random(1);
        for (int bit = 1; bit < 64; bit++)
        for (int trial = 0; trial < Trials; trial++)
        {
            long[] data = randomBlock(random, bit);
            long[] fast = new long[bit];
            long[] slow = new long[bit];
            LongBitPacking.fastpackwithoutmask(data, 0, fast, 0, bit);
            LongBitPacking.slowpackwithoutmask(data, 0, slow, 0, bit);
            Assert.Equal(slow, fast);
        }
    }

    [Fact]
    public void layoutMatchesSlowUnpack()
    {
        Random random = new Random(2);
        for (int bit = 1; bit < 64; bit++)
        for (int trial = 0; trial < Trials; trial++)
        {
            long[] data = randomBlock(random, bit);
            long[] packed = new long[bit];
            LongBitPacking.slowpackwithoutmask(data, 0, packed, 0, bit);
            long[] fast = new long[Block];
            long[] slow = new long[Block];
            LongBitPacking.fastunpack(packed, 0, fast, 0, bit);
            LongBitPacking.slowunpack(packed, 0, slow, 0, bit);
            Assert.Equal(slow, fast);
        }
    }

    [Fact]
    public void extremeValues()
    {
        for (int bit = 0; bit <= 64; bit++)
        {
            long mask = bit == 64 ? -1L : (1L << bit) - 1;
            long[] data = new long[Block];
            Array.Fill(data, mask);
            long[] packed = new long[bit];
            LongBitPacking.fastpackwithoutmask(data, 0, packed, 0, bit);
            long[] unpacked = new long[Block];
            LongBitPacking.fastunpack(packed, 0, unpacked, 0, bit);
            Assert.Equal(data, unpacked);
            if (bit > 0 && bit < 64)
            {
                long[] slow = new long[bit];
                LongBitPacking.slowpackwithoutmask(data, 0, slow, 0, bit);
                Assert.Equal(slow, packed);
            }
        }
    }

    [Fact]
    public void roundTripWithOffsets()
    {
        Random random = new Random(3);
        const int inOffset = 7;
        const int outOffset = 5;
        const long sentinel = 0x5555555555555555L;
        for (int bit = 0; bit <= 64; bit++)
        {
            long[] data = randomBlock(random, bit);
            long[] input = new long[Block + inOffset];
            Array.Copy(data, 0, input, inOffset, Block);
            long[] packed = new long[bit + outOffset];
            Array.Fill(packed, sentinel, 0, outOffset);
            LongBitPacking.fastpackwithoutmask(input, inOffset, packed, outOffset, bit);
            long[] unpacked = new long[Block + inOffset];
            Array.Fill(unpacked, sentinel, 0, inOffset);
            LongBitPacking.fastunpack(packed, outOffset, unpacked, inOffset, bit);
            long[] result = new long[Block];
            Array.Copy(unpacked, inOffset, result, 0, Block);
            Assert.Equal(data, result);
            for (int i = 0; i < outOffset; i++)
                Assert.Equal(sentinel, packed[i]);
            for (int i = 0; i < inOffset; i++)
                Assert.Equal(sentinel, unpacked[i]);
        }
    }
}