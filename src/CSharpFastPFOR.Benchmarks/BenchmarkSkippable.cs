/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/*
 *
 * Simple class meant to compare the speed of different schemes.
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

internal static class BenchmarkSkippable
{
    private static readonly object[] codecs =
    [
        new SkippableIntegratedComposition(new IntegratedBinaryPacking(),
            new IntegratedVariableByte()),
        new JustCopy(), new VariableByte(),

        new SkippableComposition(new BinaryPacking(), new VariableByte()),
        new SkippableComposition(new NewPFD(), new VariableByte()),
        new SkippableComposition(new NewPFDS9(), new VariableByte()),
        new SkippableComposition(new NewPFDS16(), new VariableByte()),
        new SkippableComposition(new OptPFD(), new VariableByte()),
        new SkippableComposition(new OptPFDS9(), new VariableByte()),
        new SkippableComposition(new OptPFDS16(), new VariableByte()),
        new SkippableComposition(new FastPFOR(), new VariableByte()),
        new SkippableComposition(new FastPFOR128(), new VariableByte()),
        new Simple9(), new Simple16()
    ];

    /*
     * Main method.
     *
     * @param args
     * command-line arguments
     */
    public static void main(string[] args)
    {
        _ = args;
        Console.WriteLine("# benchmark based on the ClusterData model from:");
        Console.WriteLine("#      Vo Ngoc Anh and Alistair Moffat. ");
        Console.WriteLine("#     Index compression using 64-bit words.");
        Console.WriteLine("#      Softw. Pract. Exper.40, 2 (February 2010), 131-147. ");
        Console.WriteLine();

        string csvFile = "benchmark-" + DateTime.Now.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture) + ".csv";
        using FileStream file = new FileStream(csvFile, FileMode.Create);
        using StreamWriter writer = new StreamWriter(file);
        Console.WriteLine("# Results will be written into a CSV file: " + csvFile);
        Console.WriteLine();
        test(writer, 20, 18, 10);
        Console.WriteLine();
        Console.WriteLine("Results were written into a CSV file: " + csvFile);
    }

    private static void compressWithSkipTable(object c, int[] data, int[] output, IntWrapper outpos, int[] metadata, int blocksize)
    {
        int metapos = 0;
        metadata[metapos++] = data.Length;
        IntWrapper inpos = new IntWrapper();
        int initvalue = 0;
        IntWrapper ival = new IntWrapper(initvalue);
        while (inpos.get() < data.Length)
        {
            metadata[metapos++] = outpos.get();
            metadata[metapos++] = initvalue;
            if (c is SkippableIntegerCODEC skippableCodec)
            {
                int size = blocksize > data.Length - inpos.get()
                    ? data.Length
                      - inpos.get()
                    : blocksize;
                initvalue = Delta.delta(data, inpos.get(), size, initvalue);

                skippableCodec.headlessCompress(data, inpos,
                    blocksize, output, outpos);
            }
            else if (c is SkippableIntegratedIntegerCODEC integratedCodec)
            {
                ival.set(initvalue);
                integratedCodec.headlessCompress(data,
                    inpos, blocksize, output, outpos, ival);
                initvalue = ival.get();
            }
            else
                throw new ArgumentException("Unrecognized codec " + c, nameof(c));
        }
    }

    private static int decompressFromSkipTable(object c, int[] compressed, IntWrapper compressedpos, int[] metadata, int blocksize, int[] data)
    {
        int metapos = 0;
        int length = metadata[metapos++];
        IntWrapper uncomppos = new IntWrapper();
        IntWrapper ival = new IntWrapper();
        while (uncomppos.get() < length)
        {
            int num = blocksize;
            if (num > length - uncomppos.get())
                num = length - uncomppos.get();
            int location = metadata[metapos++];
            // Console.WriteLine("location = "+location);
            int initvalue = metadata[metapos++];
            int outputlocation = uncomppos.get();
            if (location != compressedpos.get())
            {
                throw new InvalidOperationException("Bug " + location + " "
                                    + compressedpos.get() + " codec " + c);
            }

            if (c is SkippableIntegerCODEC skippableCodec)
            {
                skippableCodec.headlessUncompress(compressed,
                    compressedpos, compressed.Length - uncomppos.get(),
                    data, uncomppos, num);
                Delta.fastinverseDelta(data, outputlocation, num, initvalue);
            }
            else if (c is SkippableIntegratedIntegerCODEC integratedCodec)
            {
                ival.set(initvalue);
                integratedCodec.headlessUncompress(
                    compressed, compressedpos, compressed.Length
                                               - uncomppos.get(), data, uncomppos, num, ival);
            }
            else
                throw new ArgumentException("Unrecognized codec " + c, nameof(c));
        }
        return length;
    }

    /*
     * Standard benchmark
     *
     * @param csvLog
     * Writer for CSV log.
     * @param c
     * the codec
     * @param data
     * arrays of input data
     * @param repeat
     * How many times to repeat the test
     * @param verbose
     * whether to output result on screen
     */
    private static void testCodec(StreamWriter csvLog, int sparsity, object c, int[][] data, int repeat, bool verbose)
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
                maxLength = data[k].Length;
        }

        // 4x + 1024 to account for the possibility of some negative
        // compression.
        int[] compressBuffer = new int[(4 * maxLength) + 1024];
        int[] decompressBuffer = new int[maxLength + 1024];
        int[] metadataBuffer = new int[maxLength];

        const int blocksize = 1024;

        // These variables hold time in microseconds (10^-6).
        double compressTime = 0;
        double decompressTime = 0;

        const int times = 5;

        int size = 0;

        for (int r = 0; r < repeat; ++r)
        {
            size = 0;
            for (int k = 0; k < N; ++k)
            {
                int[] backupdata = Arrays.copyOf(data[k], data[k].Length);

                // compress data.
                long beforeCompress = Port.System.nanoTime() / 1000;
                IntWrapper outpos = new IntWrapper();
                compressWithSkipTable(c, backupdata, compressBuffer, outpos,
                    metadataBuffer, blocksize);
                long afterCompress = Port.System.nanoTime() / 1000;

                // measure time of compression.
                compressTime += afterCompress - beforeCompress;

                int thiscompsize = outpos.get();
                size += thiscompsize;
                // dry run
                int volume;
                {
                    IntWrapper compressedpos = new IntWrapper(0);
                    volume = decompressFromSkipTable(c, compressBuffer,
                        compressedpos, metadataBuffer, blocksize,
                        decompressBuffer);

                    // let us check the answer
                    if (volume != backupdata.Length)
                    {
                        throw new InvalidOperationException(
                            "Bad output size with codec " + c);
                    }
                    for (int j = 0; j < volume; ++j)
                    {
                        if (data[k][j] != decompressBuffer[j])
                            throw new InvalidOperationException("bug in codec " + c);
                    }
                }
                // extract (uncompress) data
                long beforeDecompress = Port.System.nanoTime() / 1000;
                for (int t = 0; t < times; ++t)
                {
                    IntWrapper compressedpos = new IntWrapper(0);
                    volume = decompressFromSkipTable(c, compressBuffer,
                        compressedpos, metadataBuffer, blocksize,
                        decompressBuffer);
                }
                long afterDecompress = Port.System.nanoTime() / 1000;

                // measure time of extraction (uncompression).
                decompressTime += (afterDecompress - beforeDecompress) / (double)times;
                if (volume != data[k].Length)
                {
                    throw new InvalidOperationException("we have a bug (diff length) "
                                        + c + " expected " + data[k].Length + " got "
                                        + volume);
                }

                // verify: compare original array with
                // compressed and
                // uncompressed.
                for (int m = 0; m < outpos.get(); ++m)
                {
                    if (decompressBuffer[m] != data[k][m])
                    {
                        throw new InvalidOperationException(
                            "we have a bug (actual difference), expected "
                            + data[k][m] + " found "
                            + decompressBuffer[m] + " at " + m);
                    }
                }
            }
        }

        if (verbose)
        {
            double bitsPerInt = (size * 32.0) / totalSize;
            double compressSpeed = Math.Round((totalSize * repeat) / compressTime);
            double decompressSpeed = Math.Round((totalSize * repeat) / decompressTime);
            Console.WriteLine("\t{0:0.00}\t{1}\t{2}", bitsPerInt, compressSpeed, decompressSpeed);
            csvLog.WriteLine("\"{0}\",{1},{2:0.00},{3},{4}", c, sparsity, bitsPerInt, compressSpeed, decompressSpeed);
        }
    }

    /*
     * Generate test data.
     *
     * @param N
     * How many input arrays to generate
     * @param nbr
     * How big (in log2) should the arrays be
     * @param sparsity
     * How sparse test data generated
     */
    private static int[][] generateTestData(ClusteredDataGenerator dataGen, int N, int nbr, int sparsity)
    {
        int[][] data = new int[N][];

        int dataSize = 1 << (nbr + sparsity);
        for (int i = 0; i < N; ++i)
            data[i] = dataGen.generateClustered(1 << nbr, dataSize);
        return data;
    }

    /*
     * Generates data and calls other tests.
     *
     * @param csvLog
     * Writer for CSV log.
     * @param N
     * How many input arrays to generate
     * @param nbr
     * how big (in log2) should the arrays be
     * @param repeat
     * How many times should we repeat tests.
     */
    private static void test(StreamWriter csvLog, int N, int nbr, int repeat)
    {
        csvLog.WriteLine("\"Algorithm\",\"Sparsity\",\"Bits per int\",\"Compress speed (MiS)\",\"Decompress speed (MiS)\"");
        ClusteredDataGenerator cdg = new ClusteredDataGenerator();

        int max_sparsity = 31 - nbr;

        for (int sparsity = 1; sparsity < max_sparsity; sparsity += 4)
        {
            Console.WriteLine("# sparsity " + sparsity);
            Console.WriteLine("# generating random data...");
            int[][] data = generateTestData(cdg, N, nbr, sparsity);
            Console.WriteLine("# generating random data... ok.");
            foreach (object c in codecs)
            {
                testCodec(csvLog, sparsity, c, data, repeat, false);
                testCodec(csvLog, sparsity, c, data, repeat, false);
                testCodec(csvLog, sparsity, c, data, repeat, true);
                testCodec(csvLog, sparsity, c, data, repeat, true);
                testCodec(csvLog, sparsity, c, data, repeat, true);
            }
        }
    }
}