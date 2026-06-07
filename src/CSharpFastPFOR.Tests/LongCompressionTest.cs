using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.LongCompression;
using Genbox.CSharpFastPFOR.LongCompression.Differential;
using Genbox.CSharpFastPFOR.Tests.LongCompression;
using Genbox.CSharpFastPFOR.Tests.LongCompression.Synth;
using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests;

public class LongCompressionTest
{
    private readonly LongCODEC[] _codecs =
    [
        new LongJustCopy(),
        new LongVariableByte(),
        new LongAs2IntsCodec(),
        new LongComposition(new LongBinaryPacking(), new LongVariableByte())
    ];

    private readonly SkippableLongCODEC[] _skippableCodecs =
    [
        new LongJustCopy(),
        new LongVariableByte(),
        new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte())
    ];

    [Fact]
    public void longVariableByteAllBitWidths()
    {
        LongVariableByte codec = new LongVariableByte();
        for (int bitWidth = 0; bitWidth <= 64; bitWidth++)
        {
            long value = bitWidth == 0 ? 0 : 1L << (bitWidth - 1);
            int expectedSizeInBytes = Math.Max(1, (bitWidth + 6) / 7);
            int expectedSizeInLongs = expectedSizeInBytes > 8 ? 2 : 1;

            Assert.Equal(expectedSizeInLongs, Compress((LongCODEC)codec, [value]).Length);
            Assert.Equal(expectedSizeInBytes, Compress(codec, [value]).Length);
            Assert.Equal(expectedSizeInLongs, CompressHeadless(codec, [value]).Length);
        }
    }

    [Fact]
    public void longAs2IntsIntermediateHighPowerOfTwo()
    {
        Assert.Equal(3, Compress(new LongAs2IntsCodec(), [1L << 42]).Length);
    }

    [Fact]
    public void emptyArrayFastInverseDelta()
    {
        LongCompressor compressor = new LongCompressor();
        long[] input = [];
        LongDelta.delta(input);
        long[] compressed = compressor.compress(input);
        long[] result = compressor.uncompress(compressed);
        LongDelta.fastinverseDelta(result);
        Assert.NotNull(result);
        Assert2.assertArrayEquals(input, result);
    }

    [Fact]
    public void longCodecRoundTripsVaryingLengths()
    {
        long[] data = new long[4096];
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;

        foreach (LongCODEC codec in _codecs)
        {
            for (int length = 1; length <= 128; length++)
                AssertRoundTrip(codec, data, length);

            for (int length = 128; length <= data.Length; length *= 2)
                AssertRoundTrip(codec, data, length);
        }
    }

    [Fact]
    public void longCodecsHandleAbstractEdgeCases()
    {
        long[][] inputs =
        [
            [-1],
            [long.MinValue],
            [0, long.MinValue],
            ZeroRunThen(-1, 8),
            ZeroRunThen(-1, 127),
            ZeroRunThen(-1, 128)
        ];

        foreach (long[] input in inputs)
        foreach (LongCODEC codec in new LongCODEC[] { new LongVariableByte(), new LongAs2IntsCodec() })
            CheckConsistency(codec, input);

        for (int bitWidth = 0; bitWidth <= 64; bitWidth++)
        {
            long value = bitWidth == 0 ? 0 : 1L << (bitWidth - 1);
            foreach (LongCODEC codec in new LongCODEC[] { new LongVariableByte(), new LongAs2IntsCodec() })
            {
                CheckConsistency(codec, [value]);
                CheckConsistency(codec, [0, value]);
            }
        }
    }

    [Fact]
    public void longCodecRoundTripsNegativeTail()
    {
        long[] data = new long[128];
        data[127] = -1;

        foreach (LongCODEC codec in _codecs)
        {
            for (int length = 1; length <= 128; length++)
                AssertRoundTrip(codec, data, length);
        }
    }

    [Fact]
    public void longCodecCompressedOffsetRoundTrips()
    {
        foreach (LongCODEC codec in _codecs)
        {
            for (int offset = 0; offset < 50; offset++)
            {
                long[] input = [2, 3, 4, 5];
                long[] compressed = new long[90];
                long[] recovered = new long[input.Length];
                IntWrapper inputOffset = new IntWrapper();
                IntWrapper compressedOffset = new IntWrapper(offset);
                codec.compress(input, inputOffset, input.Length, compressed, compressedOffset);
                int compressedLength = compressedOffset.get() - offset;

                compressedOffset.set(offset);
                codec.uncompress(compressed, compressedOffset, compressedLength, recovered, new IntWrapper());
                Assert2.assertArrayEquals(input, recovered);
            }
        }
    }

    [Fact]
    public void longCodecZeroInZeroOut()
    {
        foreach (LongCODEC codec in _codecs)
        {
            long[] empty = [];
            IntWrapper outpos = new IntWrapper();
            codec.compress(empty, new IntWrapper(), 0, empty, outpos);
            Assert.Equal(0, outpos.get());

            codec.uncompress(empty, new IntWrapper(), 0, empty, outpos);
            Assert.Equal(0, outpos.get());
        }
    }

    [Fact]
    public void skippableLongCodecsAreConsistent()
    {
        long[] data = new long[4096];
        long[] recovered = new long[data.Length];
        for (int k = 0; k < data.Length; ++k)
            data[k] = k % 128;

        foreach (SkippableLongCODEC codec in _skippableCodecs)
        {
            for (int n = 0; n <= data.Length; ++n)
            {
                long[] compressed = new long[codec.maxHeadlessCompressedLength(new IntWrapper(), n)];
                IntWrapper inpos = new IntWrapper();
                IntWrapper outpos = new IntWrapper();
                codec.headlessCompress(data, inpos, n, compressed, outpos);

                IntWrapper compressedInpos = new IntWrapper();
                IntWrapper recoveredOutpos = new IntWrapper();
                codec.headlessUncompress(compressed, compressedInpos, outpos.get(), recovered, recoveredOutpos, n);
                Assert.Equal(n, recoveredOutpos.get());
                Assert.Equal(outpos.get(), compressedInpos.get());
                for (int i = 0; i < n; i++)
                    Assert.Equal(data[i], recovered[i]);
            }
        }
    }

    [Fact]
    public void skippableLongCodecRoundTripsVaryingLengths()
    {
        long[] data = new long[4096];
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;

        foreach (SkippableLongCODEC codec in _skippableCodecs)
        {
            for (int length = 1; length <= 128; length++)
                AssertHeadlessRoundTrip(codec, data, length);

            for (int length = 128; length <= data.Length; length *= 2)
                AssertHeadlessRoundTrip(codec, data, length);
        }
    }

    [Fact]
    public void skippableLongCodecRoundTripsNegativeTail()
    {
        long[] data = new long[128];
        data[127] = -1;

        foreach (SkippableLongCODEC codec in _skippableCodecs)
        {
            for (int length = 1; length <= 128; length++)
                AssertHeadlessRoundTrip(codec, data, length);
        }
    }

    [Fact]
    public void skippableLongMaxHeadlessCompressedLengthIsTightEnough()
    {
        TestMaxHeadlessCompressedLength(new LongJustCopy(), 128);
        TestMaxHeadlessCompressedLength(new LongBinaryPacking(), 16 * LongBinaryPacking.BLOCK_SIZE);
        TestMaxHeadlessCompressedLength(new LongVariableByte(), 128);
        TestMaxHeadlessCompressedLength(new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte()), (16 * LongBinaryPacking.BLOCK_SIZE) + 10);
    }

    [Fact]
    public void skippableLongCompositionRespectsOutputOffset()
    {
        foreach (int offset in new[] { 0, 1, 6 })
        {
            SkippableLongComposition codec = new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte());
            long[] input = [2, 3, 4, 5];
            long[] compressed = new long[codec.maxHeadlessCompressedLength(new IntWrapper(), input.Length)];
            long[] uncompressed = new long[offset + input.Length];

            IntWrapper inputOffset = new IntWrapper();
            IntWrapper compressedOffset = new IntWrapper();
            codec.headlessCompress(input, inputOffset, input.Length, compressed, compressedOffset);

            int compressedLength = compressedOffset.get();
            IntWrapper uncompressedOffset = new IntWrapper(offset);
            compressedOffset = new IntWrapper();
            codec.headlessUncompress(compressed, compressedOffset, compressedLength, uncompressed, uncompressedOffset, input.Length);

            long[] actual = new long[input.Length];
            Array.Copy(uncompressed, offset, actual, 0, input.Length);
            Assert2.assertArrayEquals(input, actual);
        }
    }

    [Fact]
    public void longCodecClusteredDeltaRoundTrips()
    {
        LongClusteredDataGenerator generator = new LongClusteredDataGenerator();
        foreach (LongCODEC codec in _codecs)
        {
            for (int sparsity = 1; sparsity < 20; sparsity += 4)
            {
                long[] data = generator.generateClustered(1 << 10, 1L << (10 + sparsity));
                long[] backup = new long[data.Length];
                Array.Copy(data, backup, data.Length);
                LongDelta.delta(backup);
                long[] compressed = Compress(codec, backup);
                long[] recovered = Uncompress(codec, compressed, backup.Length);
                LongDelta.fastinverseDelta(recovered);
                Assert2.assertArrayEquals(data, recovered);
            }
        }
    }

    [Fact]
    public void longCodecClusteredOffsetRoundTrips()
    {
        foreach (LongCODEC codec in _codecs)
        {
            Test(codec, codec, 5, 10);
            Test(codec, codec, 5, 14);
            Test(codec, codec, 2, 18);
        }
    }

    [Fact]
    public void longCodecRoundTripsUnsortedInputs()
    {
        foreach (LongCODEC codec in _codecs)
        {
            TestUnsorted(codec);
            TestUnsorted2(codec);
            TestUnsorted3(codec);
        }
    }

    [Fact]
    public void longCodecRoundTripsFastPforBlockEdgeCases()
    {
        foreach (LongCODEC codec in _codecs)
        {
            TestFastPforBlock(codec, FastPFOR.BLOCK_SIZE);
            TestFastPforBlock(codec, FastPFOR128.BLOCK_SIZE);
        }
    }

    private static void AssertRoundTrip(LongCODEC codec, long[] data, int length)
    {
        long[] input = new long[length];
        Array.Copy(data, input, length);
        long[] compressed = Compress(codec, input);
        long[] answer = Uncompress(codec, compressed, length);
        Assert2.assertArrayEquals(input, answer);
    }

    private static void AssertHeadlessRoundTrip(SkippableLongCODEC codec, long[] data, int length)
    {
        long[] input = new long[length];
        Array.Copy(data, input, length);
        long[] compressed = LongTestUtils.compressHeadless(codec, input);
        long[] answer = LongTestUtils.uncompressHeadless(codec, compressed, length);
        Assert2.assertArrayEquals(input, answer);
    }

    private static void CheckConsistency(LongCODEC codec, long[] array)
    {
        long[] compressed = LongTestUtils.compress(codec, array);
        long[] uncompressed = LongTestUtils.uncompress(codec, compressed, array.Length);
        Assert2.assertArrayEquals(array, uncompressed);

        if (codec is ByteLongCODEC byteLongCodec)
        {
            byte[] byteCompressed = LongTestUtils.compress(byteLongCodec, array);
            long[] byteUncompressed = LongTestUtils.uncompress(byteLongCodec, byteCompressed, array.Length);
            Assert2.assertArrayEquals(array, byteUncompressed);
        }

        if (codec is SkippableLongCODEC skippableLongCodec)
        {
            long[] headlessCompressed = LongTestUtils.compressHeadless(skippableLongCodec, array);
            long[] headlessUncompressed = LongTestUtils.uncompressHeadless(skippableLongCodec, headlessCompressed, array.Length);
            Assert2.assertArrayEquals(array, headlessUncompressed);
        }
    }

    private static void TestMaxHeadlessCompressedLength(SkippableLongCODEC codec, int inlengthTo)
    {
        for (int inlength = 0; inlength < inlengthTo; ++inlength)
        {
            long[] input = new long[inlength];
            Array.Fill(input, -1L);

            int maxOutputLength = codec.maxHeadlessCompressedLength(new IntWrapper(), inlength);
            long[] output = new long[maxOutputLength];
            IntWrapper outPos = new IntWrapper();
            codec.headlessCompress(input, new IntWrapper(), inlength, output, outPos);

            Assert.True(outPos.get() <= maxOutputLength);
        }
    }

    private static void Test(LongCODEC codec, LongCODEC codecOut, int count, int numberOfBits)
    {
        LongClusteredDataGenerator generator = new LongClusteredDataGenerator();
        for (int sparsity = 1; sparsity < 31 - numberOfBits; sparsity += 4)
        {
            long[][] data = new long[count][];
            int max = 1 << (numberOfBits + sparsity);
            for (int k = 0; k < count; ++k)
                data[k] = generator.generateClustered(1 << numberOfBits, max);

            TestCodec(codec, codecOut, data);
        }

        for (int sparsity = 1; sparsity < 63 - numberOfBits; sparsity += 4)
        {
            long[][] data = new long[count][];
            long max = 1L << (numberOfBits + sparsity);
            for (int k = 0; k < count; ++k)
                data[k] = generator.generateClustered(1 << numberOfBits, max);

            TestCodec(codec, codecOut, data);
        }
    }

    private static void TestCodec(LongCODEC codec, LongCODEC codecOut, long[][] data)
    {
        int maxLength = 0;
        foreach (long[] array in data)
        {
            if (array.Length > maxLength)
                maxLength = array.Length;
        }

        long[] buffer = new long[maxLength + 1024];
        long[] dataOut = new long[(4 * maxLength) + 1024];
        IntWrapper inpos = new IntWrapper();
        IntWrapper outpos = new IntWrapper();
        foreach (long[] input in data)
        {
            long[] backupData = new long[input.Length];
            Array.Copy(input, backupData, backupData.Length);

            inpos.set(1);
            outpos.set(0);
            if (codec is not IntegratedLongCODEC)
                LongDelta.delta(backupData);

            codec.compress(backupData, inpos, backupData.Length - inpos.get(), dataOut, outpos);
            int thisCompSize = outpos.get() + 1;
            inpos.set(0);
            outpos.set(1);
            buffer[0] = backupData[0];
            codecOut.uncompress(dataOut, inpos, thisCompSize - 1, buffer, outpos);
            if (codec is not IntegratedLongCODEC)
                LongDelta.fastinverseDelta(buffer);

            Assert.Equal(input.Length, outpos.get());
            long[] bufferCutout = new long[outpos.get()];
            Array.Copy(buffer, bufferCutout, bufferCutout.Length);
            Assert2.assertArrayEquals(input, bufferCutout);
        }
    }

    private static void TestUnsorted(LongCODEC codec)
    {
        int[] lengths = [133, 1026, 1333333];
        foreach (int length in lengths)
        {
            long[] data = new long[length];
            for (int k = 0; k < length; k++)
                data[k] = 3;
            for (int k = 0; k < length; k += 5)
                data[k] = 100;
            for (int k = 0; k < length; k += 533)
                data[k] = 10000;
            data[5] = -311;

            long[] compressed = new long[(int)Math.Ceiling(length * 1.01) + 1024];
            IntWrapper outputOffset = new IntWrapper();
            codec.compress(data, new IntWrapper(), data.Length, compressed, outputOffset);
            long[] packed = new long[outputOffset.get()];
            Array.Copy(compressed, packed, packed.Length);

            long[] recovered = new long[length];
            codec.uncompress(packed, new IntWrapper(), packed.Length, recovered, new IntWrapper());
            Assert2.assertArrayEquals(data, recovered);
        }
    }

    private static void TestUnsorted2(LongCODEC codec)
    {
        long[] data = new long[128];
        data[5] = -1;
        AssertRoundTrip(codec, data, data.Length);
    }

    private static void TestUnsorted3(LongCODEC codec)
    {
        long[] data = new long[128];
        data[127] = -1;
        AssertRoundTrip(codec, data, data.Length);
    }

    private static void TestFastPforBlock(LongCODEC codec, int length)
    {
        long[] data = new long[length];
        data[126] = -1;
        long[] compressed = LongTestUtils.compress(codec, data);
        long[] answer = LongTestUtils.uncompress(codec, compressed, length);
        Assert2.assertArrayEquals(data, answer);
    }

    private static long[] ZeroRunThen(long value, int zeroRunLength)
    {
        long[] data = new long[zeroRunLength + 1];
        data[zeroRunLength] = value;
        return data;
    }

    private static long[] Compress(LongCODEC codec, long[] data)
    {
        long[] compressed = new long[Math.Max(1, (data.Length * 16) + 1024)];
        IntWrapper inputOffset = new IntWrapper();
        IntWrapper outputOffset = new IntWrapper();
        codec.compress(data, inputOffset, data.Length, compressed, outputOffset);
        long[] answer = new long[outputOffset.get()];
        Array.Copy(compressed, answer, answer.Length);
        return answer;
    }

    private static byte[] Compress(LongVariableByte codec, long[] data)
    {
        byte[] compressed = new byte[Math.Max(1, (data.Length * 16) + 1024)];
        IntWrapper inputOffset = new IntWrapper();
        IntWrapper outputOffset = new IntWrapper();
        codec.compress(data, inputOffset, data.Length, compressed, outputOffset);
        byte[] answer = new byte[outputOffset.get()];
        Array.Copy(compressed, answer, answer.Length);
        return answer;
    }

    private static long[] CompressHeadless(LongVariableByte codec, long[] data)
    {
        long[] compressed = new long[codec.maxHeadlessCompressedLength(new IntWrapper(), data.Length)];
        IntWrapper inputOffset = new IntWrapper();
        IntWrapper outputOffset = new IntWrapper();
        codec.headlessCompress(data, inputOffset, data.Length, compressed, outputOffset);
        long[] answer = new long[outputOffset.get()];
        Array.Copy(compressed, answer, answer.Length);
        return answer;
    }

    private static long[] Uncompress(LongCODEC codec, long[] compressed, int length)
    {
        long[] answer = new long[length];
        codec.uncompress(compressed, new IntWrapper(), compressed.Length, answer, new IntWrapper());
        return answer;
    }
}