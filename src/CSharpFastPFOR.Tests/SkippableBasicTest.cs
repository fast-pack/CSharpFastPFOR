/*
 * Just some basic sanity tests.
 *
 * @author Daniel Lemire
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.IntegerCompression.Vector;
using Genbox.CSharpFastPFOR.Tests.Utils;

namespace Genbox.CSharpFastPFOR.Tests;

public class SkippableBasicTest(ITestOutputHelper _testOutputHelper)
{
    private readonly SkippableIntegerCODEC[] codecs =
    [
        new JustCopy(),
        new VariableByte(),
        new SkippableComposition(new BinaryPacking(), new VariableByte()),
        new SkippableComposition(new NewPFD(), new VariableByte()),
        new SkippableComposition(new NewPFDS9(), new VariableByte()),
        new SkippableComposition(new NewPFDS16(), new VariableByte()),
        new SkippableComposition(new OptPFD(), new VariableByte()),
        new SkippableComposition(new OptPFDS9(), new VariableByte()),
        new SkippableComposition(new OptPFDS16(), new VariableByte()),
        new SkippableComposition(new FastPFOR128(), new VariableByte()),
        new SkippableComposition(new FastPFOR(), new VariableByte()),
        new Simple9(),
        new Simple16(),
        new SkippableComposition(new VectorBinaryPacking(), new VariableByte()),
        new SkippableComposition(new VectorFastPFOR(), new VariableByte())
    ];

    [Fact]
    public void consistentTest()
    {
        const int N = 4096;
        int[] data = new int[N];
        int[] rev = new int[N];
        for (int k = 0; k < N; ++k)
            data[k] = k % 128;
        foreach (SkippableIntegerCODEC c in codecs)
        {
            _testOutputHelper.WriteLine("[SkippeableBasicTest.consistentTest] codec = " + c);
            for (int n = 0; n <= N; ++n)
            {
                int[] outBuf = new int[c.maxHeadlessCompressedLength(new IntWrapper(0), n)];
                IntWrapper inPos = new IntWrapper();
                IntWrapper outPos = new IntWrapper();
                c.headlessCompress(data, inPos, n, outBuf, outPos);

                IntWrapper inPoso = new IntWrapper();
                IntWrapper outPoso = new IntWrapper();
                c.headlessUncompress(outBuf, inPoso, outPos.get(), rev,
                    outPoso, n);
                if (outPoso.get() != n)
                    throw new InvalidOperationException("bug " + n);
                if (inPoso.get() != outPos.get())
                    throw new InvalidOperationException("bug " + n + " " + inPoso.get() + " " + outPos.get());
                for (int j = 0; j < n; ++j)
                {
                    if (data[j] != rev[j])
                        throw new InvalidOperationException("bug");
                }
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
        foreach (SkippableIntegerCODEC c in codecs)
        {
            _testOutputHelper.WriteLine("[SkippeableBasicTest.varyingLengthTest] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug " + c + " " + k + " " + answer[k] + " " + data[k]);
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
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
        foreach (SkippableIntegerCODEC c in codecs)
        {
            _testOutputHelper.WriteLine("[SkippeableBasicTest.varyingLengthTest2] codec = " + c);

            if (c is Simple9)
                continue;

            if (c is GroupSimple9)
                continue;

            if (c is Simple16)
                continue;

            for (int L = 1; L <= 128; L++)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug at k = " + k + " " + answer[k] + " " + data[k] + " for " + c);
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                int[] comp = TestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                int[] answer = TestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
                }
            }
        }
    }

    [Fact]
    public void testMaxHeadlessCompressedLength()
    {
        MaxHeadlessCompressedLength(new IntegratedBinaryPacking(), 16 * IntegratedBinaryPacking.BLOCK_SIZE);
        MaxHeadlessCompressedLength(new IntegratedVariableByte(), 128);
        MaxHeadlessCompressedLength(new SkippableIntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()), (16 * IntegratedBinaryPacking.BLOCK_SIZE) + 10);

        MaxHeadlessCompressedLength(new BinaryPacking(), 16 * BinaryPacking.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new VariableByte(), 128, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new BinaryPacking(), new VariableByte()), (16 * BinaryPacking.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new JustCopy(), 128, 32);
        MaxHeadlessCompressedLength(new Simple9(), 128, 28);
        MaxHeadlessCompressedLength(new GroupSimple9(), 128, 28);
        MaxHeadlessCompressedLength(new Simple16(), 128, 28);
        MaxHeadlessCompressedLength(new OptPFD(), 4 * OptPFD.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new OptPFD(), new VariableByte()), (4 * OptPFD.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new OptPFDS9(), 4 * OptPFDS9.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new OptPFDS9(), new VariableByte()), (4 * OptPFDS9.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new OptPFDS16(), 4 * OptPFDS16.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new OptPFDS9(), new VariableByte()), (4 * OptPFDS16.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new NewPFD(), 4 * NewPFD.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new NewPFD(), new VariableByte()), (4 * NewPFD.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new NewPFDS9(), 4 * NewPFDS9.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new NewPFDS9(), new VariableByte()), (4 * NewPFDS9.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new NewPFDS16(), 4 * NewPFDS16.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new NewPFDS16(), new VariableByte()), (4 * NewPFDS16.BLOCK_SIZE) + 10, 32);

        int fastPfor128PageSize = FastPFOR128.BLOCK_SIZE * 4;
        MaxHeadlessCompressedLength(new FastPFOR128(fastPfor128PageSize), 2 * fastPfor128PageSize, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new FastPFOR128(fastPfor128PageSize), new VariableByte()), (2 * fastPfor128PageSize) + 10, 32);
        int fastPforPageSize = FastPFOR.BLOCK_SIZE * 4;
        MaxHeadlessCompressedLength(new FastPFOR(fastPforPageSize), 2 * fastPforPageSize, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new FastPFOR(fastPforPageSize), new VariableByte()), (2 * fastPforPageSize) + 10, 32);
        int vectorFastPforPageSize = VectorFastPFOR.BLOCK_SIZE * 4;
        MaxHeadlessCompressedLength(new VectorBinaryPacking(), 4 * VectorBinaryPacking.BLOCK_SIZE, 32);
        MaxHeadlessCompressedLength(new SkippableComposition(new VectorBinaryPacking(), new VariableByte()), (4 * VectorBinaryPacking.BLOCK_SIZE) + 10, 32);
        MaxHeadlessCompressedLength(new VectorFastPFOR(vectorFastPforPageSize, VectorBitPackerKernels.LaneWidth.BITS_128), 2 * vectorFastPforPageSize, 32);
    }

    private static void MaxHeadlessCompressedLength(SkippableIntegratedIntegerCODEC codec, int inlengthTo)
    {
        int delta = -1;
        int value = delta;

        for (int inlength = 0; inlength < inlengthTo; ++inlength)
        {
            int[] input = new int[inlength];
            for (int i = 0; i < inlength; i++)
            {
                input[i] = value;
                value += delta;
            }

            int maxOutputLength = codec.maxHeadlessCompressedLength(new IntWrapper(), inlength);
            int[] output = new int[maxOutputLength];
            IntWrapper outPos = new IntWrapper();

            codec.headlessCompress(input, new IntWrapper(), inlength, output, outPos, new IntWrapper());
            Assert.True(maxOutputLength <= outPos.get() + 1);
        }
    }

    private static void MaxHeadlessCompressedLength(SkippableIntegerCODEC codec, int inlengthTo, int maxBitWidth)
    {
        Assert.True(maxBitWidth >= 20);
        int[] regularValueBitWidths = [20, maxBitWidth - 1];

        for (int inlength = 0; inlength < inlengthTo; ++inlength)
        {
            int[] input = new int[inlength];

            int maxOutputLength = codec.maxHeadlessCompressedLength(new IntWrapper(), inlength);
            int[] output = new int[maxOutputLength];

            for (int exceptionCount = 0; exceptionCount < inlength; exceptionCount++)
            {
                int exception = maxBitWidth == 32 ? -1 : (1 << maxBitWidth) - 1;

                foreach (int regularValueBitWidth in regularValueBitWidths)
                {
                    int regularValue = regularValueBitWidth == 32 ? -1 : (1 << regularValueBitWidth) - 1;

                    Array.Fill(input, exception, 0, exceptionCount);
                    Array.Fill(input, regularValue, exceptionCount, input.Length - exceptionCount);

                    codec.headlessCompress(input, new IntWrapper(), inlength, output, new IntWrapper());
                }
            }
        }
    }

    [Fact]
    public void testUncompressOutputOffset_SkippableComposition()
    {
        foreach (int offset in new[] { 0, 1, 6 })
        {
            SkippableComposition codec = new SkippableComposition(new BinaryPacking(), new VariableByte());

            int[] input = [2, 3, 4, 5];
            int[] compressed = new int[codec.maxHeadlessCompressedLength(new IntWrapper(0), input.Length)];
            int[] uncompressed = new int[offset + input.Length];

            IntWrapper inputOffset = new IntWrapper(0);
            IntWrapper compressedOffset = new IntWrapper(0);

            codec.headlessCompress(input, inputOffset, input.Length, compressed, compressedOffset);

            int compressedLength = compressedOffset.get();
            IntWrapper uncompressedOffset = new IntWrapper(offset);
            compressedOffset = new IntWrapper(0);
            codec.headlessUncompress(compressed, compressedOffset, compressedLength, uncompressed, uncompressedOffset, input.Length);

            Assert.Equal(input, uncompressed.Skip(offset).Take(input.Length).ToArray());
        }
    }

    [Fact]
    public void testUncompressOutputOffset_SkippableIntegratedComposition()
    {
        foreach (int offset in new[] { 0, 1, 6 })
        {
            SkippableIntegratedComposition codec = new SkippableIntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte());

            int[] input = [2, 3, 4, 5];
            int[] compressed = new int[codec.maxHeadlessCompressedLength(new IntWrapper(0), input.Length)];
            int[] uncompressed = new int[offset + input.Length];

            IntWrapper inputOffset = new IntWrapper(0);
            IntWrapper compressedOffset = new IntWrapper(0);
            IntWrapper initValue = new IntWrapper(0);

            codec.headlessCompress(input, inputOffset, input.Length, compressed, compressedOffset, initValue);

            int compressedLength = compressedOffset.get();
            IntWrapper uncompressedOffset = new IntWrapper(offset);
            compressedOffset = new IntWrapper(0);
            initValue = new IntWrapper(0);
            codec.headlessUncompress(compressed, compressedOffset, compressedLength, uncompressed, uncompressedOffset, input.Length, initValue);

            Assert.Equal(input, uncompressed.Skip(offset).Take(input.Length).ToArray());
        }
    }
}