/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/**
 * Simple synthetic benchmark
 *
 */

using Genbox.CSharpFastPFOR.Differential;
using Genbox.CSharpFastPFOR.Port;

namespace Genbox.CSharpFastPFOR.Benchmarks;

public static class BenchmarkOffsettedSeries
{
    private const int DEFAULT_MEAN = 1 << 20;
    private const int DEFAULT_RANGE = 1 << 10;
    private const int DEFAULT_REPEAT = 5;
    private const int DEFAULT_WARMUP = 2;

    /**
     * Run benchmark.
     *
     * @param csvWriter
     *                Write for results in CSV.
     * @param count
     *                Count of data chunks.
     * @param length
     *                Length of a data chunk.
     */
    public static void run(StreamWriter csvWriter, int count, int length)
    {
        IntegerCODEC[] codecs =
        [
            new JustCopy(), new BinaryPacking(),
            new DeltaZigzagBinaryPacking(),
            new DeltaZigzagVariableByte(),
            new IntegratedBinaryPacking(), new XorBinaryPacking(),
            new FastPFOR128(), new FastPFOR()
        ];

        csvWriter.WriteLine("\"Dataset\",\"CODEC\",\"Bits per int\"," + "\"Compress speed (MiS)\",\"Decompress speed (MiS)\"");

        benchmark(csvWriter, codecs, count, length, DEFAULT_MEAN, DEFAULT_RANGE);
        benchmark(csvWriter, codecs, count, length, DEFAULT_MEAN >> 5, DEFAULT_RANGE);

        IntegerCODEC[] codecs2 =
        [
            new JustCopy(), new BinaryPacking(),
            new DeltaZigzagBinaryPacking(),
            new DeltaZigzagVariableByte(),
            new IntegratedBinaryPacking(), new XorBinaryPacking(),
            new FastPFOR128(), new FastPFOR()
        ];

        int freq = length / 4;
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 0, DEFAULT_RANGE >> 0, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 5, DEFAULT_RANGE >> 0, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 10, DEFAULT_RANGE >> 0, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 0, DEFAULT_RANGE >> 2, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 5, DEFAULT_RANGE >> 2, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 10, DEFAULT_RANGE >> 2, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 0, DEFAULT_RANGE >> 4, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 5, DEFAULT_RANGE >> 4, freq);
        benchmarkSine(csvWriter, codecs2, count, length, DEFAULT_MEAN >> 10, DEFAULT_RANGE >> 4, freq);
    }

    private static void benchmarkSine(StreamWriter csvWriter, IntegerCODEC[] codecs, int count, int length, int mean, int range, int freq)
    {
        string dataProp = string.Format("(mean={0} range={1} freq={2})", mean, range, freq);
        int[][] data = generateSineDataChunks(0, count, length, mean, range, freq);
        benchmark(csvWriter, "Sine " + dataProp, codecs, data, DEFAULT_REPEAT, DEFAULT_WARMUP);
        benchmark(csvWriter, "Sine+delta " + dataProp, codecs, data, DEFAULT_REPEAT, DEFAULT_WARMUP);
    }

    private static void benchmark(StreamWriter csvWriter, IntegerCODEC[] codecs, int count, int length, int mean, int range)
    {
        string dataProp = string.Format("(mean={0} range={1})", mean, range);

        int[][] randData = generateDataChunks(0, count, length, mean, range);
        int[][] deltaData = deltaDataChunks(randData);
        int[][] sortedData = sortDataChunks(randData);
        int[][] sortedDeltaData = deltaDataChunks(sortedData);

        benchmark(csvWriter, "Random " + dataProp, codecs, randData, DEFAULT_REPEAT, DEFAULT_WARMUP);
        benchmark(csvWriter, "Random+delta " + dataProp, codecs, deltaData, DEFAULT_REPEAT, DEFAULT_WARMUP);
        benchmark(csvWriter, "Sorted " + dataProp, codecs, sortedData, DEFAULT_REPEAT, DEFAULT_WARMUP);
        benchmark(csvWriter, "Sorted+delta " + dataProp, codecs, sortedDeltaData, DEFAULT_REPEAT, DEFAULT_WARMUP);
    }

    private static void benchmark(StreamWriter csvWriter, string dataName, IntegerCODEC[] codecs, int[][] data, int repeat, int warmup)
    {
        Console.WriteLine("Processing: " + dataName);
        foreach (IntegerCODEC codec in codecs)
        {
            string codecName = codec.ToString();
            for (int i = 0; i < warmup; ++i)
            {
                benchmark(null, null, null, codec, data, repeat);
            }
            benchmark(csvWriter, dataName, codecName, codec, data, repeat);
        }
    }

    private static void benchmark(StreamWriter csvWriter, string dataName, string codecName, IntegerCODEC codec, int[][] data, int repeat)
    {
        PerformanceLogger logger = new PerformanceLogger();

        int maxLen = getMaxLen(data);
        int[] compressBuffer = new int[4 * maxLen + 1024];
        int[] decompressBuffer = new int[maxLen];

        for (int i = 0; i < repeat; ++i)
        {
            foreach (int[] array in data)
            {
                int compSize = compress(logger, codec, array, compressBuffer);
                int decompSize = decompress(logger, codec, compressBuffer, compSize, decompressBuffer);
                checkArray(array, decompressBuffer, decompSize, codec);
            }
        }

        if (csvWriter != null)
        {
            csvWriter.WriteLine("\"{0}\",\"{1}\",{2:0.00},{3:0},{4:0}", dataName, codecName, logger.getBitPerInt(), logger.getCompressSpeed(), logger.getDecompressSpeed());
        }
    }

    private static void checkArray(int[] expected, int[] actualArray,
        int actualLen, IntegerCODEC codec)
    {
        if (actualLen != expected.Length)
        {
            throw new Exception("Length mismatch:" + " expected=" + expected.Length + " actual=" + actualLen + " codec=" + codec);
        }

        for (int i = 0; i < expected.Length; ++i)
        {
            if (actualArray[i] != expected[i])
            {
                throw new Exception("Value mismatch: "
                                    + " where=" + i + " expected="
                                    + expected[i] + " actual="
                                    + actualArray[i] + " codec="
                                    + codec);
            }
        }
    }

    private static int compress(PerformanceLogger logger, IntegerCODEC codec, int[] src, int[] dst)
    {
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();
        logger.compressionTimer.start();
        codec.compress(src, inpos, src.Length, dst, outpos);
        logger.compressionTimer.end();
        int outSize = outpos.get();
        logger.addOriginalSize(src.Length);
        logger.addCompressedSize(outSize);
        return outSize;
    }

    private static int decompress(PerformanceLogger logger, IntegerCODEC codec, int[] src, int srcLen, int[] dst)
    {
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();
        logger.decompressionTimer.start();
        codec.uncompress(src, inpos, srcLen, dst, outpos);
        logger.decompressionTimer.end();
        return outpos.get();
    }

    private static int getMaxLen(int[][] data)
    {
        int maxLen = 0;
        foreach (int[] array in data)
        {
            if (array.Length > maxLen)
            {
                maxLen = array.Length;
            }
        }
        return maxLen;
    }

    private static int[][] generateSineDataChunks(int seed, int count, int length, int mean, int range, int freq)
    {
        int[][] chunks = new int[count][];
        Random r = new Random(seed);
        for (int i = 0; i < count; ++i)
        {
            int[] chunk = chunks[i] = new int[length];
            int phase = r.Next(2 * freq);
            for (int j = 0; j < length; ++j)
            {
                double angle = 2.0 * Math.PI * (j + phase) / freq;
                chunk[j] = (int)(mean + Math.Sin(angle) * range);
            }
        }
        return chunks;
    }

    private static int[][] generateDataChunks(int seed, int count, int length, int mean, int range)
    {
        int offset = mean - range / 2;
        int[][] chunks = new int[count][];
        Random r = new Random(seed);
        for (int i = 0; i < count; ++i)
        {
            int[] chunk = chunks[i] = new int[length];
            for (int j = 0; j < length; ++j)
            {
                chunk[j] = r.Next(range) + offset;
            }
        }
        return chunks;
    }

    private static int[][] deltaDataChunks(int[][] src)
    {
        int[][] dst = new int[src.Length][];
        for (int i = 0; i < src.Length; ++i)
        {
            int[] s = src[i];
            int[] d = dst[i] = new int[s.Length];
            int prev = 0;
            for (int j = 0; j < s.Length; ++j)
            {
                d[j] = s[j] - prev;
                prev = s[j];
            }
        }
        return dst;
    }

    private static int[][] sortDataChunks(int[][] src)
    {
        int[][] dst = new int[src.Length][];
        for (int i = 0; i < src.Length; ++i)
        {
            dst[i] = Arrays.copyOf(src[i], src[i].Length);
            Arrays.sort(dst[i]);
        }
        return dst;
    }
}