/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.LongCompression.Differential;
using Genbox.CSharpFastPFOR.Tests.LongCompression.Synth;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression;

/*
 * Just some basic sanity tests.
 *
 * @author Benoit Lacelle
 */
public class LongBasicTest(ITestOutputHelper testOutputHelper)
{
    private readonly LongCODEC[] codecs =
    [
        new LongJustCopy(),
        new LongVariableByte(),
        new LongAs2IntsCodec(),
        new LongComposition(new LongBinaryPacking(), new LongVariableByte())
    ];

    /*
     * This tests with a compressed array with various offset
     */
    [Fact]
    public void saulTest()
    {
        foreach (LongCODEC C in codecs)
        {
            for (int x = 0; x < 50; ++x)
            {
                long[] a = [2, 3, 4, 5];
                long[] b = new long[90];
                long[] c = new long[a.Length];

                IntWrapper aOffset = new IntWrapper(0);
                IntWrapper bOffset = new IntWrapper(x);
                C.compress(a, aOffset, a.Length, b, bOffset);
                int len = bOffset.get() - x;

                bOffset.set(x);
                IntWrapper cOffset = new IntWrapper(0);
                C.uncompress(b, bOffset, len, c, cOffset);
                if (!a.SequenceEqual(c))
                    testOutputHelper.WriteLine("Problem with " + C);
                Assert2.assertArrayEquals(a, c);
            }
        }
    }

    /*
     *
     */
    [Fact]
    public void varyingLengthTest()
    {
        const int N = 4096;
        long[] data = new long[N];
        for (int k = 0; k < N; ++k)
            data[k] = k;
        foreach (LongCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[BasicTest.varyingLengthTest] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                long[] comp = LongTestUtils.compress(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                    {
                        throw new InvalidOperationException("bug");
                    }
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                long[] comp = LongTestUtils.compress(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                    {
                        testOutputHelper.WriteLine(string.Join(", ", Arrays.copyOf(answer, L)));
                        testOutputHelper.WriteLine(string.Join(", ", Arrays.copyOf(data, L)));
                        throw new InvalidOperationException("bug");
                    }
                }
            }
        }
    }

    /*
     *
     */
    [Fact]
    public void varyingLengthTest2()
    {
        const int N = 128;
        long[] data = new long[N];
        data[127] = -1;
        foreach (LongCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[BasicTest.varyingLengthTest2] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                long[] comp = LongTestUtils.compress(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                long[] comp = LongTestUtils.compress(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
                }
            }
        }
    }

    /*
     *
     */
    [Fact]
    public void checkVariousCases()
    {
        foreach (LongCODEC c in codecs)
        {
            testZeroInZeroOut(c);
            test(c, c, 5, 10);
            test(c, c, 5, 14);
            test(c, c, 2, 18);
            // TODO Unclear which codec should manage an empty output array or not
            // Some IntegerCodec does not output anything if the input is smaller than some block size
            // testSpurious(c);
            testUnsorted(c);
            testUnsorted2(c);
            testUnsorted3(c);
        }
    }

    /*
     * check that the codecs can be inverted.
     */
    [Fact]
    public void basictest()
    {
        foreach (LongCODEC codec in codecs)
        {
            test(codec, 5, 10);
            test(codec, 5, 14);
            test(codec, 2, 18);
        }
    }

    private static void testZeroInZeroOut(LongCODEC c)
    {
        long[] x = [];
        long[] y = [];
        IntWrapper i0 = new IntWrapper(0);
        IntWrapper i1 = new IntWrapper(0);
        c.compress(x, i0, 0, y, i1);
        Assert.Equal(0, i1.intValue());

        long[] @out = [];
        IntWrapper outpos = new IntWrapper(0);
        c.uncompress(y, i1, 0, @out, outpos);
        Assert.Equal(0, outpos.intValue());
    }

    private static void test(LongCODEC c, LongCODEC co, int N, int nbr)
    {
        LongClusteredDataGenerator cdg = new LongClusteredDataGenerator();
        for (int sparsity = 1; sparsity < 31 - nbr; sparsity += 4)
        {
            long[][] data = new long[N][];
            int max = 1 << (nbr + sparsity);
            for (int k = 0; k < N; ++k)
                data[k] = cdg.generateClustered(1 << nbr, max);
            testCodec(c, co, data);
        }
    }

    private void test(LongCODEC codec, int N, int nbr)
    {
        LongClusteredDataGenerator cdg = new LongClusteredDataGenerator();
        testOutputHelper.WriteLine("[BasicTest.test] N = " + N + " " + nbr);
        for (int sparsity = 1; sparsity < 63 - nbr; sparsity += 4)
        {
            long[][] data = new long[N][];
            long max = 1L << (nbr + sparsity);
            for (int k = 0; k < N; ++k)
                data[k] = cdg.generateClustered(1 << nbr, max);

            testCodec(codec, codec, data);
        }
    }

