/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/**
 *
 * Simple class meant to compare the speed of different schemes.
 *
 * @author Daniel Lemire
 *
 */

using Genbox.CSharpFastPFOR.Differential;
using Genbox.CSharpFastPFOR.Port;
using Genbox.CSharpFastPFOR.Synth;

namespace Genbox.CSharpFastPFOR.Benchmarks;

public static class Benchmark
{
    /**
     * Standard benchmark
     *
     * @param csvLog
     *                Writer for CSV log.
     * @param c
     *                the codec
     * @param data
     *                arrays of input data
     * @param repeat
     *                How many times to repeat the test
     * @param verbose
     *                whether to output result on screen
     */
    private static void testCodec(StreamWriter csvLog, int sparsity, IntegerCODEC c, int[][] data, int repeat, bool verbose)
    {
        if (verbose)
        {
            Console.WriteLine("# " + c);
            Console.WriteLine("# bits per int, compress speed (mis), decompression speed (mis) ");
        }

        int N = data.Length;

        int totalSize = 0;
        int maxLength = 0;
        for (int k = 0; k < N; ++k)
        {
            totalSize += data[k].Length;
            if (data[k].Length > maxLength)
            {
                maxLength = data[k].Length;
            }
        }

        // 4x + 1024 to account for the possibility of some negative
        // compression.
        int[] compressBuffer = new int[4 * maxLength + 1024];
        int[] decompressBuffer = new int[maxLength + 1024];

        // These variables hold time in microseconds (10^-6).
        long compressTime = 0;
        long decompressTime = 0;

        int size = 0;
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();

        for (int r = 0; r < repeat; ++r)
        {
            size = 0;
            for (int k = 0; k < N; ++k)
            {
                int[] backupdata = Arrays.copyOf(data[k],
                    data[k].Length);

                // compress data.
                long beforeCompress = Port.System.nanoTime() / 1000;
                inpos.set(1);
                outpos.set(0);
                if (!(c is IntegratedIntegerCODEC))
                {
                    Delta.delta(backupdata);
                }
                c.compress(backupdata, inpos, backupdata.Length
                                              - inpos.get(), compressBuffer, outpos);
                long afterCompress = Port.System.nanoTime() / 1000;

                // measure time of compression.
                compressTime += afterCompress - beforeCompress;

                int thiscompsize = outpos.get() + 1;
                size += thiscompsize;

                // extract (uncompress) data
                long beforeDecompress = Port.System.nanoTime() / 1000;
                inpos.set(0);
                outpos.set(1);
                decompressBuffer[0] = backupdata[0];
                c.uncompress(compressBuffer, inpos,
                    thiscompsize - 1, decompressBuffer,
                    outpos);
                if (!(c is IntegratedIntegerCODEC))
                    Delta.fastinverseDelta(decompressBuffer);
                long afterDecompress = Port.System.nanoTime() / 1000;

                // measure time of extraction (uncompression).
                decompressTime += afterDecompress
                                  - beforeDecompress;
                if (outpos.get() != data[k].Length)
                    throw new Exception(
                        "we have a bug (diff length) "
                        + c + " expected "
                        + data[k].Length
                        + " got "
                        + outpos.get());

                // verify: compare original array with
                // compressed and
                // uncompressed.

                for (int m = 0; m < outpos.get(); ++m)
                {
                    if (decompressBuffer[m] != data[k][m])
                    {
                        throw new Exception(
                            "we have a bug (actual difference), expected "
                            + data[k][m]
                            + " found "
                            + decompressBuffer[m]
                            + " at " + m);
                    }
                }
            }
        }

        if (verbose)
        {
            double bitsPerInt = size * 32.0 / totalSize;
            long compressSpeed = totalSize * repeat / (compressTime);
            long decompressSpeed = totalSize * repeat / (decompressTime);

            Console.WriteLine("\t{0:0.00}\t{1}\t{2}", bitsPerInt, compressSpeed, decompressSpeed);
            csvLog.WriteLine("\"{0}\",{1},{2:0.00},{3},{4}", c, sparsity, bitsPerInt, compressSpeed, decompressSpeed);
        }
    }

