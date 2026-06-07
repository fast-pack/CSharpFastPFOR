using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Vector;
using static Genbox.CSharpFastPFOR.IntegerCompression.Vector.VectorBitPackerKernels;

namespace Genbox.CSharpFastPFOR.Tests.Vector;

public class VectorBinaryPackingTest
{
    [Fact]
    public void roundTripAcrossLaneWidths()
    {
        roundTrip(LaneWidth.BITS_128);
        roundTrip(LaneWidth.BITS_256);
        roundTrip(LaneWidth.BITS_512);
    }

    private static void roundTrip(LaneWidth laneWidth)
    {
        int[] data = new int[3 * VectorBinaryPacking.BLOCK_SIZE];
        for (int i = 0; i < data.Length; i++)
            data[i] = i % 8;
        data[5] = 1 << 20;
        data[600] = 1 << 30;

        VectorBinaryPacking codec = new VectorBinaryPacking(laneWidth);
        int[] compressed = new int[2 * data.Length];
        IntWrapper outpos = new IntWrapper();
        codec.headlessCompress(data, new IntWrapper(), data.Length, compressed, outpos);

        int[] recovered = new int[data.Length];
        codec.headlessUncompress(compressed, new IntWrapper(), outpos.get(), recovered, new IntWrapper(), data.Length);

        Assert.Equal(data, recovered);
    }
}