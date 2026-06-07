/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.LongCompression;
using System.Globalization;

namespace Genbox.CSharpFastPFOR.Benchmarks;

internal static class BenchmarkLongBitPacking
{
    private const long RandomMultiplier = 0x5DEECE66DL;
    private const long RandomAddend = 0xBL;
    private const long RandomMask = (1L << 48) - 1;

    private static void test(bool verbose)
    {
        const int N = 64;
        const int times = 100000;
        long randomSeed = RandomMultiplier;
        long[] data = new long[N];
        long[] compressed = new long[N];
        long[] uncompressed = new long[N];
        for (int bit = 1; bit < 64; ++bit)
        {
            long mask = (1L << bit) - 1;
            long slowcomp = 0;
            long slowdecomp = 0;
            long fastcomp = 0;
            long fastdecomp = 0;
            for (int t = 0; t < times; ++t)
            {
                for (int k = 0; k < N; ++k)
                    data[k] = nextLong(ref randomSeed) & mask;

                long time1 = Port.System.nanoTime();
                slowpackwithoutmask(data, 0, compressed, 0, bit);
                long time2 = Port.System.nanoTime();
                slowunpack(compressed, 0, uncompressed, 0, bit);
                long time3 = Port.System.nanoTime();
                LongBitPacking.fastpackwithoutmask(data, 0, compressed, 0, bit);
                long time4 = Port.System.nanoTime();
                LongBitPacking.fastunpack(compressed, 0, uncompressed, 0, bit);
                long time5 = Port.System.nanoTime();
                slowcomp += time2 - time1;
                slowdecomp += time3 - time2;
                fastcomp += time4 - time3;
                fastdecomp += time5 - time4;
            }
            if (verbose)
            {
                Console.WriteLine("bit = "
                                  + bit
                                  + " slow comp. speed = "
                                  + ((N * times * 1000.0) / slowcomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " slow decomp. speed = "
                                  + ((N * times * 1000.0) / slowdecomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " unrolled comp. speed = "
                                  + ((N * times * 1000.0) / fastcomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " unrolled decomp. speed = "
                                  + ((N * times * 1000.0) / fastdecomp).ToString("0", CultureInfo.InvariantCulture));
            }
        }
    }

    public static void main(string[] args)
    {
        _ = args;
        Console.WriteLine("Testing long packing (slow vs unrolled) ");
        test(false);
        test(true);
    }

    private static long nextLong(ref long seed) => ((long)nextBits(ref seed, 32) << 32) + nextBits(ref seed, 32);

    private static int nextBits(ref long seed, int bits)
    {
        seed = ((seed * RandomMultiplier) + RandomAddend) & RandomMask;
        return (int)(seed >>> (48 - bits));
    }

    private static void slowpackwithoutmask(long[] @in, int inpos, long[] @out, int outpos, int bit)
    {
        int bucket = 0;
        int shift = 0;
        @out[outpos + bucket] = 0L;
        for (int i = 0; i < 64; i++)
        {
            if (shift >= 64)
            {
                bucket++;
                @out[bucket + outpos] = 0L;
                shift -= 64;
                if (shift > 0)
                    @out[outpos + bucket] |= @in[inpos + i - 1] >> (bit - shift);
            }
            @out[outpos + bucket] |= @in[inpos + i] << shift;
            shift += bit;
        }
    }

    private static void slowunpack(long[] @in, int inpos, long[] @out, int outpos, int bit)
    {
        int bucket = 0;
        int shift = 0;
        for (int i = 0; i < 64; i++)
        {
            if (shift >= 64)
            {
                bucket++;
                shift -= 64;
                if (shift > 0)
                    @out[outpos + i - 1] |= (@in[inpos + bucket] << (bit - shift)) & ((1L << bit) - 1);
            }
            @out[outpos + i] = ((long)((ulong)@in[inpos + bucket] >> shift)) & ((1L << bit) - 1);
            shift += bit;
        }
    }
}