    /**
     * Standard benchmark byte byte-aligned schemes
     *
     * @param csvLog
     *                Writer for CSV log.
     * @param c
     *                the codec
     * @param data
     *                arrays of input data
     * @param repeat
     *                How many times to repeat the test
     * @param verbose
     *                whether to output result on screen
     */
    private static void testByteCodec(StreamWriter csvLog, int sparsity, ByteIntegerCODEC c, int[][] data, int repeat, bool verbose)
    {
        if (verbose)
        {
            Console.WriteLine("# " + c);
            Console.WriteLine("# bits per int, compress speed (mis), decompression speed (mis) ");
        }

        int N = data.Length;

        int totalSize = 0;
        int maxLength = 0;
        for (int k = 0; k < N; ++k)
        {
            totalSize += data[k].Length;
            if (data[k].Length > maxLength)
            {
                maxLength = data[k].Length;
            }
        }

        sbyte[] compressBuffer = new sbyte[8 * maxLength + 1024];
        int[] decompressBuffer = new int[maxLength + 1024];

        // These variables hold time in microseconds (10^-6).
        long compressTime = 0;
        long decompressTime = 0;

        int size = 0;
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();

        for (int r = 0; r < repeat; ++r)
        {
            size = 0;
            for (int k = 0; k < N; ++k)
            {
                int[] backupdata = Arrays.copyOf(data[k],
                    data[k].Length);

                // compress data.
                long beforeCompress = Port.System.nanoTime() / 1000;
                inpos.set(1);
                outpos.set(0);
                if (!(c is IntegratedByteIntegerCODEC))
                {
                    Delta.delta(backupdata);
                }
                c.compress(backupdata, inpos, backupdata.Length
                                              - inpos.get(), compressBuffer, outpos);
                long afterCompress = Port.System.nanoTime() / 1000;

                // measure time of compression.
                compressTime += afterCompress - beforeCompress;

                int thiscompsize = outpos.get() + 1;
                size += thiscompsize;

                // extract (uncompress) data
                long beforeDecompress = Port.System.nanoTime() / 1000;
                inpos.set(0);
                outpos.set(1);
                decompressBuffer[0] = backupdata[0];
                c.uncompress(compressBuffer, inpos,
                    thiscompsize - 1, decompressBuffer,
                    outpos);
                if (!(c is IntegratedByteIntegerCODEC))
                    Delta.fastinverseDelta(decompressBuffer);
                long afterDecompress = Port.System.nanoTime() / 1000;

                // measure time of extraction (uncompression).
                decompressTime += afterDecompress
                                  - beforeDecompress;
                if (outpos.get() != data[k].Length)
                    throw new Exception(
                        "we have a bug (diff length) "
                        + c + " expected "
                        + data[k].Length
                        + " got "
                        + outpos.get());

                // verify: compare original array with
                // compressed and
                // uncompressed.
                for (int m = 0; m < outpos.get(); ++m)
                {
                    if (decompressBuffer[m] != data[k][m])
                    {
                        throw new Exception(
                            "we have a bug (actual difference), expected "
                            + data[k][m]
                            + " found "
                            + decompressBuffer[m]
                            + " at " + m);
                    }
                }
            }
        }

        if (verbose)
        {
            double bitsPerInt = size * 8.0 / totalSize;
            long compressSpeed = totalSize * repeat / (compressTime);
            long decompressSpeed = totalSize * repeat / (decompressTime);

            Console.WriteLine("\t{0:0.00}\t{1}\t{2}", bitsPerInt, compressSpeed, decompressSpeed);
            csvLog.WriteLine("\"{0}\",{1},{2:0.00},{3},{4}", c, sparsity, bitsPerInt, compressSpeed, decompressSpeed);
        }
    }

    /**
     * Standard test for the Kamikaze library
     *
     * @param data
     *                input data
     * @param repeat
     *                how many times to repeat
     * @param verbose
     *                whether to output data on screen
     */
    //TODO port
    //public static void testKamikaze(int[][] data, int repeat, bool verbose)
    //{
    //    if (verbose)
    //        Console.WriteLine("# kamikaze PForDelta");
    //    if (verbose)
    //        Console.WriteLine("# bits per int, compress speed (mis), decompression speed (mis) ");
    //    long bef, aft;
    //    String line = "";
    //    int N = data.Length;
    //    int totalsize = 0;
    //    int maxlength = 0;
    //    for (int k = 0; k < N; ++k)
    //    {
    //        totalsize += data[k].Length;
    //        if (data[k].Length > maxlength)
    //            maxlength = data[k].Length;
    //    }
    //    int[] buffer = new int[maxlength + 1024];
    //    /*
    //     * 4x + 1024 to account for the possibility of some negative
    //     * compression
    //     */
    //    int size = 0;
    //    long comptime = 0;
    //    long decomptime = 0;

