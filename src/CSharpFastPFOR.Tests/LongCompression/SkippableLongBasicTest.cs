/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression;

/*
 * Just some basic sanity tests.
 *
 * @author Benoit Lacelle
 */
public class SkippableLongBasicTest(ITestOutputHelper testOutputHelper)
{
    private readonly SkippableLongCODEC[] codecs =
    [
        new LongJustCopy(),
        new LongVariableByte(),
        new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte())
    ];

    /*
     *
     */
    [Fact]
    public void consistentTest()
    {
        const int N = 4096;
        long[] data = new long[N];
        long[] rev = new long[N];
        for (int k = 0; k < N; ++k)
            data[k] = k % 128;
        foreach (SkippableLongCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[SkippeableBasicTest.consistentTest] codec = " + c);
            for (int n = 0; n <= N; ++n)
            {
                IntWrapper inPos = new IntWrapper();
                IntWrapper outPos = new IntWrapper();
                long[] outBuf = new long[c.maxHeadlessCompressedLength(new IntWrapper(0), n)];

                c.headlessCompress(data, inPos, n, outBuf, outPos);

                IntWrapper inPoso = new IntWrapper();
                IntWrapper outPoso = new IntWrapper();
                c.headlessUncompress(outBuf, inPoso, outPos.get(), rev, outPoso, n);
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
        foreach (SkippableLongCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[SkippeableBasicTest.varyingLengthTest] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                long[] comp = LongTestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug " + c + " " + k + " " + answer[k] + " " + data[k]);
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                long[] comp = LongTestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompressHeadless(c, comp, L);
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
    public void varyingLengthTest2()
    {
        const int N = 128;
        long[] data = new long[N];
        data[127] = -1;
        foreach (SkippableLongCODEC c in codecs)
        {
            testOutputHelper.WriteLine("[SkippeableBasicTest.varyingLengthTest2] codec = " + c);
            for (int L = 1; L <= 128; L++)
            {
                long[] comp = LongTestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("L=" + L + ": bug at k = " + k + " " + answer[k] + " " + data[k] + " for " + c);
                }
            }
            for (int L = 128; L <= N; L *= 2)
            {
                long[] comp = LongTestUtils.compressHeadless(c, Arrays.copyOf(data, L));
                long[] answer = LongTestUtils.uncompressHeadless(c, comp, L);
                for (int k = 0; k < L; ++k)
                {
                    if (answer[k] != data[k])
                        throw new InvalidOperationException("bug");
                }
            }
        }
    }

    [Fact]
    public void testMaxHeadlessCompressedLengthForAllCodecs()
    {
        testMaxHeadlessCompressedLength(new LongJustCopy(), 128);
        testMaxHeadlessCompressedLength(new LongBinaryPacking(), 16 * LongBinaryPacking.BLOCK_SIZE);
        testMaxHeadlessCompressedLength(new LongVariableByte(), 128);
        testMaxHeadlessCompressedLength(new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte()), (16 * LongBinaryPacking.BLOCK_SIZE) + 10);
    }

    private static void testMaxHeadlessCompressedLength(SkippableLongCODEC codec, int inlengthTo)
    {
        for (int inlength = 0; inlength < inlengthTo; ++inlength)
        {
            long[] input = new long[inlength];
            Arrays.fill(input, -1L);

            int maxOutputLength = codec.maxHeadlessCompressedLength(new IntWrapper(), inlength);
            long[] output = new long[maxOutputLength];
            IntWrapper outPos = new IntWrapper();

            codec.headlessCompress(input, new IntWrapper(), inlength, output, outPos);
            // If we reach this point, no exception was thrown, which means the calculated output length was sufficient.

            Assert.True(maxOutputLength <= outPos.get() + 1); // +1 because SkippableLongComposition always adds one extra integer for the potential header
        }
    }

    [Fact]
    public void testUncompressOutputOffset_SkippableLongComposition()
    {
        foreach (int offset in new[] { 0, 1, 6 })
        {
            SkippableLongComposition codec = new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte());

            long[] input = [2, 3, 4, 5];
            long[] compressed = new long[codec.maxHeadlessCompressedLength(new IntWrapper(0), input.Length)];
            long[] uncompressed = new long[offset + input.Length];

            IntWrapper inputOffset = new IntWrapper(0);
            IntWrapper compressedOffset = new IntWrapper(0);

            codec.headlessCompress(input, inputOffset, input.Length, compressed, compressedOffset);

            int compressedLength = compressedOffset.get();
            IntWrapper uncompressedOffset = new IntWrapper(offset);
            compressedOffset = new IntWrapper(0);
            codec.headlessUncompress(compressed, compressedOffset, compressedLength, uncompressed, uncompressedOffset, input.Length);

            Assert2.assertArrayEquals(input, Arrays.copyOfRange(uncompressed, offset, offset + input.Length));
        }
    }
}