/**
 * Just some basic sanity tests.
 *
 * @author Daniel Lemire
 */

using Genbox.CSharpFastPFOR.Differential;
using Genbox.CSharpFastPFOR.Port;
using Genbox.CSharpFastPFOR.Synth;
using Genbox.CSharpFastPFOR.Tests.Port;
using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class BasicTest(ITestOutputHelper testOutputHelper)
{
    private readonly IntegerCODEC[] codecs =
    [
        new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()),
        new JustCopy(),
        new VariableByte(),
        new IntegratedVariableByte(),
        new Composition(new BinaryPacking(), new VariableByte()),
        new Composition(new NewPFD(), new VariableByte()),
        new Composition(new NewPFDS16(), new VariableByte()),
        new Composition(new OptPFDS9(), new VariableByte()),
        new Composition(new OptPFDS16(), new VariableByte()),
        new Composition(new FastPFOR128(), new VariableByte()),
        new Composition(new FastPFOR(), new VariableByte()),
        new Simple9(),
        new Simple16(),
        new Composition(new XorBinaryPacking(), new VariableByte()),
        new Composition(new DeltaZigzagBinaryPacking(), new DeltaZigzagVariableByte())
    ];

    [Fact]
    public void saulTest()
    {
        foreach (IntegerCODEC C in codecs)
        {
            for (int x = 0; x < 50; ++x)
            {
                int[] a = [2, 3, 4, 5];
                int[] b = new int[90];
                int[] c = new int[a.Length];

                IntWrapper aOffset = new IntWrapper(0);
                IntWrapper bOffset = new IntWrapper(x);
                C.compress(a, aOffset, a.Length, b, bOffset);
                int len = bOffset.get() - x;

                bOffset.set(x);
                IntWrapper cOffset = new IntWrapper(0);
                C.uncompress(b, bOffset, len, c, cOffset);
                if (!Arrays.equals(a, c))
                {
                    testOutputHelper.WriteLine("Problem with " + C);
                }
                Assert2.assertArrayEquals(a, c);
            }
        }
    }

    [Fact]
    public void varyingLengthTest()
    {
        const int N = 4096;
        int[] data = new int[N];
        for (int k = 0; k < N; ++k)
            data[k] = k;
        foreach (IntegerCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[BasicTest.varyingLengthTest] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                int[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug");
            }
            for (int L = 128; L <= N; L *= 2)
            {
                int[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                    {
                        testOutputHelper.WriteLine(Arrays.toString(Arrays.copyOf(answer, L)));
                        testOutputHelper.WriteLine(Arrays.toString(Arrays.copyOf(data, L)));
                        throw new Exception("bug");
                    }
            }
        }
    }

    [Fact]
    public void varyingLengthTest2()
    {
        const int N = 128;
        int[] data = new int[N];
        data[127] = -1;
        foreach (IntegerCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[BasicTest.varyingLengthTest2] codec = " + c);

            if (c is Simple9)
                continue;

            if (c is Simple16)
                continue;

            for (int L = 1; L <= 128; L++)
            {
                int[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug");
            }
            for (int L = 128; L <= N; L *= 2)
            {
                int[] comp = TestUtils.compress(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompress(c, comp, L);
                for (int k = 0; k < L; ++k)
                    if (answer[k] != data[k])
                        throw new Exception("bug");
            }
        }
    }

    [Fact]
    public void checkDeltaZigzagVB()
    {
        DeltaZigzagVariableByte codec = new DeltaZigzagVariableByte();
        DeltaZigzagVariableByte codeco = new DeltaZigzagVariableByte();

        testZeroInZeroOut(codec);
        test(codec, codeco, 5, 10);
        test(codec, codeco, 5, 14);
        test(codec, codeco, 2, 18);
    }

    [Fact]
    public void checkDeltaZigzagPacking()
    {
        DeltaZigzagBinaryPacking codec = new DeltaZigzagBinaryPacking();
        testZeroInZeroOut(codec);
        testSpurious(codec);

        IntegerCODEC compo = new Composition(new DeltaZigzagBinaryPacking(), new VariableByte());
        IntegerCODEC compo2 = new Composition(new DeltaZigzagBinaryPacking(), new VariableByte());

        testZeroInZeroOut(compo);
        testUnsorted(compo);
        test(compo, compo2, 5, 10);
        test(compo, compo2, 5, 14);
        test(compo, compo2, 2, 18);
    }

    [Fact]
    public void checkXorBinaryPacking()
    {
        testZeroInZeroOut(new XorBinaryPacking());
        testSpurious(new XorBinaryPacking());
    }

    [Fact]
    public void checkXorBinaryPacking1()
    {
        IntegerCODEC c = new IntegratedComposition(new XorBinaryPacking(),
            new IntegratedVariableByte());
        testZeroInZeroOut(c);
    }

    [Fact]
    public void checkXorBinaryPacking2()
    {
        IntegerCODEC c = new IntegratedComposition(new XorBinaryPacking(),
            new IntegratedVariableByte());
        testUnsorted(c);
    }

    [Fact]
    public void checkXorBinaryPacking3()
    {
        IntegerCODEC c = new IntegratedComposition(new XorBinaryPacking(), new IntegratedVariableByte());
        IntegerCODEC co = new IntegratedComposition(new XorBinaryPacking(), new IntegratedVariableByte());
        test(c, co, 5, 10);
        test(c, co, 5, 14);
        test(c, co, 2, 18);
    }

    [Fact]
    public void verifyBitPacking()
    {
        const int N = 32;
        const int TIMES = 1000;
        Random r = new Random();
        int[] data = new int[N];
        int[] compressed = new int[N];
        int[] uncompressed = new int[N];
        for (int bit = 0; bit < 31; ++bit)
        {
            for (int t = 0; t < TIMES; ++t)
            {
                for (int k = 0; k < N; ++k)
                {
                    data[k] = r.Next(1 << bit);
                }
                BitPacking.fastpack(data, 0, compressed, 0, bit);
                BitPacking.fastunpack(compressed, 0, uncompressed, 0, bit);
                Assert2.assertArrayEquals(uncompressed, data);
            }
        }
    }

    [Fact]
    public void verifyWithoutMask()
    {
        const int N = 32;
        const int TIMES = 1000;
        Random r = new Random();
        int[] data = new int[N];
        int[] compressed = new int[N];
        int[] uncompressed = new int[N];
        for (int bit = 0; bit < 31; ++bit)
        {
            for (int t = 0; t < TIMES; ++t)
            {
                for (int k = 0; k < N; ++k)
                {
                    data[k] = r.Next(1 << bit);
                }
                BitPacking.fastpackwithoutmask(data, 0, compressed, 0, bit);
                BitPacking.fastunpack(compressed, 0, uncompressed, 0, bit);
                Assert2.assertArrayEquals(uncompressed, data);
            }
        }
    }

    /**
     * @param array
     *            some array
     * @param mask
     *            provided mask
     */

    private static void maskArray(int[] array, int mask)
    {
        for (int i = 0, end = array.Length; i < end; ++i)
        {
            array[i] &= mask;
        }
    }

    /**
     * Verify bitpacking with exception.
     */
    [Fact]
    public void verifyWithExceptions()
    {
        const int N = 32;
        const int TIMES = 1000;
        Random r = new Random();
        int[] data = new int[N];
        int[] compressed = new int[N];
        int[] uncompressed = new int[N];
        for (int bit = 0; bit < 31; ++bit)
        {
            for (int t = 0; t < TIMES; ++t)
            {
                for (int k = 0; k < N; ++k)
                {
                    data[k] = r.Next();
                }
                BitPacking.fastpack(data, 0, compressed, 0, bit);
                BitPacking.fastunpack(compressed, 0, uncompressed, 0, bit);

                // Check assertions.
                maskArray(data, ((1 << bit) - 1));
                Assert2.assertArrayEquals(data, uncompressed);
            }
        }
    }

    /**
     * check that the codecs can be inverted.
     */
    [Fact]
    public void basictest()
    {
        test(5, 10);
        test(5, 14);
        test(2, 18);
    }

    /**
     * check that there is no spurious output.
     */
    [Fact]
    public void spuriousouttest()
    {
        testSpurious(new IntegratedBinaryPacking());
        testSpurious(new BinaryPacking());
        testSpurious(new NewPFD());
        testSpurious(new NewPFDS9());
        testSpurious(new NewPFDS16());
        testSpurious(new OptPFD());
        testSpurious(new OptPFDS9());
        testSpurious(new OptPFDS16());
        testSpurious(new FastPFOR128());
        testSpurious(new FastPFOR());
    }

    /**
     * check that an empty array generates an empty array after compression.
     */
    [Fact]
    public void zeroinzerouttest()
    {
        testZeroInZeroOut(new IntegratedBinaryPacking());
        testZeroInZeroOut(new IntegratedVariableByte());
        testZeroInZeroOut(new BinaryPacking());
        testZeroInZeroOut(new NewPFD());
        testZeroInZeroOut(new NewPFDS9());
        testZeroInZeroOut(new NewPFDS16());
        testZeroInZeroOut(new OptPFD());
        testZeroInZeroOut(new OptPFDS9());
        testZeroInZeroOut(new OptPFDS16());
        testZeroInZeroOut(new FastPFOR128());
        testZeroInZeroOut(new FastPFOR());
        testZeroInZeroOut(new VariableByte());
        testZeroInZeroOut(new Composition(new IntegratedBinaryPacking(), new VariableByte()));
        testZeroInZeroOut(new Composition(new BinaryPacking(), new VariableByte()));
        testZeroInZeroOut(new Composition(new NewPFD(), new VariableByte()));
        testZeroInZeroOut(new Composition(new NewPFDS9(), new VariableByte()));
        testZeroInZeroOut(new Composition(new NewPFDS16(), new VariableByte()));
        testZeroInZeroOut(new Composition(new OptPFD(), new VariableByte()));
        testZeroInZeroOut(new Composition(new OptPFDS9(), new VariableByte()));
        testZeroInZeroOut(new Composition(new OptPFDS16(), new VariableByte()));
        testZeroInZeroOut(new Composition(new FastPFOR128(), new VariableByte()));
        testZeroInZeroOut(new Composition(new FastPFOR(), new VariableByte()));

        testZeroInZeroOut(new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()));
    }

    private static void testSpurious(IntegerCODEC c)
    {
        int[] x = new int[1024];
        int[] y = [];
        IntWrapper i0 = new IntWrapper(0);
        IntWrapper i1 = new IntWrapper(0);
        for (int inlength = 0; inlength < 32; ++inlength)
        {
            c.compress(x, i0, inlength, y, i1);
            Assert2.assertEquals(0, i1.intValue());
        }
    }

    private static void testZeroInZeroOut(IntegerCODEC c)
    {
        int[] x = [];
        int[] y = [];
        IntWrapper i0 = new IntWrapper(0);
        IntWrapper i1 = new IntWrapper(0);
        c.compress(x, i0, 0, y, i1);
        Assert2.assertEquals(0, i1.intValue());

        int[] @out = [];
        IntWrapper outpos = new IntWrapper(0);
        c.uncompress(y, i1, 0, @out, outpos);
        Assert2.assertEquals(0, outpos.intValue());
    }

    private static void test(IntegerCODEC c, IntegerCODEC co, int N, int nbr)
    {
        ClusteredDataGenerator cdg = new ClusteredDataGenerator();
        for (int sparsity = 1; sparsity < 31 - nbr; sparsity += 4)
        {
            int[][] data = new int[N][];
            int max = (1 << (nbr + 9));
            for (int k = 0; k < N; ++k)
            {
                data[k] = cdg.generateClustered((1 << nbr), max);
            }

            testCodec(c, co, data, max);
        }
    }

    private void test(int N, int nbr)
    {
        ClusteredDataGenerator cdg = new ClusteredDataGenerator();
        testOutputHelper.WriteLine("[BasicTest.test] N = " + N + " " + nbr);
        for (int sparsity = 1; sparsity < 31 - nbr; sparsity += 4)
        {
            int[][] data = new int[N][];
            int max = (1 << (nbr + sparsity));
            for (int k = 0; k < N; ++k)
            {
                data[k] = cdg.generateClustered((1 << nbr), max);
            }

            testCodec(new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()), new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()), data, max);
            testCodec(new JustCopy(), new JustCopy(), data, max);
            testCodec(new VariableByte(), new VariableByte(), data, max);
            testCodec(new IntegratedVariableByte(), new IntegratedVariableByte(), data, max);
            testCodec(new Composition(new BinaryPacking(), new VariableByte()), new Composition(new BinaryPacking(), new VariableByte()), data, max);
            testCodec(new Composition(new NewPFD(), new VariableByte()), new Composition(new NewPFD(), new VariableByte()), data, max);
            testCodec(new Composition(new NewPFDS9(), new VariableByte()), new Composition(new NewPFDS9(), new VariableByte()), data, max);
            testCodec(new Composition(new NewPFDS16(), new VariableByte()), new Composition(new NewPFDS16(), new VariableByte()), data, max);
            testCodec(new Composition(new OptPFD(), new VariableByte()), new Composition(new OptPFD(), new VariableByte()), data, max);
            testCodec(new Composition(new OptPFDS9(), new VariableByte()), new Composition(new OptPFDS9(), new VariableByte()), data, max);
            testCodec(new Composition(new OptPFDS16(), new VariableByte()), new Composition(new OptPFDS16(), new VariableByte()), data, max);
            testCodec(new Composition(new FastPFOR128(), new VariableByte()), new Composition(new FastPFOR128(), new VariableByte()), data, max);
            testCodec(new Composition(new FastPFOR(), new VariableByte()), new Composition(new FastPFOR(), new VariableByte()), data, max);
            testCodec(new Simple9(), new Simple9(), data, max);
        }
    }

    private static void testCodec(IntegerCODEC c, IntegerCODEC co, int[][] data, int max)
    {
        int N = data.Length;
        int maxlength = 0;
        for (int k = 0; k < N; ++k)
        {
            if (data[k].Length > maxlength)
                maxlength = data[k].Length;
        }
        int[] buffer = new int[maxlength + 1024];
        int[] dataout = new int[4 * maxlength + 1024];
        // 4x + 1024 to account for the possibility of some negative
        // compression.
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();
        for (int k = 0; k < N; ++k)
        {
            int[] backupdata = Arrays.copyOf(data[k], data[k].Length);

            inpos.set(1);
            outpos.set(0);

            if (!(c is IntegratedIntegerCODEC))
                Delta.delta(backupdata);

            c.compress(backupdata, inpos, backupdata.Length - inpos.get(), dataout, outpos);

            int thiscompsize = outpos.get() + 1;
            inpos.set(0);
            outpos.set(1);
            buffer[0] = backupdata[0];
            co.uncompress(dataout, inpos, thiscompsize - 1, buffer, outpos);

            if (!(c is IntegratedIntegerCODEC))
                Delta.fastinverseDelta(buffer);

            // Check assertions.
            Assert2.assertEquals(outpos.get(), data[k].Length); //"length is not match"
            int[] bufferCutout = Arrays.copyOf(buffer, outpos.get());
            Assert2.assertArrayEquals(data[k], bufferCutout); //"failed to reconstruct original data"
        }
    }

    /**
     * Test for unsorted array.
     */
    [Fact]
    public void testUnsortedExample()
    {
        testUnsorted(new VariableByte());
        testUnsorted(new IntegratedVariableByte());
        testUnsorted(new Composition(new BinaryPacking(), new VariableByte()));
        testUnsorted(new Composition(new NewPFD(), new VariableByte()));
        testUnsorted(new Composition(new NewPFDS9(), new VariableByte()));
        testUnsorted(new Composition(new NewPFDS16(), new VariableByte()));
        testUnsorted(new Composition(new OptPFD(), new VariableByte()));
        testUnsorted(new Composition(new OptPFDS9(), new VariableByte()));
        testUnsorted(new Composition(new OptPFDS16(), new VariableByte()));
        testUnsorted(new Composition(new FastPFOR128(), new VariableByte()));
        testUnsorted(new Composition(new FastPFOR(), new VariableByte()));

        testUnsorted(new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()));
        testUnsorted(new Composition(new IntegratedBinaryPacking(), new VariableByte()));

        testUnsorted2(new VariableByte());
        testUnsorted2(new IntegratedVariableByte());
        testUnsorted2(new Composition(new BinaryPacking(), new VariableByte()));
        testUnsorted2(new Composition(new NewPFD(), new VariableByte()));
        testUnsorted2(new Composition(new NewPFDS9(), new VariableByte()));
        testUnsorted2(new Composition(new NewPFDS16(), new VariableByte()));
        testUnsorted2(new Composition(new OptPFD(), new VariableByte()));
        testUnsorted2(new Composition(new OptPFDS9(), new VariableByte()));
        testUnsorted2(new Composition(new OptPFDS16(), new VariableByte()));
        testUnsorted2(new Composition(new FastPFOR128(), new VariableByte()));
        testUnsorted2(new Composition(new FastPFOR(), new VariableByte()));

        testUnsorted3(new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()));
        testUnsorted3(new Composition(new IntegratedBinaryPacking(), new VariableByte()));
        testUnsorted3(new VariableByte());
        testUnsorted3(new IntegratedVariableByte());
        testUnsorted3(new Composition(new BinaryPacking(), new VariableByte()));
        testUnsorted3(new Composition(new NewPFD(), new VariableByte()));
        testUnsorted3(new Composition(new NewPFDS9(), new VariableByte()));
        testUnsorted3(new Composition(new NewPFDS16(), new VariableByte()));
        testUnsorted3(new Composition(new OptPFD(), new VariableByte()));
        testUnsorted3(new Composition(new OptPFDS9(), new VariableByte()));
        testUnsorted3(new Composition(new OptPFDS16(), new VariableByte()));
        testUnsorted3(new Composition(new FastPFOR128(), new VariableByte()));
        testUnsorted3(new Composition(new FastPFOR(), new VariableByte()));

        testUnsorted2(new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()));
        testUnsorted2(new Composition(new IntegratedBinaryPacking(), new VariableByte()));

    }

    public void testUnsorted(IntegerCODEC codec)
    {
        int[] lengths = [133, 1026, 1333333];
        foreach (int N in lengths)
        {
            int[] data = new int[N];
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
            int[] compressed = new int[(int)Math.Ceiling(N * 1.01) + 1024];
            IntWrapper inputoffset = new IntWrapper(0);
            IntWrapper outputoffset = new IntWrapper(0);
            codec.compress(data, inputoffset, data.Length, compressed,
                outputoffset);
            // we can repack the data: (optional)
            compressed = Arrays.copyOf(compressed, outputoffset.intValue());

            int[] recovered = new int[N];
            IntWrapper recoffset = new IntWrapper(0);
            codec.uncompress(compressed, new IntWrapper(0), compressed.Length,
                recovered, recoffset);
            Assert2.assertArrayEquals(data, recovered);
        }
    }

    private void testUnsorted2(IntegerCODEC codec)
    {
        int[] data = new int[128];
        data[5] = -1;
        int[] compressed = new int[1024];
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        int[] recovered = new int[128];
        IntWrapper recoffset = new IntWrapper(0);
        codec.uncompress(compressed, new IntWrapper(0), compressed.Length,
            recovered, recoffset);
        Assert2.assertArrayEquals(data, recovered);
    }

    private void testUnsorted3(IntegerCODEC codec)
    {
        int[] data = new int[128];
        data[127] = -1;
        int[] compressed = new int[1024];
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        int[] recovered = new int[128];
        IntWrapper recoffset = new IntWrapper(0);
        codec.uncompress(compressed, new IntWrapper(0), compressed.Length,
            recovered, recoffset);
        Assert2.assertArrayEquals(data, recovered);
    }

    [Fact]
    public void fastPforTest()
    {
        // proposed by Stefan Ackermann (https://github.com/Stivo)
        FastPFOR codec1 = new FastPFOR();
        FastPFOR codec2 = new FastPFOR();
        const int N = FastPFOR.BLOCK_SIZE;
        int[] data = new int[N];
        for (int i = 0; i < N; i++)
            data[i] = 0;
        data[126] = -1;
        int[] comp = TestUtils.compress(codec1, Arrays.copyOf(data, N));
        int[] answer = TestUtils.uncompress(codec2, comp, N);
        for (int k = 0; k < N; ++k)
            if (answer[k] != data[k])
                throw new Exception("bug " + k + " " + answer[k]
                                    + " != " + data[k]);
    }

    [Fact]
    public void fastPfor128Test()
    {
        // proposed by Stefan Ackermann (https://github.com/Stivo)
        FastPFOR128 codec1 = new FastPFOR128();
        FastPFOR128 codec2 = new FastPFOR128();
        const int N = FastPFOR128.BLOCK_SIZE;
        int[] data = new int[N];
        for (int i = 0; i < N; i++)
            data[i] = 0;
        data[126] = -1;
        int[] comp = TestUtils.compress(codec1, Arrays.copyOf(data, N));
        int[] answer = TestUtils.uncompress(codec2, comp, N);
        for (int k = 0; k < N; ++k)
            if (answer[k] != data[k])
                throw new Exception("bug " + k + " " + answer[k]
                                    + " != " + data[k]);
    }
}