    //    for (int r = 0; r < repeat; ++r)
    //    {
    //        size = 0;
    //        for (int k = 0; k < N; ++k)
    //        {
    //            int outpos = 0;
    //            int[] backupdata = Arrays.copyOf(data[k],
    //                    data[k].Length);

    //            sw.Restart();
    //            //
    //            bef = sw.ElapsedMilliseconds;
    //            Delta.delta(backupdata);
    //            List<int[]> dataout = new List<int[]>(
    //                    data[k].Length / 128);
    //            for (int K = 0; K < data[k].Length; K += 128)
    //            {
    //
    //                int[] compressedbuf = PForDelta
    //              .compressOneBlockOpt(Arrays
    //                      .copyOfRange(
    //                              backupdata, K,
    //                              K + 128), 128);
    //                dataout.Add(compressedbuf);
    //                outpos += compressedbuf.Length;
    //            }
    //            aft = sw.ElapsedMilliseconds;
    //            //
    //            comptime += aft - bef;
    //
    //            int thiscompsize = outpos;
    //            size += thiscompsize;
    //            //
    //            bef = sw.ElapsedMilliseconds;
    //            List<int[]> datauncomp = new List<int[]>(
    //                    dataout.Count);
    //            int deltaoffset = 0;

    //            foreach (int[] compbuf in dataout)
    //            {
    //                int[] tmpbuf = new int[128];
    //                PForDelta.decompressOneBlock(tmpbuf,
    //                        compbuf, 128);
    //                tmpbuf[0] += deltaoffset;
    //                Delta.fastinverseDelta(tmpbuf);
    //                deltaoffset = tmpbuf[127];
    //                datauncomp.Add(tmpbuf);
    //            }
    //            aft = sw.ElapsedMilliseconds;
    //            //
    //            decomptime += aft - bef;
    //            if (datauncomp.Count * 128 != data[k].Length)
    //                throw new Exception(
    //                        "we have a bug (diff length) "
    //                                + " expected "
    //                                + data[k].Length
    //                                + " got "
    //                                + datauncomp.Count
    //                                * 128);
    //            for (int m = 0; m < data[k].Length; ++m)
    //                if (datauncomp[m / 128][m % 128] != data[k][m])
    //                {
    //                    throw new Exception(
    //                            "we have a bug (actual difference), expected "
    //                                    + data[k][m]
    //                                    + " found "
    //                                    + buffer[m]
    //                                    + " at " + m);
    //                }

    //        }
    //    }

    //    line += "\t" + (size * 32.0 / totalsize).ToString("0.00"); //TODO port
    //    line += "\t" + (totalsize * repeat / (comptime)).ToString("0"); //TODO port
    //    line += "\t" + (totalsize * repeat / (decomptime)).ToString("0"); //TODO port

    //    if (verbose)
    //        Console.WriteLine(line);
    //}

    /**
     * Generate test data.
     *
     * @param N
     *                How many input arrays to generate
     * @param nbr
     *                How big (in log2) should the arrays be
     * @param sparsity
     *                How sparse test data generated
     */
    private static int[][] generateTestData(ClusteredDataGenerator dataGen, int N, int nbr, int sparsity)
    {
        int[][] data = new int[N][];

        int dataSize = (1 << (nbr + sparsity));
        for (int i = 0; i < N; ++i)
        {
            data[i] = dataGen.generateClustered((1 << nbr),
                dataSize);
        }
        return data;
    }

