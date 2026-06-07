/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.IntegerCompression.Vector;
using System.Globalization;

namespace Genbox.CSharpFastPFOR.Benchmarks;

internal static class VectorIntBitPackBenchmark
{
    private const int BLOCK = 256;
    private const int SUBBLOCKS = BLOCK / 32;
    private const long RandomMultiplier = 0x5DEECE66DL;
    private const long RandomAddend = 0xBL;
    private const long RandomMask = (1L << 48) - 1;

    private static void scalarPack(int[] @in, int[] @out, int bit)
    {
        for (int blk = 0; blk < SUBBLOCKS; blk++)
            BitPacking.fastpackwithoutmask(@in, blk * 32, @out, blk * bit, bit);
    }

    private static void scalarUnpack(int[] @in, int[] @out, int bit)
    {
        for (int blk = 0; blk < SUBBLOCKS; blk++)
            BitPacking.fastunpack(@in, blk * bit, @out, blk * 32, bit);
    }

    private static void test(bool verbose)
    {
        const int times = 100000;
        long randomSeed = RandomMultiplier;
        int[] data = new int[BLOCK];
        int[] compressed = new int[8 * 32];
        int[] uncompressed = new int[BLOCK];
        VectorBitPacker256 vec256 = new VectorBitPacker256();

        for (int bit = 1; bit <= 32; ++bit)
        {
            int mask = bit == 32 ? -1 : (1 << bit) - 1;
            long scalarComp = 0;
            long scalarDecomp = 0;
            long vec256Comp = 0;
            long vec256Decomp = 0;
            long vecComp = 0;
            long vecDecomp = 0;
            for (int t = 0; t < times; ++t)
            {
                for (int k = 0; k < BLOCK; ++k)
                    data[k] = nextInt(ref randomSeed) & mask;

                long time1 = Port.System.nanoTime();
                scalarPack(data, compressed, bit);
                long time2 = Port.System.nanoTime();
                scalarUnpack(compressed, uncompressed, bit);
                long time3 = Port.System.nanoTime();
                vec256.fastpackNoMask(data, 0, compressed, 0, bit);
                long time4 = Port.System.nanoTime();
                vec256.fastunpack(compressed, 0, uncompressed, 0, bit);
                long time5 = Port.System.nanoTime();
                VectorBitPacker.fastpackNoMask(data, 0, compressed, 0, bit);
                long time6 = Port.System.nanoTime();
                VectorBitPacker.fastunpack(compressed, 0, uncompressed, 0, bit);
                long time7 = Port.System.nanoTime();
                scalarComp += time2 - time1;
                scalarDecomp += time3 - time2;
                vec256Comp += time4 - time3;
                vec256Decomp += time5 - time4;
                vecComp += time6 - time5;
                vecDecomp += time7 - time6;
            }
            if (verbose)
            {
                double sc = BLOCK * times * 1000.0;
                Console.WriteLine("bit = " + bit
                                  + " | scalar comp = " + (sc / scalarComp).ToString("0", CultureInfo.InvariantCulture)
                                  + " vec256 comp = " + (sc / vec256Comp).ToString("0", CultureInfo.InvariantCulture)
                                  + " vec comp = " + (sc / vecComp).ToString("0", CultureInfo.InvariantCulture)
                                  + " | scalar decomp = " + (sc / scalarDecomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " vec256 decomp = " + (sc / vec256Decomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " vec decomp = " + (sc / vecDecomp).ToString("0", CultureInfo.InvariantCulture));
            }
        }
    }

    public static void main(string[] args)
    {
        _ = args;
        Console.WriteLine("Testing int packing (scalar vs VectorBitPacker256 vs VectorBitPacker), 256-int blocks, speeds in millions of ints/s");
        test(false);
        test(true);
    }

    private static int nextInt(ref long seed)
    {
        seed = ((seed * RandomMultiplier) + RandomAddend) & RandomMask;
        return (int)(seed >>> 16);
    }
}