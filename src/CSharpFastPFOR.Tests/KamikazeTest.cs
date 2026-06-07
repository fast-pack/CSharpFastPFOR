using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.Kamikaze;
using KamikazeCodec = Genbox.CSharpFastPFOR.IntegerCompression.Kamikaze;

namespace Genbox.CSharpFastPFOR.Tests;

public class KamikazeTest
{
    [Fact]
    public void pforDeltaRoundTripsBlocks()
    {
        int[][] inputs =
        [
            new int[128],
            Enumerable.Range(0, 128).Select(i => i % 17).ToArray(),
            Enumerable.Range(0, 128).Select(i => i == 64 ? 1 << 20 : i % 8).ToArray()
        ];

        foreach (int[] input in inputs)
        {
            int[] compressed = PForDelta.compressOneBlockOpt(input, input.Length);
            int[] recovered = new int[input.Length];

            int compressedBits = PForDelta.decompressOneBlock(recovered, compressed, recovered.Length);

            Assert.Equal(compressed.Length, (compressedBits + 31) / 32);
            Assert.Equal(input, recovered);
        }
    }

    [Fact]
    public void headlessCodecRoundTripsBlock()
    {
        int[] input = Enumerable.Range(0, 128).Select(i => i % 23).ToArray();
        int[] compressed = new int[input.Length * 4];
        int[] recovered = new int[input.Length];
        KamikazeCodec codec = new KamikazeCodec();
        IntWrapper inputPosition = new IntWrapper();
        IntWrapper compressedPosition = new IntWrapper();

        codec.headlessCompress(input, inputPosition, input.Length, compressed, compressedPosition);

        IntWrapper recoveredPosition = new IntWrapper();
        codec.headlessUncompress(compressed, new IntWrapper(), compressedPosition.get(), recovered, recoveredPosition, input.Length);

        Assert.Equal(input.Length, inputPosition.get());
        Assert.Equal(input.Length, recoveredPosition.get());
        Assert.Equal(input, recovered);
    }

    [Fact]
    public void lucenePforDeltaRoundTripsBlock()
    {
        int[] input = Enumerable.Range(0, 128).Select(i => i == 37 ? 1 << 18 : i % 11).ToArray();
        TestableLCPForDelta codec = new TestableLCPForDelta();
        int compressedLength = codec.compress(input, input.Length);
        int[] recovered = new int[input.Length];

        LCPForDelta.decompressOneBlock(recovered, codec.getCompressedBuffer(), recovered.Length);

        Assert.InRange(compressedLength, 1, codec.getCompressedBuffer().Length);
        Assert.Equal(input, recovered);
    }

    [Fact]
    public void hardCodedUnpackMatchesGenericUnpack()
    {
        int[] bitWidths = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 16, 20, 28];

        foreach (int bits in bitWidths)
        {
            int[] packed = new int[1 + (bits * 4)];
            int mask = bits == 0 ? 0 : -1 >>> (32 - bits);
            int[] expected = Enumerable.Range(0, 128).Select(i => i & mask).ToArray();

            for (int i = 0; i < expected.Length; i++)
                LCPForDelta.writeBits(packed, expected[i], 32 + (i * bits), bits);

            int[] generic = new int[128];
            int[] hardCoded = new int[128];
            if (bits != 0)
                LCPForDelta.decompressBBitSlots(generic, packed, generic.Length, bits);
            TestableLCPForDelta.hardCodedUnpack(hardCoded, packed, bits);

            Assert.Equal(expected, generic);
            Assert.Equal(generic, hardCoded);
        }
    }

    private sealed class TestableLCPForDelta : LCPForDelta
    {
        public int[] getCompressedBuffer() => getCompBuffer();

        public static void hardCodedUnpack(int[] output, int[] input, int bits) =>
            decompressBBitSlotsWithHardCodes(output, input, output.Length, bits);
    }
}