    /**
     * Generates data and calls other tests.
     *
     * @param csvLog
     *                Writer for CSV log.
     * @param N
     *                How many input arrays to generate
     * @param nbr
     *                how big (in log2) should the arrays be
     * @param repeat
     *                How many times should we repeat tests.
     */
    public static void test(StreamWriter csvLog, int N, int nbr, int repeat)
    {
        csvLog.WriteLine("\"Algorithm\",\"Sparsity\",\"Bits per int\",\"Compress speed (MiS)\",\"Decompress speed (MiS)\"");
        ClusteredDataGenerator cdg = new ClusteredDataGenerator();

        int max_sparsity = 31 - nbr;
        for (int sparsity = 1; sparsity < max_sparsity; ++sparsity)
        {
            Console.WriteLine("# sparsity " + sparsity);
            Console.WriteLine("# generating random data...");
            int[][] data = generateTestData(cdg, N, nbr, sparsity);
            Console.WriteLine("# generating random data... ok.");

            testCodec(csvLog, sparsity, new Composition(
                    new FastPFOR128(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new FastPFOR128(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new FastPFOR128(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();
            testCodec(csvLog, sparsity, new Composition(
                    new FastPFOR(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new FastPFOR(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new FastPFOR(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            //TODO: port
            // TODO: support CSV log output.
            //testKamikaze(data, repeat, false);
            //testKamikaze(data, repeat, false);
            //testKamikaze(data, repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new IntegratedComposition(
                    new IntegratedBinaryPacking(),
                    new IntegratedVariableByte()), data, repeat,
                false);
            testCodec(csvLog, sparsity, new IntegratedComposition(
                    new IntegratedBinaryPacking(),
                    new IntegratedVariableByte()), data, repeat,
                false);
            testCodec(csvLog, sparsity, new IntegratedComposition(
                    new IntegratedBinaryPacking(),
                    new IntegratedVariableByte()), data, repeat,
                true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new JustCopy(), data,
                repeat, false);
            testCodec(csvLog, sparsity, new JustCopy(), data,
                repeat, false);
            testCodec(csvLog, sparsity, new JustCopy(), data,
                repeat, true);
            Console.WriteLine();

            testByteCodec(csvLog, sparsity, new VariableByte(),
                data, repeat, false);
            testByteCodec(csvLog, sparsity, new VariableByte(),
                data, repeat, false);
            testByteCodec(csvLog, sparsity, new VariableByte(),
                data, repeat, true);
            Console.WriteLine();

            testByteCodec(csvLog, sparsity,
                new IntegratedVariableByte(), data, repeat,
                false);
            testByteCodec(csvLog, sparsity,
                new IntegratedVariableByte(), data, repeat,
                false);
            testByteCodec(csvLog, sparsity,
                new IntegratedVariableByte(), data, repeat,
                true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new BinaryPacking(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new BinaryPacking(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new BinaryPacking(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new NewPFD(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new NewPFD(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new NewPFD(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new NewPFDS9(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new NewPFDS9(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new NewPFDS9(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new NewPFDS16(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new NewPFDS16(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new NewPFDS16(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new OptPFD(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new OptPFD(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new OptPFD(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new OptPFDS9(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new OptPFDS9(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new OptPFDS9(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();

            testCodec(csvLog, sparsity, new Composition(
                    new OptPFDS16(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new OptPFDS16(), new VariableByte()), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Composition(
                    new OptPFDS16(), new VariableByte()), data,
                repeat, true);
            Console.WriteLine();


            testCodec(csvLog, sparsity, new Simple9(), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Simple9(), data,
                repeat, false);
            testCodec(csvLog, sparsity, new Simple9(), data,
                repeat, true);
            Console.WriteLine();

            {
                IntegerCODEC c = new Composition(
                    new XorBinaryPacking(),
                    new VariableByte());
                testCodec(csvLog, sparsity, c, data, repeat,
                    false);
                testCodec(csvLog, sparsity, c, data, repeat,
                    false);
                testCodec(csvLog, sparsity, c, data, repeat,
                    true);
                Console.WriteLine();
            }

            {
                IntegerCODEC c = new Composition(
                    new DeltaZigzagBinaryPacking(),
                    new DeltaZigzagVariableByte());
                testCodec(csvLog, sparsity, c, data, repeat,
                    false);
                testCodec(csvLog, sparsity, c, data, repeat,
                    false);
                testCodec(csvLog, sparsity, c, data, repeat,
                    true);
                Console.WriteLine();
            }
        }
    }
}