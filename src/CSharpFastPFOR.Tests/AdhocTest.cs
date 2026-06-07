/*
 * Collection of adhoc tests.
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Vector;
using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class AdhocTest
{
    [Fact]
    public void testIssue59()
    {
        FastPFOR128 fastpfor = new FastPFOR128();

        const int N = 9984;
        int[] data = new int[N];
        for (int i = 0; i < N; i += 150)
            data[i] = i;

        int[] compressedoutput1 = new int[N + 1024];

        IntWrapper inputoffset1 = new IntWrapper(0);
        IntWrapper outputoffset1 = new IntWrapper(0);

        fastpfor.compress(data, inputoffset1, N, compressedoutput1, outputoffset1);
        int compressedsize1 = outputoffset1.get();

        int[] recovered1 = new int[N];
        inputoffset1 = new IntWrapper(0);
        outputoffset1 = new IntWrapper(0);
        fastpfor.uncompress(compressedoutput1, outputoffset1, compressedsize1, recovered1, inputoffset1);
        Assert.Equal(data, recovered1);

        int[] compressedoutput2 = new int[N + 1024];

        IntWrapper inputoffset2 = new IntWrapper(0);
        IntWrapper outputoffset2 = new IntWrapper(0);

        fastpfor.compress(data, inputoffset2, N, compressedoutput2, outputoffset2);
        int compressedsize2 = outputoffset2.get();

        int[] recovered2 = new int[N];
        inputoffset2 = new IntWrapper(0);
        outputoffset2 = new IntWrapper(0);
        fastpfor.uncompress(compressedoutput2, outputoffset2, compressedsize2, recovered2, inputoffset2);
        Assert.Equal(data, recovered2);
    }

    [Fact]
    public void testIssue29()
    {
        for (int x = 0; x < 64; x++)
        {
            int[] a = [2, 3, 4, 5];
            int[] b = new int[90];
            int[] c = new int[a.Length];
            Composition codec = new Composition(new BinaryPacking(), new VariableByte());

            IntWrapper aOffset = new IntWrapper(0);
            IntWrapper bOffset = new IntWrapper(x);
            codec.compress(a, aOffset, a.Length, b, bOffset);
            int len = bOffset.get() - x;
            bOffset.set(x);
            IntWrapper cOffset = new IntWrapper(0);
            codec.uncompress(b, bOffset, len, c, cOffset);
            Assert.Equal(a, c);
        }
    }

    [Fact]
    public void testIssue29b()
    {
        for (int x = 0; x < 64; x++)
        {
            SkippableComposition codec = new SkippableComposition(new BinaryPacking(), new VariableByte());
            int[] a = [2, 3, 4, 5];
            int[] b = new int[x + codec.maxHeadlessCompressedLength(new IntWrapper(0), a.Length)];
            int[] c = new int[a.Length];
            IntWrapper aOffset = new IntWrapper(0);
            IntWrapper bOffset = new IntWrapper(x);
            codec.headlessCompress(a, aOffset, a.Length, b, bOffset);
            int len = bOffset.get() - x;
            bOffset.set(x);
            IntWrapper cOffset = new IntWrapper(0);
            codec.headlessUncompress(b, bOffset, len, c, cOffset, a.Length);
            Assert.Equal(a, c);
        }
    }

    [Fact]
    public void testIssue41()
    {
        for (int x = 0; x < 64; x++)
        {
            SkippableIntegratedComposition codec = new SkippableIntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte());
            int[] a = [2, 3, 4, 5];
            int[] b = new int[x + codec.maxHeadlessCompressedLength(new IntWrapper(0), a.Length)];
            int[] c = new int[a.Length];
            IntWrapper aOffset = new IntWrapper(0);
            IntWrapper bOffset = new IntWrapper(x);
            IntWrapper initValue = new IntWrapper(0);

            codec.headlessCompress(a, aOffset, a.Length, b, bOffset, initValue);
            int len = bOffset.get() - x;
            bOffset.set(x);
            IntWrapper cOffset = new IntWrapper(0);
            initValue = new IntWrapper(0);
            codec.headlessUncompress(b, bOffset, len, c, cOffset, a.Length, initValue);
            Assert.Equal(a, c);
        }
    }

    [Fact]
    public void s9UsesUnsignedSelectorComparison()
    {
        int[] input = [int.MinValue];
        int[] output = new int[1];

        int length = S9.compress(input, 0, input.Length, output, 0);

        Assert.Equal(1, length);
        Assert.Equal(8, (int)((uint)output[0] >>> 28));
    }

    [Fact]
    public void integratedBitPackingSpillsHighBitDeltasLogically()
    {
        int[] input = new int[32];
        for (int i = 3; i < input.Length; i++)
            input[i] = int.MinValue;

        int[] output = new int[10];

        IntegratedBitPacking.integratedpack(0, input, 0, output, 0, 10);

        Assert.Equal(unchecked(0x20000000), output[1]);
    }

    [Fact]
    public void groupSimple9UsesJavaPairedBlockLayout()
    {
        int[] input = new int[57];
        Array.Fill(input, 4, 0, 9); // selector 2: 9 values at 3 bits each.
        Array.Fill(input, 1, 9, 28); // selector 0: 28 values at 1 bit each.

        int[] output = new int[64];
        GroupSimple9 codec = new GroupSimple9();
        codec.headlessCompress(input, new IntWrapper(), input.Length, output, new IntWrapper());

        Assert.Equal(0x12924924, output[0]);
        Assert.Equal(0x4FFFFFFF, output[1]);
    }

    [Fact]
    public void longBitPackingSpillsNegativeValuesArithmetically()
    {
        long[] input = new long[64];
        Array.Fill(input, -1L);
        long[] output = new long[8];

        LongBitPacking.fastpackwithoutmask(input, 0, output, 0, 5);

        Assert.Equal(-1L, output[1]);
    }

    [Fact]
    public void vectorFastPforRoundTripsVectorBlocks()
    {
        int[] input = new int[VectorFastPFOR.BLOCK_SIZE * 2];
        for (int i = 0; i < input.Length; i++)
            input[i] = i % 257;

        int[] compressed = new int[input.Length * 4];
        int[] recovered = new int[input.Length];
        VectorFastPFOR codec = new VectorFastPFOR();

        IntWrapper inputOffset = new IntWrapper();
        IntWrapper compressedOffset = new IntWrapper();
        codec.compress(input, inputOffset, input.Length, compressed, compressedOffset);

        codec.uncompress(compressed, new IntWrapper(), compressedOffset.get(), recovered, new IntWrapper());

        Assert.Equal(input, recovered);
    }

    [Fact]
    public void biggerCompressedArray0()
    {
        // No problem: for comparison.
        IntegerCODEC c = new Composition(new FastPFOR128(), new VariableByte());
        TestUtils.assertSymmetry(c, 0, 16384);
        c = new Composition(new FastPFOR(), new VariableByte());
        TestUtils.assertSymmetry(c, 0, 16384);
    }

    [Fact]
    public void biggerCompressedArray1()
    {
        // Compressed array is bigger than original, because of VariableByte.
        IntegerCODEC c = new VariableByte();
        TestUtils.assertSymmetry(c, -1);
    }

    [Fact]
    public void biggerCompressedArray2()
    {
        // Compressed array is bigger than original, because of Composition.
        IntegerCODEC c = new Composition(new FastPFOR128(), new VariableByte());
        TestUtils.assertSymmetry(c, 65535, 65535);
        c = new Composition(new FastPFOR(), new VariableByte());
        TestUtils.assertSymmetry(c, 65535, 65535);
    }
}