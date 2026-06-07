/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/*
 * Class used to benchmark the speed of bit packing. (For expert use.)
 *
 * @author Daniel Lemire
 *
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.IntegerCompression.Synth;
using System.Globalization;

namespace Genbox.CSharpFastPFOR.Benchmarks;

internal static class BenchmarkBitPacking
{
    /*
     * Main method
     *
     * @param args
     * command-line arguments
     */
    public static void main(string[] args)
    {
        _ = args;
        Console.WriteLine("Testing packing and delta ");
        testWithDeltas(false);
        testWithDeltas(true);
        Console.WriteLine("Testing packing alone ");
        test(false);
        test(true);
    }

    private static void test(bool verbose)
    {
        const int N = 32;
        const int times = 100000;
        JavaRandom r = new JavaRandom(0);
        int[] data = new int[N];
        int[] compressed = new int[N];
        int[] uncompressed = new int[N];

        for (int bit = 0; bit < 31; ++bit)
        {
            long comp = 0;
            long compwm = 0;
            long decomp = 0;
            for (int t = 0; t < times; ++t)
            {
                for (int k = 0; k < N; ++k)
                    data[k] = r.Next(1 << bit);

                long time1 = Port.System.nanoTime();
                BitPacking
                    .fastpack(data, 0, compressed, 0, bit);
                long time2 = Port.System.nanoTime();
                BitPacking.fastpackwithoutmask(data, 0,
                    compressed, 0, bit);
                long time3 = Port.System.nanoTime();
                BitPacking.fastunpack(compressed, 0,
                    uncompressed, 0, bit);
                long time4 = Port.System.nanoTime();
                comp += time2 - time1;
                compwm += time3 - time2;
                decomp += time4 - time3;
            }
            if (verbose)
            {
                Console.WriteLine("bit = "
                                  + bit
                                  + " comp. speed = "
                                   + ((N * times * 1000.0) / comp).ToString("0", CultureInfo.InvariantCulture)
                                  + " comp. speed wm = "
                                   + ((N * times * 1000.0) / compwm).ToString("0", CultureInfo.InvariantCulture)
                                  + " decomp. speed = "
                                   + ((N * times * 1000.0) / decomp).ToString("0", CultureInfo.InvariantCulture));
            }
        }
    }

    private static void testWithDeltas(bool verbose)
    {
        const int N = 32;
        const int times = 100000;

        JavaRandom r = new JavaRandom(0);
        int[] data = new int[N];
        int[] compressed = new int[N];
        int[] icompressed = new int[N];
        int[] uncompressed = new int[N];

        for (int bit = 1; bit < 31; ++bit)
        {
            long comp = 0;
            long decomp = 0;
            long icomp = 0;
            long idecomp = 0;

            for (int t = 0; t < times; ++t)
            {
                data[0] = r.Next(1 << bit);
                for (int k = 1; k < N; ++k)
                {
                    data[k] = r.Next(1 << bit)
                              + data[k - 1];
                }

                int[] tmpdata = Arrays.copyOf(data, data.Length);

                long time1 = Port.System.nanoTime();
                Delta.delta(tmpdata);
                BitPacking.fastpackwithoutmask(tmpdata, 0,
                    compressed, 0, bit);
                long time2 = Port.System.nanoTime();
                BitPacking.fastunpack(compressed, 0,
                    uncompressed, 0, bit);
                Delta.fastinverseDelta(uncompressed);
                long time3 = Port.System.nanoTime();
                if (!Arrays.equals(data, uncompressed))
                    throw new InvalidOperationException("bug");
                comp += time2 - time1;
                decomp += time3 - time2;
                tmpdata = Arrays.copyOf(data, data.Length);
                time1 = Port.System.nanoTime();
                IntegratedBitPacking.integratedpack(0, tmpdata,
                    0, icompressed, 0, bit);
                time2 = Port.System.nanoTime();
                IntegratedBitPacking.integratedunpack(0,
                    icompressed, 0, uncompressed, 0, bit);
                time3 = Port.System.nanoTime();
                if (!Arrays.equals(icompressed, compressed))
                    throw new InvalidOperationException("ibug " + bit);

                if (!Arrays.equals(data, uncompressed))
                    throw new InvalidOperationException("bug " + bit);
                icomp += time2 - time1;
                idecomp += time3 - time2;
            }

            if (verbose)
            {
                Console.WriteLine("bit = "
                                  + bit
                                  + " comp. speed = "
                                   + ((N * times * 1000.0) / comp).ToString("0", CultureInfo.InvariantCulture)
                                  + " decomp. speed = "
                                   + ((N * times * 1000.0) / decomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " icomp. speed = "
                                   + ((N * times * 1000.0) / icomp).ToString("0", CultureInfo.InvariantCulture)
                                  + " idecomp. speed = "
                                   + ((N * times * 1000.0) / idecomp).ToString("0", CultureInfo.InvariantCulture));
            }
        }
    }
}