    private static void testCodec(LongCODEC c, LongCODEC co, long[][] data)
    {
        int N = data.Length;
        int maxlength = 0;
        for (int k = 0; k < N; ++k)
        {
            if (data[k].Length > maxlength)
                maxlength = data[k].Length;
        }
        long[] buffer = new long[maxlength + 1024];
        long[] dataout = new long[(4 * maxlength) + 1024];
        // 4x + 1024 to account for the possibility of some negative
        // compression.
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();
        for (int k = 0; k < N; ++k)
        {
            long[] backupdata = Arrays.copyOf(data[k], data[k].Length);

            inpos.set(1);
            outpos.set(0);
            if (c is not IntegratedLongCODEC)
                LongDelta.delta(backupdata);
            c.compress(backupdata, inpos, backupdata.Length - inpos.get(), dataout, outpos);
            int thiscompsize = outpos.get() + 1;
            inpos.set(0);
            outpos.set(1);
            buffer[0] = backupdata[0];
            co.uncompress(dataout, inpos, thiscompsize - 1, buffer, outpos);
            if (c is not IntegratedLongCODEC)
                LongDelta.fastinverseDelta(buffer);

            // Check assertions.
            Assert.Equal(data[k].Length, outpos.get());
            long[] bufferCutout = Arrays.copyOf(buffer, outpos.get());
            Assert2.assertArrayEquals(data[k], bufferCutout);
        }
    }

    /*
     * @param codec
     * provided codec
     */
    private void testUnsorted(LongCODEC codec)
    {
        int[] lengths = [133, 1026, 1333333];
        foreach (int N in lengths)
        {
            long[] data = new long[N];
            // initialize the data (most will be small)
            for (int k = 0; k < N; k += 1)
                data[k] = 3;
            // throw some larger values
            for (int k = 0; k < N; k += 5)
                data[k] = 100;
            for (int k = 0; k < N; k += 533)
                data[k] = 10000;
            data[5] = -311;
            // could need more compressing
            long[] compressed = new long[(int)Math.Ceiling(N * 1.01) + 1024];
            IntWrapper inputoffset = new IntWrapper(0);
            IntWrapper outputoffset = new IntWrapper(0);
            codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
            // we can repack the data: (optional)
            compressed = Arrays.copyOf(compressed, outputoffset.intValue());

            long[] recovered = new long[N];
            IntWrapper recoffset = new IntWrapper(0);
            codec.uncompress(compressed, new IntWrapper(0), compressed.Length, recovered, recoffset);
            Assert2.assertArrayEquals(data, recovered);
        }
    }

    private void testUnsorted2(LongCODEC codec)
    {
        long[] data = new long[128];
        data[5] = -1;
        long[] compressed = new long[1024];
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        long[] recovered = new long[128];
        IntWrapper recoffset = new IntWrapper(0);
        codec.uncompress(compressed, new IntWrapper(0), compressed.Length, recovered, recoffset);
        Assert2.assertArrayEquals(data, recovered);
    }

    private void testUnsorted3(LongCODEC codec)
    {
        long[] data = new long[128];
        data[127] = -1;
        long[] compressed = new long[1024];
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        long[] recovered = new long[128];
        IntWrapper recoffset = new IntWrapper(0);
        codec.uncompress(compressed, new IntWrapper(0), compressed.Length, recovered, recoffset);
        Assert2.assertArrayEquals(data, recovered);
    }

    /*
     *
     */
    [Fact]
    public void fastPforTest()
    {
        // proposed by Stefan Ackermann (https://github.com/Stivo)
        foreach (LongCODEC codec in codecs)
        {
            int N = FastPFOR.BLOCK_SIZE;
            long[] data = new long[N];
            for (int i = 0; i < N; i++)
                data[i] = 0;
            data[126] = -1;
            long[] comp = LongTestUtils.compress(codec, Arrays.copyOf(data, N));
            long[] answer = LongTestUtils.uncompress(codec, comp, N);
            for (int k = 0; k < N; ++k)
            {
                if (answer[k] != data[k])
                {
                    throw new InvalidOperationException("bug " + k + " " + answer[k] + " != " + data[k]);
                }
            }
        }
    }

    /*
     *
     */
    [Fact]
    public void fastPfor128Test()
    {
        // proposed by Stefan Ackermann (https://github.com/Stivo)
        foreach (LongCODEC codec in codecs)
        {
            int N = FastPFOR128.BLOCK_SIZE;
            long[] data = new long[N];
            for (int i = 0; i < N; i++)
                data[i] = 0;
            data[126] = -1;
            long[] comp = LongTestUtils.compress(codec, Arrays.copyOf(data, N));
            long[] answer = LongTestUtils.uncompress(codec, comp, N);
            for (int k = 0; k < N; ++k)
            {
                if (answer[k] != data[k])
                    throw new InvalidOperationException("bug " + k + " " + answer[k] + " != " + data[k]);
            }
        }
    }
}